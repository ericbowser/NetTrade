// NetTrade/Service/GridTradingService.cs

using NLog;
using NetTrade.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Alpaca.Markets;
using ILogger = NLog.ILogger;
using NetTrade.Helpers;

namespace NetTrade.Service
{
    public interface IGridBacktestService
    {
        Task<GridBacktestResult> RunBacktest(GridBacktestRequest request);
        Task<List<GridLevel>> CalculateGridLevels(GridTradingConfiguration config);
        Task<List<CandleData>> GetHistoricalData(GridTradingConfiguration config);
    }

    public class GridStrategyService : IGridBacktestService
    {
        private readonly IAlpacaCryptoDataClient _alpacaCryptoDataClient;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public GridStrategyService(IAlpacaCryptoDataClient alpacaCryptoDataClient)
        {
            _alpacaCryptoDataClient = alpacaCryptoDataClient;
        }

        public async Task<GridBacktestResult> RunBacktest(GridBacktestRequest request)
        {

            GridBacktestResult? finalResult = null;
            try
            {
                // Split date range into manageable chunks (e.g., monthly)
                if (request.Configuration != null)
                {
                    var dateChunks = Extensions.CreateDateChunks(request.Configuration.StartDate, request.Configuration.EndDate, TimeSpan.FromDays(30));
                
                    _logger.Info("Split backtest period into {Count} chunks for processing", dateChunks.Count);

                    // Calculate grid levels just once
                    _logger.Info("Calculating grid levels for backtest...");
                    var gridLevels = await CalculateGridLevels(request.Configuration);
                    
                    if (gridLevels == null || gridLevels.Count == 0)
                    {
                        _logger.Error("Grid levels calculation returned null or empty list");
                        throw new InvalidOperationException("Failed to calculate grid levels. Please check your configuration.");
                    }
                    
                    _logger.Info($"Grid levels calculated successfully: {gridLevels.Count} total levels");
                    var buyLevelsCount = gridLevels.Count(g => g.OrderSide == OrderSide.Buy);
                    var sellLevelsCount = gridLevels.Count(g => g.OrderSide == OrderSide.Sell);
                    _logger.Info($"Grid levels breakdown: {buyLevelsCount} Buy levels, {sellLevelsCount} Sell levels");

                    // Initialize the result object
                    finalResult = new GridBacktestResult
                    {
                        InitialCapital = request.InitialCapital,
                        FinalEquity = request.InitialCapital,
                        StartDate = request.Configuration.StartDate,
                        EndDate = request.Configuration.EndDate,
                        Configuration = request.Configuration,
                        GridLevels = gridLevels,
                        Trades = new List<GridTrade>(),
                        TotalProfitPercentage = 0.00M
                    };

                    // Process each time chunk
                    decimal runningCapital = request.InitialCapital;
                    decimal runningAssetHolding = 0;
                    int successfulChunks = 0;
                    int failedChunks = 0;

                    for (var i = 0; i < dateChunks.Count; i++)
                    {
                        var chunk = dateChunks[i];
                        _logger.Info(
                            $"Processing chunk {i + 1}/{dateChunks.Count}: {chunk.Start:yyyy-MM-dd} to {chunk.End:yyyy-MM-dd}");

                        try
                        {
                            // Create a copy of the config with the chunk's date range
                            var chunkConfig = request.Configuration.CloneConfiguration();
                            chunkConfig.StartDate = chunk.Start;
                            chunkConfig.EndDate = chunk.End;

                            // Get historical data for this chunk with timeout protection
                            _logger.Info($"Fetching data for chunk {i + 1}...");
                            var chunkCandles = await GetHistoricalData(chunkConfig);
                            _logger.Info($"Retrieved {chunkCandles.Count} candles for chunk {i + 1}");

                            if (chunkCandles.Count == 0)
                            {
                                _logger.Warn($"No data available for chunk {i + 1}, skipping");
                                failedChunks++;
                                continue;
                            }

                            // Run backtest for this chunk, passing in the current capital and holdings
                            _logger.Info($"Running backtest simulation for chunk {i + 1} with {chunkCandles.Count} candles...");
                            var chunkResult = chunkCandles.BacktestGridStrategyChunk(
                                gridLevels,
                                runningCapital,
                                runningAssetHolding
                            );

                            // Update the running values for the next chunk
                            runningCapital = chunkResult.Capital;
                            runningAssetHolding = chunkResult.AssetHolding;

                            // Add this chunk's trades to the overall result
                            finalResult.Trades.AddRange(chunkResult.Trades);

                            successfulChunks++;
                            _logger.Info(
                                $"Chunk {i + 1} complete: Capital now ${runningCapital:F2}, Assets: {runningAssetHolding:F8}, Trades: {chunkResult.Trades.Count}");
                        }
                        catch (Exception ex)
                        {
                            failedChunks++;
                            _logger.Error(ex, $"Error processing chunk {i + 1}/{dateChunks.Count}. Continuing with next chunk...");
                            // Continue processing remaining chunks even if one fails
                            continue;
                        }
                    }

                    _logger.Info($"Chunk processing complete: {successfulChunks} successful, {failedChunks} failed out of {dateChunks.Count} total chunks");

                    // Validate we have at least one successful chunk
                    if (successfulChunks == 0)
                    {
                        _logger.Error("No chunks were successfully processed. Cannot complete backtest.");
                        throw new InvalidOperationException("Failed to process any chunks. Please check your configuration and data availability.");
                    }

                    if (failedChunks > 0)
                    {
                        _logger.Warn($"Warning: {failedChunks} chunk(s) failed to process. Results may be incomplete.");
                    }

                    // Calculate final equity (capital + asset value at final price)
                    // We need the last price to value the assets
                    decimal finalPrice = 0;
                    try
                    {
                        var lastChunkConfig = request.Configuration.CloneConfiguration();
                        lastChunkConfig.StartDate = dateChunks.Last().End.AddMinutes(-10); // Just get a few minutes of data
                        lastChunkConfig.EndDate = dateChunks.Last().End;
                        _logger.Info("Fetching final price data...");
                        var finalPriceData = await GetHistoricalData(lastChunkConfig);
                        _logger.Info($"Final price data retrieved: {finalPriceData?.Count ?? 0} candles");
                        
                        if (finalPriceData != null && finalPriceData.Any())
                        {
                            _logger.Info("Extracting final price from last candle...");
                            finalPrice = finalPriceData.Last().Close;
                            _logger.Info($"Final price extracted: {finalPrice}");
                        }
                        else
                        {
                            _logger.Warn("Final price data is empty or null");
                            finalPrice = 0;
                        }
                        
                        if (finalPrice == 0)
                        {
                            _logger.Warn("Could not determine final price from API. Using last trade price or current price.");
                            // Try to get price from last trade if available
                            if (finalResult.Trades.Any())
                            {
                                var lastTrade = finalResult.Trades.OrderByDescending(t => t.Timestamp).First();
                                finalPrice = lastTrade.Price;
                                _logger.Info($"Using last trade price as final price: {finalPrice}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Error fetching final price. Using last trade price or default.");
                        // Fallback to last trade price if available
                        if (finalResult.Trades.Any())
                        {
                            var lastTrade = finalResult.Trades.OrderByDescending(t => t.Timestamp).First();
                            finalPrice = lastTrade.Price;
                            _logger.Info($"Using last trade price as fallback: {finalPrice}");
                        }
                        else
                        {
                            _logger.Warn("No trades available for fallback price. Final equity calculation may be inaccurate.");
                        }
                    }

                    // Update final result metrics
                    _logger.Info("Calculating final equity and metrics...");
                    _logger.Info($"Running capital: ${runningCapital:F2}, Running asset holding: {runningAssetHolding:F8}, Final price: {finalPrice:F2}");
                    
                    decimal finalAssetValue = runningAssetHolding * finalPrice;
                    _logger.Info($"Final asset value: ${finalAssetValue:F2}");
                    
                    finalResult.FinalEquity = runningCapital + finalAssetValue;
                    _logger.Info($"Final equity calculated: ${finalResult.FinalEquity:F2}");
                    
                    finalResult.TotalProfit = finalResult.FinalEquity - request.InitialCapital;
                    _logger.Info($"Total profit: ${finalResult.TotalProfit:F2}");
                    
                    finalResult.TotalProfitPercentage = (finalResult.TotalProfit / request.InitialCapital) * 100;
                    _logger.Info($"Total profit percentage: {finalResult.TotalProfitPercentage:F2}%");
                    
                    finalResult.TotalTrades = finalResult.Trades.Count;
                    _logger.Info($"Total trades: {finalResult.TotalTrades}");

                    // Calculate win rate and profit metrics
                    // For grid trading, only count sell trades that have PnL calculated (completed round trips)
                    _logger.Info("Calculating win rate metrics...");
                    _logger.Info($"Total trades in result: {finalResult.Trades?.Count ?? 0}");
                    
                    var sellTrades = finalResult.Trades?.Where(t => t.Direction == OrderSide.Sell).ToList() ?? new List<GridTrade>();

                    _logger.Info($"Total sell trades: {sellTrades.Count}");
                    _logger.Info($"Sell trades with PnL: {sellTrades.Count(t => t.PnL != 0)}");

                    // Count profitable and unprofitable round trips
                    _logger.Info("Counting winning and losing trades...");
                    try
                    {
                        int winningCount = 0;
                        int losingCount = 0;
                        
                        foreach (var trade in sellTrades)
                        {
                            if (trade.PnL > 0)
                                winningCount++;
                            else if (trade.PnL < 0)
                                losingCount++;
                        }
                        
                        finalResult.WinningTrades = winningCount;
                        finalResult.LosingTrades = losingCount;
                        
                        _logger.Info($"Winning trades: {finalResult.WinningTrades}, Losing trades: {finalResult.LosingTrades}");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Error counting winning/losing trades");
                        finalResult.WinningTrades = 0;
                        finalResult.LosingTrades = 0;
                    }

                    if (sellTrades.Count > 0)
                    {
                        finalResult.WinRate = ((decimal)finalResult.WinningTrades / sellTrades.Count) * 100;

                        var winningTradesTotal = sellTrades.Where(t => t.PnL > 0).Sum(t => t.PnL);
                        var losingTradesTotal = Math.Abs(sellTrades.Where(t => t.PnL < 0).Sum(t => t.PnL));

                        _logger.Info($"Total winning PnL: ${winningTradesTotal:F2}, Total losing PnL: ${losingTradesTotal:F2}");

                        if (finalResult.WinningTrades > 0)
                            finalResult.AverageWin = winningTradesTotal / finalResult.WinningTrades;

                        if (finalResult.LosingTrades > 0)
                            finalResult.AverageLoss = losingTradesTotal / finalResult.LosingTrades;

                        finalResult.ProfitFactor = losingTradesTotal > 0 ? winningTradesTotal / losingTradesTotal : (winningTradesTotal > 0 ? decimal.MaxValue : 0);
                    }
                    else
                    {
                        _logger.Info("No sell trades found. Setting default metrics.");
                        finalResult.WinRate = 0;
                        finalResult.AverageWin = 0;
                        finalResult.AverageLoss = 0;
                        finalResult.ProfitFactor = 0;
                    }

                    _logger.Info("Final metrics calculated. Preparing to return result...");
                    _logger.Info($"Final Result Summary - Initial Capital: ${finalResult.InitialCapital:F2}, Final Equity: ${finalResult.FinalEquity:F2}, Total Trades: {finalResult.TotalTrades}");
                    
                    // Log trade count breakdown to help diagnose serialization issues
                    if (finalResult.Trades != null)
                    {
                        _logger.Info($"Trades list contains {finalResult.Trades.Count} items");
                        var buyCount = finalResult.Trades.Count(t => t.Direction == OrderSide.Buy);
                        var sellCount = finalResult.Trades.Count(t => t.Direction == OrderSide.Sell);
                        _logger.Info($"Trade breakdown: {buyCount} buys, {sellCount} sells");
                    }
                    
                    // Log grid levels info to diagnose serialization issues
                    if (finalResult.GridLevels != null)
                    {
                        _logger.Info($"GridLevels list contains {finalResult.GridLevels.Count} items");
                        var buyLevels = finalResult.GridLevels.Count(g => g.OrderSide == OrderSide.Buy);
                        var sellLevels = finalResult.GridLevels.Count(g => g.OrderSide == OrderSide.Sell);
                        _logger.Info($"Grid levels breakdown: {buyLevels} buy levels, {sellLevels} sell levels");
                    }
                    
                    // Log configuration info
                    if (finalResult.Configuration != null)
                    {
                        _logger.Info($"Configuration present: Symbol={finalResult.Configuration.Symbol}, GridLevels={finalResult.Configuration.GridLevels}");
                    }
                    
                    _logger.Info("Returning final result from RunBacktest method");
                    _logger.Info("About to return result object with GridLevels count: {Count}", finalResult.GridLevels?.Count ?? 0);
                    
                    // GridLevels are excluded from serialization via [JsonIgnore] attribute
                    // No need to clear them manually
                    _logger.Info("About to return finalResult from RunBacktest...");
                    return finalResult;
                }

                _logger.Error("Configuration is null. Cannot run backtest.");
                throw new ArgumentNullException(nameof(request.Configuration), "Grid trading configuration is required.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error running grid trading backtest");
                throw;
            }
        }

        public async Task<List<GridLevel>> CalculateGridLevels(GridTradingConfiguration config)
        {
            try
            {
                _logger.Info($"Calculating grid levels: GridLevels={config.GridLevels}, GridRange={config.GridRange}%");
                
                var gridLevels = new List<GridLevel>();

                // Get reference price
                // For backtesting, use StartDate if it's valid (not DateTime.MinValue)
                // For live trading, use null to get current price
                DateTime? referenceDate = null;
                if (config.StartDate != DateTime.MinValue && config.StartDate != default(DateTime))
                {
                    referenceDate = config.StartDate;
                    _logger.Info($"Using StartDate {config.StartDate} for reference price (backtesting mode)");
                }
                else
                {
                    _logger.Info($"StartDate not set or invalid, using current price (live trading mode)");
                }
                
                var currentPrice = await GetReferencePriceAsync(config.Symbol, referenceDate);
                _logger.Info($"Reference price for {config.Symbol}: {currentPrice}");

                // Calculate price range
                decimal upperBound = currentPrice * (1 + config.GridRange / 100);
                decimal lowerBound = currentPrice * (1 - config.GridRange / 100);
                _logger.Info($"Price range: Lower={lowerBound}, Upper={upperBound}, Range={upperBound - lowerBound}");

                // Calculate grid step size
                // Create exactly config.GridLevels levels, so divide by (GridLevels - 1) to get intervals between levels
                // For GridLevels=5, we need 4 intervals between 5 points
                decimal gridStep = config.GridLevels > 1 
                    ? (upperBound - lowerBound) / (config.GridLevels - 1) 
                    : 0;
                _logger.Info($"Grid step size: {gridStep}");

                // Calculate midpoint for buy/sell split
                // Create exactly config.GridLevels levels (0 to GridLevels-1)
                int midpoint = config.GridLevels / 2;
                _logger.Info($"Buy/Sell split midpoint: {midpoint} (levels 0-{midpoint - 1} are Buy, levels {midpoint}-{config.GridLevels - 1} are Sell)");

                // Generate grid levels
                // Create exactly config.GridLevels levels (0 to GridLevels-1)
                for (int i = 0; i < config.GridLevels; i++)
                {
                    decimal price = lowerBound + (gridStep * i);

                    // Split buy/sell at midpoint: levels below midpoint are Buy, at or above are Sell
                    OrderSide orderSide = i < midpoint ? OrderSide.Buy : OrderSide.Sell;

                    gridLevels.Add(new GridLevel
                    {
                        Level = i,
                        Price = price,
                        OrderSide = orderSide,
                        OrderSize = config.OrderSize
                    });
                }

                var buyCount = gridLevels.Count(g => g.OrderSide == OrderSide.Buy);
                var sellCount = gridLevels.Count(g => g.OrderSide == OrderSide.Sell);
                _logger.Info($"Generated {gridLevels.Count} grid levels: {buyCount} Buy, {sellCount} Sell");
                _logger.Info($"Price range: First level={gridLevels.First().Price}, Last level={gridLevels.Last().Price}");

                return gridLevels;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error calculating grid levels");
                throw;
            }
        }

        public async Task<List<CandleData>> GetHistoricalData(GridTradingConfiguration config)
        {
            try
            {
                _logger.Info(
                    $"Fetching historical data for {config.Symbol} from {config.StartDate} to {config.EndDate}");

                // Convert timeframe string to BarTimeFrame enum
                var timeFrame = GetTimeFrameFromString(config.Timeframe);

                // Create request for historical bars
                var barsRequest =
                    new HistoricalCryptoBarsRequest(config.Symbol, config.StartDate, config.EndDate, timeFrame);

                // Get historical bars from Alpaca
                var candles = new List<CandleData>();
                string? pageToken = null;
                var pageCount = 0;
                var maxPages = 100; // Safety limit to prevent infinite loops
                IMultiPage<IBar> barsResponse;

                do
                {
                    try
                    {
                        _logger.Info($"Fetching page {pageCount + 1} of historical data...");
                        
                        // Add timeout to prevent indefinite hangs (30 seconds per page)
                        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                        {
                            try
                            {
                                barsResponse = await _alpacaCryptoDataClient.GetCryptoHistoricalDataAsync(barsRequest)
                                    .WaitAsync(cts.Token);
                                _logger.Info($"Successfully received response for page {pageCount + 1}");
                            }
                            catch (TaskCanceledException)
                            {
                                _logger.Error($"Timeout waiting for page {pageCount + 1} response (30 seconds). Stopping pagination.");
                                // If we have some data, return what we have rather than failing completely
                                if (candles.Count > 0)
                                {
                                    _logger.Warn($"Returning partial data: {candles.Count} candles retrieved before timeout.");
                                    break;
                                }
                                throw new TimeoutException($"Timeout waiting for historical data page {pageCount + 1}. The API call took longer than 30 seconds.");
                            }
                        }

                        var bars = barsResponse?.Items;

                            if (bars != null && bars.Count > 0)
                        {
                            var barsList = bars.FirstOrDefault().Value;
                            if (barsList != null)
                            {
                                // Convert bars to candle data
                                foreach (var bar in barsList)
                                {
                                    var candle = new CandleData
                                    {
                                        Timestamp = bar.TimeUtc,
                                        Open = bar.Open,
                                        High = bar.High,
                                        Low = bar.Low,
                                        Close = bar.Close,
                                        Volume = bar.Volume
                                    };
                                    candles.Add(candle);
                                }
                                _logger.Info($"Added {barsList.Count} candles from page {pageCount + 1}. Total: {candles.Count}");
                                _logger.Info($"Finished processing candles for page {pageCount + 1}. Moving to pagination check...");
                            }
                            else
                            {
                                _logger.Warn($"Page {pageCount + 1} returned empty bars list.");
                                break;
                            }
                        }
                        else
                        {
                            _logger.Warn("No bars found in the response. Symbol may be incorrect or data not available.");
                            break;
                        }

                        // Update the request for the next page
                        _logger.Info($"Checking for next page token after processing page {pageCount + 1}...");
                        _logger.Info($"barsResponse is {(barsResponse == null ? "null" : "not null")}");
                        
                        try
                        {
                            pageToken = barsResponse?.NextPageToken;
                            _logger.Info($"Next page token retrieved: {(string.IsNullOrEmpty(pageToken) ? "null/empty - no more pages" : $"exists ({pageToken.Length} chars)")}");
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex, "Error accessing NextPageToken property");
                            pageToken = null;
                        }
                        
                        if (!string.IsNullOrEmpty(pageToken))
                        {
                            pageCount++;
                            _logger.Info($"Preparing to fetch page {pageCount + 1}...");
                            
                            // Create a fresh request for the next page to avoid state issues
                            barsRequest = new HistoricalCryptoBarsRequest(config.Symbol, config.StartDate, config.EndDate, timeFrame);
                            barsRequest.Pagination.Token = pageToken;
                            
                            // Safety check - if we've fetched too many pages, something might be wrong
                            if (pageCount >= maxPages)
                            {
                                _logger.Warn($"Reached maximum page count of {maxPages}. Breaking pagination loop.");
                                pageToken = null; // Clear token to exit loop
                                break;
                            }
                        }
                        else
                        {
                            // No more pages
                            _logger.Info("No more pages available. Pagination complete.");
                            pageToken = null; // Explicitly set to null to ensure loop exits
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, $"Error fetching page {pageCount + 1}. Stopping pagination.");
                        // If we have some data, return what we have rather than failing completely
                        if (candles.Count > 0)
                        {
                            _logger.Warn($"Returning partial data: {candles.Count} candles retrieved before error.");
                            break;
                        }
                        throw; // Re-throw if we have no data at all
                    }
                } while (!string.IsNullOrEmpty(pageToken));

                _logger.Info("The candles are ready {CandleCount}", candles.Count);

                return candles;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error fetching historical data");
                throw;
            }
        }

        private BarTimeFrame GetTimeFrameFromString(string timeframe)
        {
            return timeframe switch
            {
                "1Min" => BarTimeFrame.Minute,
                "5Min" => new BarTimeFrame(5, BarTimeFrameUnit.Minute),
                "15Min" => new BarTimeFrame(15, BarTimeFrameUnit.Minute),
                "1Hour" => BarTimeFrame.Hour,
                "1Day" => BarTimeFrame.Day,
                _ => BarTimeFrame.Minute
            };
        }

        private async Task<decimal> GetReferencePriceAsync(string symbol, DateTime? referenceDate = null)
        {
            try
            {
                // If reference date is provided (for backtesting), use that date
                // Otherwise, use current date (for live trading)
                DateTime targetDate = referenceDate ?? DateTime.UtcNow;
                
                // Validate targetDate to prevent DateTime arithmetic errors
                if (targetDate == DateTime.MinValue || targetDate == default(DateTime))
                {
                    _logger.Warn($"Invalid targetDate ({targetDate}), using current time instead");
                    targetDate = DateTime.UtcNow;
                }
                
                // Ensure we don't go before DateTime.MinValue when subtracting days
                DateTime minAllowedDate = DateTime.MinValue.AddDays(1);
                DateTime startDate = targetDate < minAllowedDate ? targetDate : targetDate.AddDays(-1);
                DateTime endDate = targetDate.AddDays(1); // Get some buffer to ensure we have data

                var barsRequest = new HistoricalCryptoBarsRequest(symbol, startDate, endDate, BarTimeFrame.Hour);
                var barsResponse = await _alpacaCryptoDataClient.GetCryptoHistoricalDataAsync(barsRequest);

                if (barsResponse.Items.Any())
                {
                    // Find the bar closest to the reference date
                    var allBars = barsResponse.Items.SelectMany(kvp => kvp.Value).ToList();
                    var closestBar = allBars
                        .OrderBy(b => Math.Abs((b.TimeUtc - targetDate).TotalSeconds))
                        .FirstOrDefault();
                    
                    if (closestBar != null)
                    {
                        _logger.Info($"Found reference price {closestBar.Close} at {closestBar.TimeUtc} (target: {targetDate})");
                        return closestBar.Close;
                    }
                    
                    // Fallback to last bar if no close match
                    var last = allBars.OrderBy(b => b.TimeUtc).LastOrDefault();
                    if (last != null)
                    {
                        _logger.Info($"Using last available bar price {last.Close} at {last.TimeUtc}");
                        return last.Close;
                    }
                }

                // Fallback
                _logger.Warn($"No price data found for {symbol} around {targetDate}, using default 100");
                return 100m; // Default value if no data is available
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error getting reference price");
                throw;
            }
        }

        private GridBacktestResult BacktestGridStrategy(List<CandleData> candles, List<GridLevel> gridLevels,
            decimal initialCapital, GridTradingConfiguration config)
        {
            var result = new GridBacktestResult
            {
                InitialCapital = initialCapital,
                FinalEquity = initialCapital,
                StartDate = candles.First().Timestamp,
                EndDate = candles.Last().Timestamp,
                Configuration = config,
                GridLevels = gridLevels,
                Trades = new List<GridTrade>()
            };

            // Simulation variables
            decimal capital = initialCapital;
            decimal assetHolding = 0;
            var activeBuyOrders = gridLevels.Where(g => g.OrderSide == OrderSide.Buy).ToList();
            var activeSellOrders = gridLevels.Where(g => g.OrderSide == OrderSide.Sell).ToList();

            // Track which grid levels have been filled
            var filledLevels = new HashSet<int>();

            // Run through each candle
            for (int i = 0; i < candles.Count; i++)
            {
                var candle = candles[i];

                // Check if any grid buy orders were filled (price went down crossing a grid level)
                foreach (var level in activeBuyOrders.ToList())
                {
                    if (candle.Low <= level.Price && !filledLevels.Contains(level.Level))
                    {
                        // Buy order executed
                        decimal spentCapital = level.OrderSize;
                        decimal boughtAssets = level.OrderSize / level.Price;

                        // Update balances
                        capital -= spentCapital;
                        assetHolding += boughtAssets;

                        // Record the trade
                        var trade = new GridTrade
                        {
                            GridLevel = level.Level,
                            Price = level.Price,
                            Size = level.OrderSize,
                            Direction = OrderSide.Buy,
                            Timestamp = candle.Timestamp
                        };
                        result.Trades.Add(trade);

                        // Mark this level as filled
                        filledLevels.Add(level.Level);

                        // In a grid strategy, we now place a sell order at the next grid level up
                        if (level.Level + 1 < gridLevels.Count)
                        {
                            var nextLevel = gridLevels[level.Level + 1];
                            // Create a copy to avoid modifying the original grid level
                            var sellLevel = new GridLevel
                            {
                                Level = nextLevel.Level,
                                Price = nextLevel.Price,
                                OrderSide = OrderSide.Sell,
                                OrderSize = boughtAssets * nextLevel.Price // Sell the same amount we bought
                            };
                            activeSellOrders.Add(sellLevel);
                        }
                    }
                }

                // Check if any grid sell orders were filled (price went up crossing a grid level)
                foreach (var level in activeSellOrders.ToList())
                {
                    if (candle.High >= level.Price && !filledLevels.Contains(level.Level))
                    {
                        // Sell order executed
                        decimal soldAssets = level.OrderSize / level.Price;
                        decimal receivedCapital = level.OrderSize;

                        // Update balances
                        capital += receivedCapital;
                        assetHolding -= soldAssets;

                        // Record the trade
                        var trade = new GridTrade
                        {
                            GridLevel = level.Level,
                            Price = level.Price,
                            Size = level.OrderSize,
                            Direction = OrderSide.Sell,
                            Timestamp = candle.Timestamp
                        };
                        result.Trades.Add(trade);

                        // Mark this level as filled
                        filledLevels.Add(level.Level);

                        // In a grid strategy, we now place a buy order at the next grid level down
                        if (level.Level - 1 >= 0)
                        {
                            var prevLevel = gridLevels[level.Level - 1];
                            // Create a copy to avoid modifying the original grid level
                            var buyLevel = new GridLevel
                            {
                                Level = prevLevel.Level,
                                Price = prevLevel.Price,
                                OrderSide = OrderSide.Buy,
                                OrderSize = receivedCapital // Buy using the capital we received
                            };
                            activeBuyOrders.Add(buyLevel);
                        }
                    }
                }
            }

            // Calculate final equity (capital + asset value at final price)
            decimal finalAssetValue = assetHolding * candles.Last().Close;
            result.FinalEquity = capital + finalAssetValue;
            result.TotalProfit = result.FinalEquity - initialCapital;
            result.TotalProfitPercentage = result.TotalProfit / initialCapital * 100;
            result.TotalTrades = result.Trades.Count;

            return result;
        }
    }
}