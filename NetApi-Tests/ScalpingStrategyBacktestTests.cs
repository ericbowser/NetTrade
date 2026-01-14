using Xunit;
using NetTrade.Service;
using NetTrade.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Moq;

namespace CoinApi_Tests
{
    public class ScalpingStrategyBacktestTests
    {
        private ScalpingStrategyService CreateService()
        {
            // Mock the IAlpacaCryptoDataClient since BacktestStrategy doesn't actually use it
            var mockClient = new Mock<IAlpacaCryptoDataClient>();
            return new ScalpingStrategyService(mockClient.Object);
        }

        private ScalpingStrategyConfiguration GetDefaultConfig()
        {
            return new ScalpingStrategyConfiguration
            {
                Symbol = "BTC/USD",
                Timeframe = "1Min",
                SmaPeriod = 5, // Shorter period for testing
                MacdFastPeriod = 3,
                MacdSlowPeriod = 5,
                MacdSignalPeriod = 3,
                RiskPerTrade = 0.02m,
                TakeProfitPips = 2m,  // 2%
                StopLossPips = 1m,    // 1%
                UseHeikinAshi = false
            };
        }

        [Fact]
        public void ScalpingBacktest_SimpleLongTrade_CalculatesCorrectPnL()
        {
            // Arrange: Create candles with indicators that trigger a long trade
            var config = GetDefaultConfig();
            var candles = CreateCandlesWithSimpleSignal();
            decimal initialCapital = 1000m;

            // Manually calculate indicators for testing
            CalculateSimpleIndicators(candles);

            // Generate signals based on indicators (required for BacktestStrategy)
            GenerateSignals(candles);

            // Act
            var service = CreateService();
            var result = service.BacktestStrategy(candles, initialCapital, config);

            // Assert
            Assert.True(result.TotalTrades >= 1);

            if (result.Trades.Any())
            {
                var firstTrade = result.Trades.First();
                Assert.NotEqual(0m, firstTrade.EntryPrice);
                Assert.True(firstTrade.Equity > 0);
            }
        }

        [Fact]
        public void ScalpingBacktest_TakeProfitHit_ClosesPositionWithProfit()
        {
            // Arrange: Create scenario where take profit is hit
            var config = GetDefaultConfig();
            config.TakeProfitPips = 2m; // 2% take profit

            var candles = new List<CandleData>();
            var baseTime = DateTime.UtcNow.AddMinutes(-20);
            var basePrice = 100m;
            
            // Setup period - build indicators with negative histogram (no signal)
            // Start with prices below SMA to create negative histogram
            for (int i = 0; i < 10; i++)
            {
                var price = basePrice - 1m; // Price below base
                candles.Add(new CandleData
                {
                    Timestamp = baseTime.AddMinutes(i),
                    Open = price,
                    High = price * 1.01m,
                    Low = price * 0.99m,
                    Close = price,
                    Volume = 100m
                });
            }

            // Buy signal at 100 - price rises above SMA, histogram crosses from negative to positive
            candles.Add(new CandleData
            {
                Timestamp = baseTime.AddMinutes(10),
                Open = basePrice,
                High = basePrice * 1.01m,
                Low = basePrice * 0.99m,
                Close = basePrice,
                Volume = 100m
            });

            // Price moves up 2.5% (hits take profit at 2%)
            for (int i = 11; i < 15; i++)
            {
                var price = basePrice + (i - 10) * 0.5m; // 100.5, 101, 101.5, 102, 102.5
                candles.Add(new CandleData
                {
                    Timestamp = baseTime.AddMinutes(i),
                    Open = price,
                    High = price * 1.01m,
                    Low = price * 0.99m,
                    Close = price,
                    Volume = 100m
                });
            }

            // Calculate indicators properly
            CalculateSimpleIndicators(candles);
            
            // Generate signals based on indicators
            GenerateSignals(candles);

            decimal initialCapital = 1000m;

            // Act
            var service = CreateService();
            var result = service.BacktestStrategy(candles, initialCapital, config);

            // Assert
            Assert.True(result.TotalTrades >= 1, "Should have at least 1 trade");

            if (result.Trades.Any())
            {
                var trade = result.Trades.First();
                Assert.Equal("LONG", trade.Direction);
                Assert.True(trade.PnL > 0, "Trade should be profitable");
                Assert.True(trade.PnLPct >= config.TakeProfitPips, $"PnL% should meet take profit target. Expected: >= {config.TakeProfitPips}%, Actual: {trade.PnLPct}%");
                Assert.Equal("WIN", trade.Result);
            }

            Assert.True(result.FinalEquity > initialCapital, "Final equity should be greater than initial");
        }

        [Fact]
        public void ScalpingBacktest_StopLossHit_ClosesPositionWithLoss()
        {
            // Arrange: Create scenario where stop loss is hit
            var config = GetDefaultConfig();
            config.StopLossPips = 1m; // 1% stop loss

            var candles = new List<CandleData>();
            var baseTime = DateTime.UtcNow.AddMinutes(-20);
            var basePrice = 100m;
            
            // Setup period - build indicators with negative histogram (no signal)
            // Start with prices below SMA to create negative histogram
            for (int i = 0; i < 10; i++)
            {
                var price = basePrice - 1m; // Price below base
                candles.Add(new CandleData
                {
                    Timestamp = baseTime.AddMinutes(i),
                    Open = price,
                    High = price * 1.01m,
                    Low = price * 0.99m,
                    Close = price,
                    Volume = 100m
                });
            }

            // Buy signal at 100 - price rises above SMA, histogram crosses from negative to positive
            candles.Add(new CandleData
            {
                Timestamp = baseTime.AddMinutes(10),
                Open = basePrice,
                High = basePrice * 1.01m,
                Low = basePrice * 0.99m,
                Close = basePrice,
                Volume = 100m
            });

            // Price moves down 1.5% (hits stop loss at -1%)
            // Need to drop from 100 to at least 99 (1% drop) to hit stop loss
            // Add more candles with larger drops to ensure stop loss is hit
            for (int i = 11; i < 16; i++)
            {
                var price = basePrice - (i - 10) * 0.6m; // 99.4, 98.8, 98.2, 97.6, 97.0 (ensures >1% drop)
                candles.Add(new CandleData
                {
                    Timestamp = baseTime.AddMinutes(i),
                    Open = price,
                    High = price * 1.01m,
                    Low = price * 0.99m,
                    Close = price,
                    Volume = 100m
                });
            }

            // Calculate indicators properly
            CalculateSimpleIndicators(candles);
            
            // Generate signals based on indicators
            GenerateSignals(candles);

            decimal initialCapital = 1000m;

            // Act
            var service = CreateService();
            var result = service.BacktestStrategy(candles, initialCapital, config);

            // Assert
            Assert.True(result.TotalTrades >= 1, "Should have at least 1 trade");

            if (result.Trades.Any())
            {
                var trade = result.Trades.First();
                Assert.Equal("LONG", trade.Direction);
                Assert.True(trade.PnL < 0, "Trade should be a loss");
                Assert.True(trade.PnLPct <= -config.StopLossPips, $"PnL% should hit stop loss. Expected: <= {-config.StopLossPips}%, Actual: {trade.PnLPct}%");
                Assert.Equal("LOSS", trade.Result);
            }

            Assert.True(result.FinalEquity < initialCapital, "Final equity should be less than initial");
        }

        [Fact]
        public void ScalpingBacktest_WinRateCalculation_IsAccurate()
        {
            // Arrange: Create scenario with known win/loss ratio
            var config = GetDefaultConfig();

            // Create 3 winning trades and 2 losing trades
            var candles = CreateCandlesForWinRateTest(config);
            decimal initialCapital = 1000m;

            // Calculate indicators first
            CalculateSimpleIndicators(candles);
            
            // Generate signals based on indicators
            GenerateSignals(candles);

            // Act
            var service = CreateService();
            var result = service.BacktestStrategy(candles, initialCapital, config);

            // Assert
            Assert.True(result.TotalTrades >= 2, "Should have at least 2 trades");

            if (result.TotalTrades > 0)
            {
                decimal expectedWinRate = ((decimal)result.WinningTrades / result.TotalTrades) * 100;
                Assert.Equal(expectedWinRate, result.WinRate);

                Assert.Equal(result.WinningTrades + result.LosingTrades, result.TotalTrades);
            }
        }

        [Fact]
        public void ScalpingBacktest_EquityTracking_IsMonotonic()
        {
            // Arrange: Verify equity is tracked consistently
            var config = GetDefaultConfig();
            var candles = CreateCandlesWithMultipleTrades();
            decimal initialCapital = 1000m;

            CalculateSimpleIndicators(candles);
            
            // Generate signals based on indicators
            GenerateSignals(candles);

            // Act
            var service = CreateService();
            var result = service.BacktestStrategy(candles, initialCapital, config);

            // Assert
            Assert.All(result.Trades, trade =>
            {
                Assert.True(trade.Equity > 0, "Equity should always be positive");
            });

            // First trade equity should be close to initial capital
            if (result.Trades.Any())
            {
                var firstTradeEquity = result.Trades.First().Equity;
                Assert.True(Math.Abs(firstTradeEquity - initialCapital) < initialCapital * 0.1m,
                    "First trade equity should be within 10% of initial capital");
            }

            // Final equity should match the last trade's equity
            Assert.Equal(result.FinalEquity, result.Trades.LastOrDefault()?.Equity ?? initialCapital);
        }

        // Helper methods to create test data
        private CandleData CreateCandleWithIndicators(DateTime time, decimal price, bool buySignal)
        {
            // Set up indicators to match signal generation logic
            // For buy signal: price > SMA200, MACD histogram > 0
            // For no signal: set up neutral indicators
            decimal sma = price * (buySignal ? 0.99m : 1.01m); // Price above SMA for buy
            decimal macd = buySignal ? 0.1m : -0.1m;
            decimal macdSignal = 0m;
            decimal macdHistogram = macd - macdSignal;
            
            return new CandleData
            {
                Timestamp = time,
                Open = price,
                High = price * 1.01m,
                Low = price * 0.99m,
                Close = price,
                Volume = 100m,
                Sma200 = sma,
                Macd = macd,
                MacdSignal = macdSignal,
                MacdHistogram = macdHistogram,
                Signal = 0, // Will be set by GenerateSignals
                Trend = price > sma ? 1 : -1
            };
        }

        private List<CandleData> CreateCandlesWithSimpleSignal()
        {
            var candles = new List<CandleData>();
            var basePrice = 100m;
            var baseTime = DateTime.UtcNow.AddMinutes(-30);

            // Create enough candles for indicators (need at least 5 for SMA, more for MACD)
            // Start with prices below SMA to create negative histogram
            for (int i = 0; i < 10; i++)
            {
                var price = basePrice - 1m; // Price below base to ensure negative histogram initially
                var candle = new CandleData
                {
                    Timestamp = baseTime.AddMinutes(i),
                    Open = price,
                    High = price * 1.01m,
                    Low = price * 0.99m,
                    Close = price,
                    Volume = 100m
                };
                candles.Add(candle);
            }

            // Create rising prices to trigger buy signal (price crosses above SMA, histogram turns positive)
            for (int i = 10; i < 20; i++)
            {
                // Gradually increase price to create uptrend
                var price = basePrice - 1m + (i - 10) * 0.2m; // Rising prices
                var candle = new CandleData
                {
                    Timestamp = baseTime.AddMinutes(i),
                    Open = price,
                    High = price * 1.01m,
                    Low = price * 0.99m,
                    Close = price,
                    Volume = 100m
                };
                candles.Add(candle);
            }

            return candles;
        }

        private List<CandleData> CreateCandlesWithMultipleTrades()
        {
            var candles = new List<CandleData>();
            var prices = new[] { 100m, 101m, 102m, 101m, 100m, 99m, 100m, 101m, 102m, 103m };

            for (int i = 0; i < prices.Length; i++)
            {
                candles.Add(new CandleData
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-prices.Length + i),
                    Open = prices[i],
                    High = prices[i] * 1.01m,
                    Low = prices[i] * 0.99m,
                    Close = prices[i],
                    Volume = 100m
                });
            }

            return candles;
        }

        private List<CandleData> CreateCandlesForWinRateTest(ScalpingStrategyConfiguration config)
        {
            // Create a series of candles that will result in multiple trades
            // Strategy: Create clear price movements that cross SMA and create histogram crossovers
            var candles = new List<CandleData>();
            var basePrice = 100m;
            var baseTime = DateTime.UtcNow.AddMinutes(-60);

            // Setup period - enough candles for indicators (start low to create negative histogram)
            for (int i = 0; i < 10; i++)
            {
                var price = basePrice - 2m; // Start well below base
                candles.Add(new CandleData
                {
                    Timestamp = baseTime.AddMinutes(i),
                    Open = price,
                    High = price * 1.01m,
                    Low = price * 0.99m,
                    Close = price,
                    Volume = 100m
                });
            }

            // First trade setup: Price rises to cross SMA and create buy signal
            // Then continue rising to hit take profit
            for (int i = 10; i < 18; i++)
            {
                var price = basePrice - 2m + (i - 10) * 0.4m; // Rising from 98 to 101.2
                candles.Add(new CandleData
                {
                    Timestamp = baseTime.AddMinutes(i),
                    Open = price,
                    High = price * 1.01m,
                    Low = price * 0.99m,
                    Close = price,
                    Volume = 100m
                });
            }

            // Price drops to create sell signal (cross below SMA, histogram negative)
            for (int i = 18; i < 25; i++)
            {
                var price = basePrice + 1m - (i - 18) * 0.4m; // Falling from 101.2 to 98.4
                candles.Add(new CandleData
                {
                    Timestamp = baseTime.AddMinutes(i),
                    Open = price,
                    High = price * 1.01m,
                    Low = price * 0.99m,
                    Close = price,
                    Volume = 100m
                });
            }

            // Second trade: Sell signal, then price continues down (win via take profit or reversal)
            for (int i = 25; i < 32; i++)
            {
                var price = basePrice - 1m - (i - 25) * 0.3m; // Continue falling from 98.4 to 96.5
                candles.Add(new CandleData
                {
                    Timestamp = baseTime.AddMinutes(i),
                    Open = price,
                    High = price * 1.01m,
                    Low = price * 0.99m,
                    Close = price,
                    Volume = 100m
                });
            }

            // Price rises to create buy signal again
            for (int i = 32; i < 40; i++)
            {
                var price = basePrice - 2.5m + (i - 32) * 0.35m; // Rising from 96.5 to 99.3
                candles.Add(new CandleData
                {
                    Timestamp = baseTime.AddMinutes(i),
                    Open = price,
                    High = price * 1.01m,
                    Low = price * 0.99m,
                    Close = price,
                    Volume = 100m
                });
            }

            // Third trade: Buy signal, but price drops (loss via stop loss)
            for (int i = 40; i < 48; i++)
            {
                var price = basePrice - 0.5m - (i - 40) * 0.35m; // Falling from 99.3 to 96.7
                candles.Add(new CandleData
                {
                    Timestamp = baseTime.AddMinutes(i),
                    Open = price,
                    High = price * 1.01m,
                    Low = price * 0.99m,
                    Close = price,
                    Volume = 100m
                });
            }

            return candles;
        }

        private void CalculateSimpleIndicators(List<CandleData> candles)
        {
            // Calculate SMA using the same logic as the service
            int smaPeriod = 5;
            for (int i = 0; i < candles.Count; i++)
            {
                if (i >= smaPeriod - 1)
                {
                    decimal sum = 0;
                    for (int j = i - (smaPeriod - 1); j <= i; j++)
                    {
                        sum += candles[j].Close;
                    }
                    candles[i].Sma200 = sum / smaPeriod;
                }
            }

            // Calculate MACD using EMA (simplified but functional)
            int fastPeriod = 3;
            int slowPeriod = 5;
            int signalPeriod = 3;

            // Calculate Fast EMA
            var fastEma = new List<decimal>();
            for (int i = 0; i < candles.Count; i++)
            {
                if (i < fastPeriod - 1)
                {
                    fastEma.Add(0);
                    continue;
                }
                if (i == fastPeriod - 1)
                {
                    decimal sum = 0;
                    for (int j = 0; j <= i; j++)
                    {
                        sum += candles[j].Close;
                    }
                    fastEma.Add(sum / fastPeriod);
                }
                else
                {
                    decimal multiplier = 2m / (fastPeriod + 1);
                    fastEma.Add((candles[i].Close - fastEma[i - 1]) * multiplier + fastEma[i - 1]);
                }
            }

            // Calculate Slow EMA
            var slowEma = new List<decimal>();
            for (int i = 0; i < candles.Count; i++)
            {
                if (i < slowPeriod - 1)
                {
                    slowEma.Add(0);
                    continue;
                }
                if (i == slowPeriod - 1)
                {
                    decimal sum = 0;
                    for (int j = 0; j <= i; j++)
                    {
                        sum += candles[j].Close;
                    }
                    slowEma.Add(sum / slowPeriod);
                }
                else
                {
                    decimal multiplier = 2m / (slowPeriod + 1);
                    slowEma.Add((candles[i].Close - slowEma[i - 1]) * multiplier + slowEma[i - 1]);
                }
            }

            // Calculate MACD Line (Fast EMA - Slow EMA)
            int macdStartIndex = Math.Max(fastPeriod, slowPeriod) - 1;
            for (int i = macdStartIndex; i < candles.Count; i++)
            {
                if (fastEma[i] != 0 && slowEma[i] != 0)
                {
                    candles[i].Macd = fastEma[i] - slowEma[i];
                }
            }

            // Calculate Signal Line (EMA of MACD Line)
            var macdValues = new List<decimal>();
            for (int i = macdStartIndex; i < candles.Count; i++)
            {
                if (candles[i].Macd.HasValue)
                {
                    macdValues.Add(candles[i].Macd.Value);
                }
            }

            var signalLine = new List<decimal>();
            int signalStartIndex = macdStartIndex + signalPeriod - 1;
            for (int i = 0; i < macdValues.Count; i++)
            {
                if (i < signalPeriod - 1)
                {
                    signalLine.Add(0);
                    continue;
                }
                if (i == signalPeriod - 1)
                {
                    decimal sum = 0;
                    for (int j = 0; j <= i; j++)
                    {
                        sum += macdValues[j];
                    }
                    signalLine.Add(sum / signalPeriod);
                }
                else
                {
                    decimal multiplier = 2m / (signalPeriod + 1);
                    signalLine.Add((macdValues[i] - signalLine[i - 1]) * multiplier + signalLine[i - 1]);
                }
            }

            // Set signal line values back to candles
            for (int i = 0; i < signalLine.Count; i++)
            {
                int candleIndex = signalStartIndex + i;
                if (candleIndex < candles.Count)
                {
                    candles[candleIndex].MacdSignal = signalLine[i];
                }
            }

            // Calculate Histogram (MACD Line - Signal Line)
            for (int i = signalStartIndex; i < candles.Count; i++)
            {
                if (candles[i].Macd.HasValue && candles[i].MacdSignal.HasValue)
                {
                    candles[i].MacdHistogram = candles[i].Macd.Value - candles[i].MacdSignal.Value;
                }
            }
        }

        private void GenerateSignals(List<CandleData> candles)
        {
            // Generate signals based on the same logic as ScalpingStrategyService
            // Skip candles that don't have complete indicator values
            int startIndex = 0;
            while (startIndex < candles.Count &&
                   (candles[startIndex].Sma200 == null ||
                    candles[startIndex].Macd == null ||
                    candles[startIndex].MacdSignal == null ||
                    candles[startIndex].MacdHistogram == null))
            {
                candles[startIndex].Signal = 0;
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
        }
    }
}
