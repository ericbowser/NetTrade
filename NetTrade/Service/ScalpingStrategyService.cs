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
    public interface IScalpingStrategyService
    {
        Task<BacktestResult> RunBackTest(BacktestRequest request);
        Task<List<CandleData>> GetHistoricalData(ScalpingStrategyConfiguration config);
    }

    public class ScalpingStrategyService : IScalpingStrategyService
    {
        private readonly IAlpacaCryptoDataClient _alpacaCryptoDataClient;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public ScalpingStrategyService(IAlpacaCryptoDataClient alpacaCryptoDataClient)
        {
            _alpacaCryptoDataClient = alpacaCryptoDataClient;
        }

        /// <summary>
        /// Run backtests using historical data through Alpaca.markets
        /// </summary>
        /// <param name="request">BacktestRequest request</param>
        /// <returns>Task<BacktestResult> backtestResult</returns>
        public async Task<BacktestResult> RunBackTest(BacktestRequest request)
        {
            try
            {
                _logger.Info("Running backtest for {Symbol} from {StartDate} to {EndDate}",
                    request.Configuration.Symbol, request.Configuration.StartDate, request.Configuration.EndDate);

                // Get historical data with chunking
                var candles = await GetHistoricalData(request.Configuration);

                if (candles.Count == 0)
                {
                    _logger.Warn("No data available for the given date range");
                    return new BacktestResult
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

                // Calculate indicators
                CalculateIndicators(candles, request.Configuration);

                // Generate signals
                GenerateSignals(candles);

                // Run backtest
                var result = BacktestStrategy(candles, request.InitialCapital, request.Configuration);
                
                _logger.Info("Backtest completed with {TotalTrades} trades, Win Rate: {WinRate}%, Profit: {Profit}",
                    result.TotalTrades, result.WinRate, result.TotalProfit);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error running backtest");
                throw;
            }
        }

        public async Task<List<CandleData>> GetHistoricalData(ScalpingStrategyConfiguration config)
        {
            try
            {
                _logger.Info("Fetching historical data for {Symbol} from {StartDate} to {EndDate}",
                    config.Symbol, config.StartDate, config.EndDate);

                // Create chunks of date ranges to handle large requests
                var chunks = Extensions.CreateDateChunks(config.StartDate, config.EndDate, TimeSpan.FromDays(7));
                _logger.Info("Split date range into {ChunkCount} chunks for processing", chunks.Count);

                var allCandles = new List<CandleData>();

                // Process each date chunk
                for (int i = 0; i < chunks.Count; i++)
                {
                    var chunk = chunks[i];
                    _logger.Info("Processing chunk {Current}/{Total}: {Start:yyyy-MM-dd} to {End:yyyy-MM-dd}",
                        i + 1, chunks.Count, chunk.Start, chunk.End);

                    // Create request for historical bars
                    var barsRequest = new HistoricalCryptoBarsRequest(
                        config.Symbol, 
                        chunk.Start, 
                        chunk.End, 
                        config.BarTimeframe);

                    // Get historical bars from Alpaca
                    var chunkCandles = new List<CandleData>();
                    var barsResponse = await _alpacaCryptoDataClient.GetCryptoHistoricalDataAsync(barsRequest);

                    if (barsResponse != null && barsResponse.Items.Any())
                    {
                        // Convert bars to candle data
                        foreach (var bar in barsResponse.Items.FirstOrDefault().Value)
                        {
                            chunkCandles.Add(new CandleData
                            {
                                Timestamp = bar.TimeUtc,
                                Open = bar.Open,
                                High = bar.High,
                                Low = bar.Low,
                                Close = bar.Close,
                                Volume = bar.Volume
                            });
                        }
                        
                        _logger.Info("Retrieved {CandleCount} candles for chunk {ChunkNum}",
                            chunkCandles.Count, i + 1);
                        
                        // Process pagination if needed
                        string? pageToken = barsResponse.NextPageToken;
                        int pageCount = 1;
                        int maxPages = 10; // Safety limit
                        
                        while (!string.IsNullOrEmpty(pageToken) && pageCount < maxPages)
                        {
                            barsRequest.Pagination.Token = pageToken;
                            var pageResponse = await _alpacaCryptoDataClient.GetCryptoHistoricalDataAsync(barsRequest);
                            
                            if (pageResponse.Items.Any())
                            {
                                foreach (var bar in pageResponse.Items.FirstOrDefault().Value)
                                {
                                    chunkCandles.Add(new CandleData
                                    {
                                        Timestamp = bar.TimeUtc,
                                        Open = bar.Open,
                                        High = bar.High,
                                        Low = bar.Low,
                                        Close = bar.Close,
                                        Volume = bar.Volume
                                    });
                                }
                                
                                pageToken = pageResponse.NextPageToken;
                                pageCount++;
                                
                                _logger.Info("Retrieved {AdditionalCount} additional candles on page {PageNum}",
                                    pageResponse.Items.FirstOrDefault().Value.Count, pageCount);
                            }
                            else
                            {
                                break;
                            }
                        }
                        
                        allCandles?.AddRange(chunkCandles);
                    }
                    else
                    {
                        _logger.Warn("No data available for chunk {ChunkNum}, skipping", i + 1);
                    }
                }

                // Sort candles by timestamp to ensure correct order
                allCandles = allCandles.OrderBy(c => c.Timestamp).ToList();

                // Apply Heikin-Ashi transformation if requested
                if (config.UseHeikinAshi)
                {
                    allCandles = CalculateHeikinAshi(allCandles);
                    _logger.Info("Applied Heikin-Ashi transformation to {CandleCount} candles", allCandles.Count);
                }

                return allCandles;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error fetching historical data");
                throw;
            }
        }

        private List<CandleData> CalculateHeikinAshi(List<CandleData> candles)
        {
            var haCandles = new List<CandleData>();

            for (int i = 0; i < candles.Count; i++)
            {
                var candle = candles[i];
                var haCandle = new CandleData
                {
                    Timestamp = candle.Timestamp,
                    Volume = candle.Volume
                };

                if (i == 0)
                {
                    // First candle
                    haCandle.Open = candle.Open;
                    haCandle.Close = (candle.Open + candle.High + candle.Low + candle.Close) / 4;
                    haCandle.High = candle.High;
                    haCandle.Low = candle.Low;
                }
                else
                {
                    // Calculate Heikin-Ashi values
                    var prevHaCandle = haCandles[i - 1];
                    haCandle.Open = (prevHaCandle.Open + prevHaCandle.Close) / 2;
                    haCandle.Close = (candle.Open + candle.High + candle.Low + candle.Close) / 4;
                    haCandle.High = new[] { candle.High, haCandle.Open, haCandle.Close }.Max();
                    haCandle.Low = new[] { candle.Low, haCandle.Open, haCandle.Close }.Min();
                }

                haCandles.Add(haCandle);
            }

            return haCandles;
        }

        private void CalculateIndicators(List<CandleData> candles, ScalpingStrategyConfiguration config)
        {
            // Calculate SMA
            CalculateSMA(candles, config.SmaPeriod);

            // Calculate MACD
            CalculateMACD(candles, config.MacdFastPeriod, config.MacdSlowPeriod, config.MacdSignalPeriod);
            
            _logger.Info("Calculated indicators for {CandleCount} candles", candles.Count);
        }

        private void CalculateSMA(List<CandleData> candles, int period)
        {
            for (int i = 0; i < candles.Count; i++)
            {
                if (i >= period - 1)
                {
                    decimal sum = 0;
                    for (int j = i - (period - 1); j <= i; j++)
                    {
                        sum += candles[j].Close;
                    }

                    candles[i].Sma200 = sum / period;
                }
            }
        }

        private void CalculateEMA(List<CandleData> candles, int period, Func<CandleData, decimal> valueSelector,
            Action<CandleData, decimal> resultSetter)
        {
            // Skip if not enough data
            if (candles.Count < period)
                return;

            // First value is SMA
            decimal sum = 0;
            for (int i = 0; i < period; i++)
            {
                sum += valueSelector(candles[i]);
            }

            decimal ema = sum / period;
            resultSetter(candles[period - 1], ema);

            // Calculate EMA for the rest
            decimal multiplier = 2m / (period + 1);
            for (int i = period; i < candles.Count; i++)
            {
                // EMA = (Close - Previous EMA) * multiplier + Previous EMA
                ema = (valueSelector(candles[i]) - ema) * multiplier + ema;
                resultSetter(candles[i], ema);
            }
        }

        private void CalculateMACD(List<CandleData> candles, int fastPeriod, int slowPeriod, int signalPeriod)
        {
            // Temporary storage for EMAs
            var fastEma = new Dictionary<int, decimal>();
            var slowEma = new Dictionary<int, decimal>();
            var macdLine = new Dictionary<int, decimal>();
            var signalLine = new Dictionary<int, decimal>();

            // Calculate Fast EMA
            CalculateEMA(
                candles,
                fastPeriod,
                c => c.Close,
                (c, v) => fastEma[candles.IndexOf(c)] = v
            );

            // Calculate Slow EMA
            CalculateEMA(
                candles,
                slowPeriod,
                c => c.Close,
                (c, v) => slowEma[candles.IndexOf(c)] = v
            );

            // Calculate MACD Line (Fast EMA - Slow EMA)
            // Start from the point where both EMAs are available
            int startIndex = Math.Max(fastPeriod, slowPeriod) - 1;
            for (int i = startIndex; i < candles.Count; i++)
            {
                if (fastEma.ContainsKey(i) && slowEma.ContainsKey(i))
                {
                    macdLine[i] = fastEma[i] - slowEma[i];
                    candles[i].Macd = macdLine[i];
                }
            }

            // Create a sublist for signal line calculation
            var macdValues = new List<CandleData>();
            for (int i = startIndex; i < candles.Count; i++)
            {
                if (macdLine.ContainsKey(i))
                {
                    var candleWithMacd = new CandleData
                    {
                        Close = macdLine[i] // Use MACD as "close" for EMA calculation
                    };
                    macdValues.Add(candleWithMacd);
                }
            }

            // Calculate Signal Line (EMA of MACD Line)
            CalculateEMA(
                macdValues,
                signalPeriod,
                c => c.Close,
                (c, v) =>
                {
                    int originalIndex = startIndex + macdValues.IndexOf(c);
                    signalLine[originalIndex] = v;
                    candles[originalIndex].MacdSignal = v;
                }
            );

            // Calculate Histogram (MACD Line - Signal Line)
            for (int i = 0; i < candles.Count; i++)
            {
                if (macdLine.ContainsKey(i) && signalLine.ContainsKey(i))
                {
                    candles[i].MacdHistogram = macdLine[i] - signalLine[i];
                }
            }
        }

        private void GenerateSignals(List<CandleData> candles)
        {
            // Skip candles that don't have complete indicator values
            int startIndex = 0;
            while (startIndex < candles.Count &&
                   (candles[startIndex].Sma200 == null ||
                    candles[startIndex].Macd == null ||
                    candles[startIndex].MacdSignal == null ||
                    candles[startIndex].MacdHistogram == null))
            {
                startIndex++;
            }

            // Generate signals based on indicators
            for (int i = startIndex; i < candles.Count; i++)
            {
                var candle = candles[i];
                var prevCandle = i > 0 ? candles[i - 1] : null;

                // Default signal = 0 (no signal)
                candle.Signal = 0;

                // Determine trend based on price vs SMA
                candle.Trend = candle.Close > candle.Sma200 ? 1 : -1;

                // Skip if we don't have previous candle or it doesn't have MACD
                if (prevCandle == null || prevCandle.MacdHistogram == null) continue;

                // Buy signal: Uptrend (price above SMA200) and MACD histogram turns positive
                if (candle.Trend == 1 &&
                    candle.MacdHistogram > 0 &&
                    prevCandle.MacdHistogram <= 0)
                {
                    candle.Signal = 1; // Buy
                }

                // Sell signal: Downtrend (price below SMA200) and MACD histogram turns negative
                else if (candle.Trend == -1 &&
                         candle.MacdHistogram < 0 &&
                         prevCandle.MacdHistogram >= 0)
                {
                    candle.Signal = -1; // Sell
                }
            }
            
            _logger.Info("Generated signals for {CandleCount} candles", candles.Count);
        }

        public BacktestResult BacktestStrategy(List<CandleData> candles, decimal initialCapital,
            ScalpingStrategyConfiguration config)
        {
            // Initialize result
            var result = new BacktestResult
            {
                InitialCapital = initialCapital,
                StartDate = candles.First().Timestamp,
                EndDate = candles.Last().Timestamp,
                Configuration = config
            };

            // Tracking variables
            decimal capital = initialCapital;
            decimal totalProfits = 0;
            decimal totalLosses = 0;
            int position = 0; // 0: no position, 1: long, -1: short
            decimal entryPrice = 0;
            Trade currentTrade = null;

            // Skip candles that don't have complete indicator values
            int startIndex = 0;
            while (startIndex < candles.Count &&
                   (candles[startIndex].Sma200 == null ||
                    candles[startIndex].Macd == null ||
                    candles[startIndex].MacdSignal == null ||
                    candles[startIndex].MacdHistogram == null))
            {
                // Store initial equity
                candles[startIndex].Equity = capital;
                startIndex++;
            }

            // Loop through valid candles
            for (int i = startIndex; i < candles.Count; i++)
            {
                var candle = candles[i];

                // Store current position and equity
                candle.Position = position;
                candle.Equity = capital;

                // Check for entry signals
                if (position == 0 && candle.Signal != 0)
                {
                    // Enter position
                    position = candle.Signal; // 1 for long, -1 for short
                    entryPrice = candle.Close;
                    candle.EntryPrice = entryPrice;

                    // Create new trade record
                    currentTrade = new Trade
                    {
                        EntryTime = candle.Timestamp,
                        EntryPrice = entryPrice,
                        Direction = position == 1 ? "LONG" : "SHORT",
                        Size = (capital * config.RiskPerTrade) / entryPrice
                    };

                    result.Trades.Add(currentTrade);
                }
                // Check for exit conditions if in a position
                else if (position != 0)
                {
                    // Calculate current PnL percentage
                    decimal pnlPct = position == 1
                        ? (candle.Close - entryPrice) / entryPrice * 100
                        : (entryPrice - candle.Close) / entryPrice * 100;

                    // Exit conditions: Take profit, stop loss, or reversal signal
                    bool takeProfit = pnlPct >= config.TakeProfitPips;
                    bool stopLoss = pnlPct <= -config.StopLossPips;
                    bool reversal = (position == 1 && candle.Signal == -1) ||
                                    (position == -1 && candle.Signal == 1);

                    if (takeProfit || stopLoss || reversal)
                    {
                        // Close position
                        decimal exitPrice = candle.Close;
                        decimal pnl = position == 1
                            ? (exitPrice - entryPrice) * currentTrade.Size
                            : (entryPrice - exitPrice) * currentTrade.Size;

                        // Update capital
                        capital += pnl;

                        // Update profit/loss trackers
                        if (pnl > 0)
                        {
                            totalProfits += pnl;
                            result.WinningTrades++;
                        }
                        else
                        {
                            totalLosses -= pnl; // Make positive for calculations
                            result.LosingTrades++;
                        }

                        // Update trade record
                        currentTrade.ExitTime = candle.Timestamp;
                        currentTrade.ExitPrice = exitPrice;
                        currentTrade.PnL = pnl;
                        currentTrade.PnLPct = pnlPct;
                        currentTrade.Result = pnl > 0 ? "WIN" : "LOSS";
                        currentTrade.Equity = capital;

                        // Record exit in candle
                        candle.ExitPrice = exitPrice;
                        candle.PnL = pnl;

                        // Check for reversal signal (enter new position in opposite direction)
                        if (reversal)
                        {
                            // Enter new position
                            position = candle.Signal;
                            entryPrice = candle.Close;
                            candle.EntryPrice = entryPrice;

                            // Create new trade record
                            currentTrade = new Trade
                            {
                                EntryTime = candle.Timestamp,
                                EntryPrice = entryPrice,
                                Direction = position == 1 ? "LONG" : "SHORT",
                                Size = (capital * config.RiskPerTrade) / entryPrice
                            };

                            result.Trades.Add(currentTrade);
                        }
                        else
                        {
                            // Reset position
                            position = 0;
                            entryPrice = 0;
                        }
                    }
                }

                // Update equity in candle
                candle.Equity = capital;
            }

            // Close any open position at the end of the backtest
            if (position != 0 && currentTrade != null)
            {
                var lastCandle = candles.Last();

                decimal exitPrice = lastCandle.Close;
                decimal pnl = position == 1
                    ? (exitPrice - entryPrice) * currentTrade.Size
                    : (entryPrice - exitPrice) * currentTrade.Size;

                decimal pnlPct = position == 1
                    ? (exitPrice - entryPrice) / entryPrice * 100
                    : (entryPrice - exitPrice) / entryPrice * 100;

                // Update capital
                capital += pnl;

                // Update profit/loss trackers
                if (pnl > 0)
                {
                    totalProfits += pnl;
                    result.WinningTrades++;
                }
                else
                {
                    totalLosses -= pnl; // Make positive for calculations
                    result.LosingTrades++;
                }

                // Update trade record
                currentTrade.ExitTime = lastCandle.Timestamp;
                currentTrade.ExitPrice = exitPrice;
                currentTrade.PnL = pnl;
                currentTrade.PnLPct = pnlPct;
                currentTrade.Result = pnl > 0 ? "WIN" : "LOSS";
                currentTrade.Equity = capital;

                // Record in candle
                lastCandle.ExitPrice = exitPrice;
                lastCandle.PnL = pnl;
                lastCandle.Equity = capital;
            }

            // Calculate final metrics
            result.FinalEquity = capital;
            result.TotalProfit = capital - initialCapital;
            result.TotalProfitPercentage = (result.TotalProfit / initialCapital) * 100;
            result.TotalTrades = result.Trades.Count;
            result.WinRate = result.TotalTrades > 0 ? (decimal)result.WinningTrades / result.TotalTrades * 100 : 0;

            // Calculate average win/loss and profit factor
            if (result.WinningTrades > 0)
                result.AverageWin = totalProfits / result.WinningTrades;

            if (result.LosingTrades > 0)
                result.AverageLoss = totalLosses / result.LosingTrades;

            result.ProfitFactor = totalLosses > 0 ? totalProfits / totalLosses : totalProfits > 0 ? decimal.MaxValue : 0;

            return result;
        }
    }
}