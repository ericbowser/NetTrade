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
    public interface IBollingerBandsStrategyService
    {
        Task<BollingerBandsBacktestResult> RunBacktest(BollingerBandsBacktestRequest request);
        Task<List<BollingerBandsCandleData>> GetHistoricalData(BollingerBandsStrategyConfiguration config);
    }

    public class BollingerBandsStrategyService : IBollingerBandsStrategyService
    {
        private readonly IAlpacaCryptoDataClient _alpacaCryptoDataClient;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public BollingerBandsStrategyService(IAlpacaCryptoDataClient alpacaCryptoDataClient)
        {
            _alpacaCryptoDataClient = alpacaCryptoDataClient;
        }

        public async Task<BollingerBandsBacktestResult> RunBacktest(BollingerBandsBacktestRequest request)
        {
            try
            {
                _logger.Info("Running Bollinger Bands backtest for {Symbol} from {StartDate} to {EndDate}",
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
                    return new BollingerBandsBacktestResult
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

                // Calculate Bollinger Bands
                CalculateBollingerBands(candles, request.Configuration.Period, request.Configuration.StandardDeviations);

                // Generate signals based on Bollinger Bands
                GenerateSignals(candles, request.Configuration);

                // Run backtest
                var result = BacktestStrategy(candles, request.InitialCapital, request.Configuration);
                
                _logger.Info("Bollinger Bands backtest completed with {TotalTrades} trades, Win Rate: {WinRate}%, Profit: {Profit}",
                    result.TotalTrades, result.WinRate, result.TotalProfit);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error running Bollinger Bands backtest");
                throw;
            }
        }

        public async Task<List<BollingerBandsCandleData>> GetHistoricalData(BollingerBandsStrategyConfiguration config)
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

                var allCandles = new List<BollingerBandsCandleData>();

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
                                .Select(bar => new BollingerBandsCandleData
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

        private void CalculateBollingerBands(List<BollingerBandsCandleData> candles, int period, decimal standardDeviations)
        {
            if (candles.Count < period)
            {
                _logger.Warn("Not enough candles to calculate Bollinger Bands. Need at least {Required}, got {Actual}",
                    period, candles.Count);
                return;
            }

            // Calculate SMA (Middle Band) and Standard Deviation for each candle
            for (int i = period - 1; i < candles.Count; i++)
            {
                // Get the period candles for calculation
                var periodCandles = candles.Skip(i - period + 1).Take(period).ToList();
                
                // Calculate SMA (Simple Moving Average) - Middle Band
                decimal sma = periodCandles.Average(c => c.Close);
                candles[i].MiddleBand = sma;

                // Calculate Standard Deviation
                decimal variance = periodCandles.Sum(c => (c.Close - sma) * (c.Close - sma)) / period;
                decimal stdDev = (decimal)Math.Sqrt((double)variance);
                candles[i].StandardDeviation = stdDev;

                // Calculate Upper and Lower Bands
                candles[i].UpperBand = sma + (stdDev * standardDeviations);
                candles[i].LowerBand = sma - (stdDev * standardDeviations);
            }

            _logger.Info("Calculated Bollinger Bands for {CandleCount} candles", candles.Count);
        }

        private void GenerateSignals(List<BollingerBandsCandleData> candles, BollingerBandsStrategyConfiguration config)
        {
            // Skip candles that don't have Bollinger Bands values
            int startIndex = config.Period;
            
            for (int i = startIndex; i < candles.Count; i++)
            {
                var candle = candles[i];
                var prevCandle = i > 0 ? candles[i - 1] : null;

                // Default: no signal
                candle.Signal = 0;

                if (candle.LowerBand == null || candle.UpperBand == null || candle.MiddleBand == null)
                    continue;

                // Mean reversion strategy: Buy when price touches lower band, sell when price touches upper band
                if (prevCandle != null && prevCandle.LowerBand != null && prevCandle.UpperBand != null)
                {
                    // Buy signal: Price crosses below or touches lower band
                    if (candle.Close <= candle.LowerBand.Value && prevCandle.Close > prevCandle.LowerBand.Value)
                    {
                        candle.Signal = 1; // Buy signal - price touched lower band
                    }
                    // Alternative: Buy when price is below lower band and starts rising
                    else if (candle.Close < candle.LowerBand.Value && 
                             candle.Close > prevCandle.Close)
                    {
                        candle.Signal = 1; // Buy signal - price recovering from lower band
                    }
                }
            }

            _logger.Info("Generated signals for {CandleCount} candles", candles.Count);
        }

        private BollingerBandsBacktestResult BacktestStrategy(List<BollingerBandsCandleData> candles, decimal initialCapital, BollingerBandsStrategyConfiguration config)
        {
            decimal capital = initialCapital;
            decimal assetHoldings = 0;
            int position = 0; // 0: no position, 1: long
            decimal entryPrice = 0;
            DateTime entryTime = DateTime.MinValue;
            decimal? lowerBandAtEntry = null;
            decimal? upperBandAtEntry = null;

            var trades = new List<BollingerBandsTrade>();
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

                // Exit existing position if stop loss, take profit, or upper band is hit
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
                        // Check upper band (mean reversion exit)
                        else if (candle.UpperBand != null && currentPrice >= candle.UpperBand.Value)
                        {
                            shouldExit = true;
                            exitReason = "UPPER_BAND";
                        }
                        
                        if (shouldExit)
                        {
                            // Exit long position (sell)
                            decimal exitValue = assetHoldings * currentPrice;
                            capital += exitValue;
                            
                            var trade = new BollingerBandsTrade
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
                                LowerBandAtEntry = lowerBandAtEntry,
                                UpperBandAtEntry = upperBandAtEntry,
                                LowerBandAtExit = candle.LowerBand,
                                UpperBandAtExit = candle.UpperBand
                            };
                            
                            trades.Add(trade);
                            assetHoldings = 0;
                            position = 0;
                            candle.Position = 0;
                            candle.ExitPrice = currentPrice;
                            candle.PnL = trade.PnL;
                            
                            _logger.Debug("Exited long position at {Price}, PnL: {PnL}%, Reason: {Reason}",
                                currentPrice, pnlPercent, exitReason);
                        }
                    }
                }

                // Enter new position based on buy signal (mean reversion: buy when price touches lower band)
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
                                lowerBandAtEntry = candle.LowerBand;
                                upperBandAtEntry = candle.UpperBand;
                                
                                candle.Position = 1;
                                candle.EntryPrice = currentPrice;
                                
                                _logger.Debug("Entered long position at {Price}, Size: {Size}, Cost: {Cost}",
                                    currentPrice, positionSize, cost);
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
                
                var finalTrade = new BollingerBandsTrade
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
                    LowerBandAtEntry = lowerBandAtEntry,
                    UpperBandAtEntry = upperBandAtEntry,
                    LowerBandAtExit = lastCandle.LowerBand,
                    UpperBandAtExit = lastCandle.UpperBand
                };
                
                trades.Add(finalTrade);
            }

            // Calculate final statistics
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
            
            // Additional validation: Check for negative equity
            if (finalEquity < 0)
            {
                _logger.Error("CRITICAL: Final equity is negative ({FinalEquity}). This indicates a calculation error.",
                    finalEquity);
            }
            
            // Log validation info
            _logger.Info("Backtest validation - Final equity: {FinalEquity}, Initial capital: {InitialCapital}, Total profit: {TotalProfit}, Sum of trade PnLs: {SumPnLs}, Difference: {Difference}",
                finalEquity, initialCapital, totalProfit, sumOfTradePnLs, difference);

            var winningTrades = trades.Where(t => t.Result == "WIN").ToList();
            var losingTrades = trades.Where(t => t.Result == "LOSS").ToList();

            decimal winRate = trades.Count > 0 ? (winningTrades.Count / (decimal)trades.Count) * 100 : 0;
            decimal avgWin = winningTrades.Count > 0 ? winningTrades.Average(t => t.PnL) : 0;
            decimal avgLoss = losingTrades.Count > 0 ? Math.Abs(losingTrades.Average(t => t.PnL)) : 0;
            decimal profitFactor = avgLoss > 0 ? avgWin / avgLoss : (avgWin > 0 ? 999 : 0);

            return new BollingerBandsBacktestResult
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

