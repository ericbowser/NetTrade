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
    public interface IMovingAverageCrossoverStrategyService
    {
        Task<MovingAverageCrossoverBacktestResult> RunBacktest(MovingAverageCrossoverBacktestRequest request);
        Task<List<MovingAverageCrossoverCandleData>> GetHistoricalData(MovingAverageCrossoverStrategyConfiguration config);
    }

    public class MovingAverageCrossoverStrategyService : IMovingAverageCrossoverStrategyService
    {
        private readonly IAlpacaCryptoDataClient _alpacaCryptoDataClient;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public MovingAverageCrossoverStrategyService(IAlpacaCryptoDataClient alpacaCryptoDataClient)
        {
            _alpacaCryptoDataClient = alpacaCryptoDataClient;
        }

        public async Task<MovingAverageCrossoverBacktestResult> RunBacktest(MovingAverageCrossoverBacktestRequest request)
        {
            try
            {
                _logger.Info("Running Moving Average Crossover backtest for {Symbol} from {StartDate} to {EndDate}",
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
                    return new MovingAverageCrossoverBacktestResult
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

                // Calculate Moving Averages
                CalculateMovingAverages(candles, request.Configuration.FastPeriod, request.Configuration.SlowPeriod);

                // Generate signals based on crossovers
                GenerateSignals(candles, request.Configuration);

                // Run backtest
                var result = BacktestStrategy(candles, request.InitialCapital, request.Configuration);
                
                _logger.Info("Moving Average Crossover backtest completed with {TotalTrades} trades, Win Rate: {WinRate}%, Profit: {Profit}",
                    result.TotalTrades, result.WinRate, result.TotalProfit);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error running Moving Average Crossover backtest");
                throw;
            }
        }

        public async Task<List<MovingAverageCrossoverCandleData>> GetHistoricalData(MovingAverageCrossoverStrategyConfiguration config)
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

                var allCandles = new List<MovingAverageCrossoverCandleData>();

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
                                .Select(bar => new MovingAverageCrossoverCandleData
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

        private void CalculateMovingAverages(List<MovingAverageCrossoverCandleData> candles, int fastPeriod, int slowPeriod)
        {
            int maxPeriod = Math.Max(fastPeriod, slowPeriod);
            
            if (candles.Count < maxPeriod)
            {
                _logger.Warn("Not enough candles to calculate Moving Averages. Need at least {Required}, got {Actual}",
                    maxPeriod, candles.Count);
                return;
            }

            // Calculate Fast MA
            for (int i = fastPeriod - 1; i < candles.Count; i++)
            {
                var periodCandles = candles.Skip(i - fastPeriod + 1).Take(fastPeriod).ToList();
                candles[i].FastMA = periodCandles.Average(c => c.Close);
            }

            // Calculate Slow MA
            for (int i = slowPeriod - 1; i < candles.Count; i++)
            {
                var periodCandles = candles.Skip(i - slowPeriod + 1).Take(slowPeriod).ToList();
                candles[i].SlowMA = periodCandles.Average(c => c.Close);
            }

            _logger.Info("Calculated Moving Averages (Fast: {FastPeriod}, Slow: {SlowPeriod}) for {CandleCount} candles",
                fastPeriod, slowPeriod, candles.Count);
        }

        private void GenerateSignals(List<MovingAverageCrossoverCandleData> candles, MovingAverageCrossoverStrategyConfiguration config)
        {
            // Skip candles that don't have both MAs
            int startIndex = Math.Max(config.FastPeriod, config.SlowPeriod);
            
            for (int i = startIndex; i < candles.Count; i++)
            {
                var candle = candles[i];
                var prevCandle = i > 0 ? candles[i - 1] : null;

                // Default: no signal
                candle.Signal = 0;

                if (candle.FastMA == null || candle.SlowMA == null)
                    continue;

                if (prevCandle != null && prevCandle.FastMA != null && prevCandle.SlowMA != null)
                {
                    // Golden Cross: Fast MA crosses above Slow MA (buy signal)
                    if (prevCandle.FastMA.Value <= prevCandle.SlowMA.Value && 
                        candle.FastMA.Value > candle.SlowMA.Value)
                    {
                        candle.Signal = 1; // Buy signal - golden cross
                    }
                    // Death Cross: Fast MA crosses below Slow MA (sell signal)
                    else if (prevCandle.FastMA.Value >= prevCandle.SlowMA.Value && 
                             candle.FastMA.Value < candle.SlowMA.Value)
                    {
                        candle.Signal = -1; // Sell signal - death cross
                    }
                }
            }

            _logger.Info("Generated signals for {CandleCount} candles", candles.Count);
        }

        private MovingAverageCrossoverBacktestResult BacktestStrategy(List<MovingAverageCrossoverCandleData> candles, decimal initialCapital, MovingAverageCrossoverStrategyConfiguration config)
        {
            decimal capital = initialCapital;
            decimal assetHoldings = 0;
            int position = 0; // 0: no position, 1: long
            decimal entryPrice = 0;
            DateTime entryTime = DateTime.MinValue;
            decimal? fastMAAtEntry = null;
            decimal? slowMAAtEntry = null;

            var trades = new List<MovingAverageCrossoverTrade>();
            decimal maxEquity = initialCapital;
            decimal maxDrawdown = 0;
            decimal maxDrawdownPercent = 0;

            for (int i = 0; i < candles.Count; i++)
            {
                var candle = candles[i];
                decimal currentPrice = candle.Close;

                // Exit existing position if stop loss, take profit, or death cross is hit
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
                        // Check death cross (sell signal)
                        else if (candle.Signal == -1)
                        {
                            shouldExit = true;
                            exitReason = "DEATH_CROSS";
                        }
                        
                        if (shouldExit)
                        {
                            // Exit long position (sell)
                            decimal exitValue = assetHoldings * currentPrice;
                            capital += exitValue;
                            
                            // Calculate equity after exit
                            decimal equityAfterExit = capital + (0 * currentPrice); // assetHoldings will be 0 after exit
                            
                            var trade = new MovingAverageCrossoverTrade
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
                                Equity = equityAfterExit,
                                FastMAAtEntry = fastMAAtEntry,
                                SlowMAAtEntry = slowMAAtEntry,
                                FastMAAtExit = candle.FastMA,
                                SlowMAAtExit = candle.SlowMA
                            };
                            
                            trades.Add(trade);
                            assetHoldings = 0;
                            position = 0;
                            candle.Position = 0;
                            candle.ExitPrice = currentPrice;
                            candle.PnL = trade.PnL;
                            
                            _logger.Debug("Exited long position at {Price}, PnL: {PnL}%, Reason: {Reason}, Equity: {Equity}",
                                currentPrice, pnlPercent, exitReason, equityAfterExit);
                        }
                    }
                }

                // Enter new position based on golden cross (buy signal)
                if (position == 0 && candle.Signal == 1) // Only enter on golden cross
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
                                fastMAAtEntry = candle.FastMA;
                                slowMAAtEntry = candle.SlowMA;
                                
                                candle.Position = 1;
                                candle.EntryPrice = currentPrice;
                                
                            _logger.Debug("Entered long position at {Price}, Size: {Size}, Cost: {Cost} (Golden Cross)",
                                currentPrice, positionSize, cost);
                            }
                        }
                    }
                }

                // Calculate equity AFTER processing exits and entries
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
                
                var finalTrade = new MovingAverageCrossoverTrade
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
                    FastMAAtEntry = fastMAAtEntry,
                    SlowMAAtEntry = slowMAAtEntry,
                    FastMAAtExit = lastCandle.FastMA,
                    SlowMAAtExit = lastCandle.SlowMA
                };
                
                trades.Add(finalTrade);
                
                // Reset position after closing
                assetHoldings = 0;
                position = 0;
            }

            // Calculate final statistics
            // After closing final position, assetHoldings should be 0, so finalEquity = capital
            decimal finalEquity = capital + (assetHoldings * (candles.Count > 0 ? candles.Last().Close : 0));
            decimal totalProfit = finalEquity - initialCapital;
            decimal totalProfitPercent = initialCapital > 0 ? (totalProfit / initialCapital) * 100 : 0;

            // Validation: Sum of all trade PnLs should approximately equal total profit
            decimal sumOfTradePnLs = trades.Sum(t => t.PnL);
            decimal difference = Math.Abs(totalProfit - sumOfTradePnLs);
            
            if (difference > 0.01m) // Allow small rounding differences
            {
                _logger.Warn("Potential calculation error: Total profit ({TotalProfit}) doesn't match sum of trade PnLs ({SumPnLs}). Difference: {Difference}",
                    totalProfit, sumOfTradePnLs, difference);
            }
            
            _logger.Info("Final equity: {FinalEquity}, Initial capital: {InitialCapital}, Total profit: {TotalProfit}, Sum of trade PnLs: {SumPnLs}",
                finalEquity, initialCapital, totalProfit, sumOfTradePnLs);

            var winningTrades = trades.Where(t => t.Result == "WIN").ToList();
            var losingTrades = trades.Where(t => t.Result == "LOSS").ToList();

            decimal winRate = trades.Count > 0 ? (winningTrades.Count / (decimal)trades.Count) * 100 : 0;
            decimal avgWin = winningTrades.Count > 0 ? winningTrades.Average(t => t.PnL) : 0;
            decimal avgLoss = losingTrades.Count > 0 ? Math.Abs(losingTrades.Average(t => t.PnL)) : 0;
            decimal profitFactor = avgLoss > 0 ? avgWin / avgLoss : (avgWin > 0 ? 999 : 0);

            return new MovingAverageCrossoverBacktestResult
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
                _ => BarTimeFrame.Hour // Default to 1Hour for trend following
            };
        }
    }
}

