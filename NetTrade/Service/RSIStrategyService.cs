using Alpaca.Markets;
using NLog;
using NetTrade.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NetTrade.Helpers;
using ILogger = NLog.ILogger;

namespace NetTrade.Service
{
    public interface IRSIStrategyService
    {
        Task<RSIBacktestResult> RunBacktest(RSIBacktestRequest request);
        Task<List<RSICandleData>> GetHistoricalData(RSIStrategyConfiguration config);
    }

    public class RSIStrategyService : IRSIStrategyService
    {
        private readonly IAlpacaCryptoDataClient _alpacaCryptoDataClient;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public RSIStrategyService(IAlpacaCryptoDataClient alpacaCryptoDataClient)
        {
            _alpacaCryptoDataClient = alpacaCryptoDataClient;
        }

        public async Task<RSIBacktestResult> RunBacktest(RSIBacktestRequest request)
        {
            try
            {
                _logger.Info("Running RSI backtest for {Symbol} from {StartDate} to {EndDate}",
                    request.Configuration.Symbol, request.Configuration.StartDate, request.Configuration.EndDate);

                // Convert Timeframe string to BarTimeFrame if not already set
                if (string.IsNullOrEmpty(request.Configuration.Timeframe) == false)
                {
                    request.Configuration.BarTimeframe = GetTimeFrameFromString(request.Configuration.Timeframe);
                    _logger.Info("Converted Timeframe '{Timeframe}' to BarTimeFrame", request.Configuration.Timeframe);
                }

                // Get historical data with chunking
                var candles = await GetHistoricalData(request.Configuration);

                if (candles.Count == 0)
                {
                    _logger.Warn("No data available for the given date range");
                    return new RSIBacktestResult
                    {
                        InitialCapital = request.InitialCapital,
                        FinalEquity = request.InitialCapital,
                        TotalTrades = 0,
                        Configuration = request.Configuration,
                        StartDate = request.Configuration.StartDate,
                        EndDate = request.Configuration.EndDate,
                        WinRate = 0.00M
                    };
                }

                // Calculate RSI indicator
                CalculateRSI(candles, request.Configuration.RSIPeriod);

                // Generate signals based on RSI levels
                GenerateSignals(candles, request.Configuration);

                // Run backtest
                var result = BacktestStrategy(candles, request.InitialCapital, request.Configuration);
                
                _logger.Info("RSI backtest completed with {TotalTrades} trades, Win Rate: {WinRate}%, Profit: {Profit}",
                    result.TotalTrades, result.WinRate, result.TotalProfit);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error running RSI backtest");
                throw;
            }
        }

        public async Task<List<RSICandleData>> GetHistoricalData(RSIStrategyConfiguration config)
        {
            try
            {
                _logger.Info("Fetching historical data for {Symbol} from {StartDate} to {EndDate}",
                    config.Symbol, config.StartDate, config.EndDate);

                // Ensure BarTimeframe is set from Timeframe string if needed
                if (string.IsNullOrEmpty(config.Timeframe) == false)
                {
                    config.BarTimeframe = GetTimeFrameFromString(config.Timeframe);
                    _logger.Info("Set BarTimeframe from Timeframe '{Timeframe}'", config.Timeframe);
                }

                // Create chunks of date ranges to handle large requests
                var chunks = Extensions.CreateDateChunks(config.StartDate, config.EndDate, TimeSpan.FromDays(7));
                _logger.Info("Split date range into {ChunkCount} chunks for processing", chunks.Count);

                var allCandles = new List<RSICandleData>();

                // Format symbol for Alpaca
                string symbol = config.Symbol.FormatSymbolForAlpaca();

                // Process each date chunk
                for (int i = 0; i < chunks.Count; i++)
                {
                    var chunk = chunks[i];
                    _logger.Info("Processing chunk {Current}/{Total}: {Start:yyyy-MM-dd} to {End:yyyy-MM-dd}",
                        i + 1, chunks.Count, chunk.Start, chunk.End);

                    try
                    {
                        // Create request for historical bars
                        var barsRequest = new HistoricalCryptoBarsRequest(
                            symbol,
                            chunk.Start,
                            chunk.End,
                            config.BarTimeframe
                        );

                        // Fetch historical data with pagination
                        var barsResponse = await _alpacaCryptoDataClient.GetCryptoHistoricalDataAsync(barsRequest);
                        
                        if (barsResponse?.Items != null && barsResponse.Items.Any())
                        {
                            // Convert bars to candle data
                            var chunkCandles = barsResponse.Items
                                .SelectMany(kvp => kvp.Value)
                                .OrderBy(b => b.TimeUtc)
                                .Select(bar => new RSICandleData
                                {
                                    Timestamp = bar.TimeUtc,
                                    Open = bar.Open,
                                    High = bar.High,
                                    Low = bar.Low,
                                    Close = bar.Close,
                                    Volume = bar.Volume
                                })
                                .ToList();

                            allCandles.AddRange(chunkCandles);
                            _logger.Info("Added {Count} candles from chunk {ChunkNumber}. Total: {Total}",
                                chunkCandles.Count, i + 1, allCandles.Count);
                        }
                        else
                        {
                            _logger.Warn("No data returned for chunk {ChunkNumber}", i + 1);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Error processing chunk {ChunkNumber}", i + 1);
                        // Continue with next chunk instead of failing entire backtest
                    }
                }

                _logger.Info("Retrieved {TotalCandles} total candles for {Symbol}", allCandles.Count, config.Symbol);
                return allCandles;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error fetching historical data");
                throw;
            }
        }

        private void CalculateRSI(List<RSICandleData> candles, int period)
        {
            if (candles.Count < period + 1)
            {
                _logger.Warn("Not enough candles to calculate RSI. Need at least {Required}, got {Actual}",
                    period + 1, candles.Count);
                return;
            }

            // Calculate initial average gain and loss
            decimal avgGain = 0;
            decimal avgLoss = 0;

            // First calculation: simple average of gains and losses
            for (int i = 1; i <= period; i++)
            {
                decimal change = candles[i].Close - candles[i - 1].Close;
                if (change > 0)
                    avgGain += change;
                else
                    avgLoss += Math.Abs(change);
            }

            avgGain /= period;
            avgLoss /= period;

            // Calculate RSI for the first period
            if (avgLoss != 0)
            {
                decimal rs = avgGain / avgLoss;
                candles[period].RSI = 100 - (100 / (1 + rs));
            }
            else
            {
                candles[period].RSI = 100; // All gains, no losses
            }

            // Calculate RSI for remaining candles using Wilder's smoothing method
            for (int i = period + 1; i < candles.Count; i++)
            {
                decimal change = candles[i].Close - candles[i - 1].Close;
                decimal gain = change > 0 ? change : 0;
                decimal loss = change < 0 ? Math.Abs(change) : 0;

                // Wilder's smoothing: new average = (previous average * (period - 1) + current value) / period
                avgGain = (avgGain * (period - 1) + gain) / period;
                avgLoss = (avgLoss * (period - 1) + loss) / period;

                if (avgLoss != 0)
                {
                    decimal rs = avgGain / avgLoss;
                    candles[i].RSI = 100 - (100 / (1 + rs));
                }
                else
                {
                    candles[i].RSI = 100; // All gains, no losses
                }
            }

            _logger.Info("Calculated RSI for {CandleCount} candles", candles.Count);
        }

        private void GenerateSignals(List<RSICandleData> candles, RSIStrategyConfiguration config)
        {
            // Skip candles that don't have RSI values
            int startIndex = config.RSIPeriod;
            
            for (int i = startIndex; i < candles.Count; i++)
            {
                var candle = candles[i];
                var prevCandle = i > 0 ? candles[i - 1] : null;

                // Default: no signal
                candle.Signal = 0;

                if (candle.RSI == null)
                    continue;

                // Only generate buy signals for mean reversion strategy
                // Exit conditions (overbought) will be checked in backtest logic when we have a position
                if (prevCandle != null && prevCandle.RSI != null)
                {
                    // Buy when RSI crosses below oversold level (entering oversold territory)
                    if (prevCandle.RSI.Value >= config.OversoldLevel && 
                        candle.RSI.Value < config.OversoldLevel)
                    {
                        candle.Signal = 1; // Buy signal
                    }
                    // Alternative: Buy when RSI is oversold and starts rising
                    else if (candle.RSI.Value < config.OversoldLevel && 
                             candle.RSI.Value > prevCandle.RSI.Value)
                    {
                        candle.Signal = 1; // Buy signal
                    }
                }
            }

            _logger.Info("Generated signals for {CandleCount} candles", candles.Count);
        }

        private RSIBacktestResult BacktestStrategy(List<RSICandleData> candles, decimal initialCapital, RSIStrategyConfiguration config)
        {
            decimal capital = initialCapital;
            decimal assetHoldings = 0;
            int position = 0; // 0: no position, 1: long, -1: short
            decimal entryPrice = 0;
            DateTime entryTime = DateTime.MinValue;
            decimal? rsiAtEntry = null;

            var trades = new List<RSITrade>();
            decimal maxEquity = initialCapital;
            decimal maxDrawdown = 0;
            decimal maxDrawdownPercent = 0;

            for (int i = 0; i < candles.Count; i++)
            {
                var candle = candles[i];
                decimal currentPrice = candle.Close;
                decimal equity = capital + (assetHoldings * currentPrice);

                // Update max equity and drawdown
                if (equity > maxEquity)
                {
                    maxEquity = equity;
                }

                decimal drawdown = maxEquity - equity;
                decimal drawdownPercent = maxEquity > 0 ? (drawdown / maxEquity) * 100 : 0;

                if (drawdown > maxDrawdown)
                {
                    maxDrawdown = drawdown;
                    maxDrawdownPercent = drawdownPercent;
                }

                candle.Equity = equity;

                // Exit existing position if stop loss, take profit, or overbought RSI is hit
                if (position != 0)
                {
                    decimal pnlPercent = 0;
                    if (position == 1) // Long position
                    {
                        pnlPercent = ((currentPrice - entryPrice) / entryPrice) * 100;
                        bool shouldExit = false;
                        string exitReason = "";
                        
                        // Check stop loss
                        if (pnlPercent <= -config.StopLossPercent)
                        {
                            shouldExit = true;
                            exitReason = "STOP_LOSS";
                        }
                        // Check take profit
                        else if (pnlPercent >= config.TakeProfitPercent)
                        {
                            shouldExit = true;
                            exitReason = "TAKE_PROFIT";
                        }
                        // Check overbought RSI (mean reversion exit)
                        else if (candle.RSI != null && candle.RSI.Value > config.OverboughtLevel)
                        {
                            shouldExit = true;
                            exitReason = "OVERBOUGHT";
                        }
                        
                        if (shouldExit)
                        {
                            // Exit long position (sell)
                            decimal exitValue = assetHoldings * currentPrice;
                            capital += exitValue;
                            
                            var trade = new RSITrade
                            {
                                EntryTime = entryTime,
                                EntryPrice = entryPrice,
                                ExitTime = candle.Timestamp,
                                ExitPrice = currentPrice,
                                Direction = "LONG",
                                Size = assetHoldings,
                                PnL = exitValue - (assetHoldings * entryPrice),
                                PnLPct = pnlPercent,
                                Result = pnlPercent > 0 ? "WIN" : (pnlPercent < 0 ? "LOSS" : ""),
                                Equity = equity,
                                RSIAtEntry = rsiAtEntry,
                                RSIAtExit = candle.RSI
                            };
                            
                            trades.Add(trade);
                            assetHoldings = 0;
                            position = 0;
                            candle.Position = 0;
                            candle.ExitPrice = currentPrice;
                            candle.PnL = trade.PnL;
                            
                            _logger.Debug("Exited long position at {Price}, PnL: {PnL}%, Reason: {Reason}, RSI: {RSI}",
                                currentPrice, pnlPercent, exitReason, candle.RSI);
                        }
                    }
                }

                // Enter new position based on buy signal (mean reversion: buy when oversold)
                if (position == 0 && candle.Signal == 1) // Only enter on buy signals
                {
                    // Calculate position size based on risk, but cap to available capital
                    decimal stopLossPrice = currentPrice * (1 - config.StopLossPercent / 100);
                    decimal riskPerUnit = currentPrice - stopLossPrice;
                    
                    if (riskPerUnit > 0 && capital > 0)
                    {
                        // Calculate maximum position size based on risk
                        decimal riskAmount = capital * config.RiskPerTrade;
                        decimal maxPositionSizeByRisk = riskAmount / riskPerUnit;
                        
                        // Calculate maximum position size based on available capital
                        decimal maxPositionSizeByCapital = capital / currentPrice;
                        
                        // Use the smaller of the two to respect both risk and capital constraints
                        decimal positionSize = Math.Min(maxPositionSizeByRisk, maxPositionSizeByCapital);
                        
                        // Only enter if we can afford at least a minimal position
                        if (positionSize > 0)
                        {
                            decimal cost = positionSize * currentPrice;
                            
                            // Ensure we don't exceed capital (safety check)
                            if (cost > capital)
                            {
                                positionSize = maxPositionSizeByCapital;
                                cost = positionSize * currentPrice;
                            }
                            
                            if (cost <= capital && positionSize > 0)
                            {
                                capital -= cost;
                                assetHoldings = positionSize;
                                position = 1;
                                entryPrice = currentPrice;
                                entryTime = candle.Timestamp;
                                rsiAtEntry = candle.RSI;
                                
                                candle.Position = 1;
                                candle.EntryPrice = currentPrice;
                                
                                _logger.Debug("Entered long position at {Price}, Size: {Size}, Cost: {Cost}, RSI: {RSI}",
                                    currentPrice, positionSize, cost, candle.RSI);
                            }
                        }
                    }
                }
            }

            // Close any remaining open positions at final price
            if (position != 0 && candles.Count > 0)
            {
                var lastCandle = candles.Last();
                decimal finalPrice = lastCandle.Close;
                decimal exitValue = assetHoldings * finalPrice;
                capital += exitValue;
                
                decimal pnlPercent = position == 1 
                    ? ((finalPrice - entryPrice) / entryPrice) * 100 
                    : 0;
                
                var finalTrade = new RSITrade
                {
                    EntryTime = entryTime,
                    EntryPrice = entryPrice,
                    ExitTime = lastCandle.Timestamp,
                    ExitPrice = finalPrice,
                    Direction = position == 1 ? "LONG" : "SHORT",
                    Size = assetHoldings,
                    PnL = exitValue - (assetHoldings * entryPrice),
                    PnLPct = pnlPercent,
                    Result = pnlPercent > 0 ? "WIN" : (pnlPercent < 0 ? "LOSS" : ""),
                    Equity = capital,
                    RSIAtEntry = rsiAtEntry,
                    RSIAtExit = lastCandle.RSI
                };
                
                trades.Add(finalTrade);
            }

            // Calculate final statistics
            decimal finalEquity = capital + (assetHoldings * (candles.Count > 0 ? candles.Last().Close : 0));
            decimal totalProfit = finalEquity - initialCapital;
            decimal totalProfitPercent = initialCapital > 0 ? (totalProfit / initialCapital) * 100 : 0;

            var winningTrades = trades.Where(t => t.Result == "WIN").ToList();
            var losingTrades = trades.Where(t => t.Result == "LOSS").ToList();

            decimal winRate = trades.Count > 0 ? (winningTrades.Count / (decimal)trades.Count) * 100 : 0;
            decimal avgWin = winningTrades.Count > 0 ? winningTrades.Average(t => t.PnL) : 0;
            decimal avgLoss = losingTrades.Count > 0 ? Math.Abs(losingTrades.Average(t => t.PnL)) : 0;
            decimal profitFactor = avgLoss > 0 ? avgWin / avgLoss : (avgWin > 0 ? 999 : 0);

            return new RSIBacktestResult
            {
                InitialCapital = initialCapital,
                FinalEquity = finalEquity,
                TotalProfit = totalProfit,
                TotalProfitPercentage = totalProfitPercent,
                TotalTrades = trades.Count,
                WinningTrades = winningTrades.Count,
                LosingTrades = losingTrades.Count,
                WinRate = winRate,
                AverageWin = avgWin,
                AverageLoss = avgLoss,
                ProfitFactor = profitFactor,
                MaxDrawdown = maxDrawdown,
                MaxDrawdownPercent = maxDrawdownPercent,
                StartDate = config.StartDate,
                EndDate = config.EndDate,
                Trades = trades,
                Configuration = config
            };
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
                _ => new BarTimeFrame(15, BarTimeFrameUnit.Minute) // Default to 15Min
            };
        }
    }
}

