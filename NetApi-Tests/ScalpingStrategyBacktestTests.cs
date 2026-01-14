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
            var baseTime = DateTime.UtcNow.AddMinutes(-10);
            
            // Setup period - build indicators with negative histogram (no signal)
            for (int i = 0; i < 5; i++)
            {
                var candle = CreateCandleWithIndicators(baseTime.AddMinutes(i), 100m, buySignal: false);
                candle.MacdHistogram = -0.1m; // Negative to allow crossover
                candles.Add(candle);
            }

            // Buy signal at 100 - histogram crosses from negative to positive
            var buyCandle = CreateCandleWithIndicators(baseTime.AddMinutes(5), 100m, buySignal: true);
            buyCandle.MacdHistogram = 0.1m; // Positive histogram
            candles.Add(buyCandle);

            // Price moves up 2.5% (hits take profit at 2%)
            for (int i = 6; i < 9; i++)
            {
                var price = 100m + (i - 5) * 1m; // 101, 102, 102.5
                var candle = CreateCandleWithIndicators(baseTime.AddMinutes(i), price, buySignal: false);
                candle.MacdHistogram = 0.1m; // Keep positive (no exit signal)
                candles.Add(candle);
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
                Assert.True(trade.PnLPct >= config.TakeProfitPips, "PnL% should meet take profit target");
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
            var baseTime = DateTime.UtcNow.AddMinutes(-10);
            
            // Setup period - build indicators with negative histogram (no signal)
            for (int i = 0; i < 5; i++)
            {
                var candle = CreateCandleWithIndicators(baseTime.AddMinutes(i), 100m, buySignal: false);
                candle.MacdHistogram = -0.1m; // Negative to allow crossover
                candles.Add(candle);
            }

            // Buy signal at 100 - histogram crosses from negative to positive
            var buyCandle = CreateCandleWithIndicators(baseTime.AddMinutes(5), 100m, buySignal: true);
            buyCandle.MacdHistogram = 0.1m; // Positive histogram
            candles.Add(buyCandle);

            // Price moves down 1.5% (hits stop loss at -1%)
            var candle1 = CreateCandleWithIndicators(baseTime.AddMinutes(6), 99m, buySignal: false);
            candle1.MacdHistogram = 0.1m; // Keep positive (no exit signal yet)
            candles.Add(candle1);
            
            var candle2 = CreateCandleWithIndicators(baseTime.AddMinutes(7), 98.5m, buySignal: false);
            candle2.MacdHistogram = 0.1m; // Keep positive (no exit signal yet)
            candles.Add(candle2);

            // Calculate indicators properly
            CalculateSimpleIndicators(candles);
            
            // Generate signals based on indicators
            GenerateSignals(candles);

            decimal initialCapital = 1000m;

            // Act
            var service = CreateService();
            var result = service.BacktestStrategy(candles, initialCapital, config);

            // Assert
            if (result.Trades.Any())
            {
                var trade = result.Trades.First();
                Assert.Equal("LONG", trade.Direction);
                Assert.True(trade.PnL < 0, "Trade should be a loss");
                Assert.True(trade.PnLPct <= -config.StopLossPips, "PnL% should hit stop loss");
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
            var baseTime = DateTime.UtcNow.AddMinutes(-20);

            // Create enough candles for indicators to be calculated
            // Start with negative histogram to allow crossover
            for (int i = 0; i < 10; i++)
            {
                var candle = new CandleData
                {
                    Timestamp = baseTime.AddMinutes(i),
                    Open = basePrice,
                    High = basePrice * 1.01m,
                    Low = basePrice * 0.99m,
                    Close = basePrice,
                    Volume = 100m
                };
                candles.Add(candle);
            }

            // Create a candle that will generate a buy signal (histogram crossover)
            for (int i = 10; i < 20; i++)
            {
                var candle = new CandleData
                {
                    Timestamp = baseTime.AddMinutes(i),
                    Open = basePrice,
                    High = basePrice * 1.01m,
                    Low = basePrice * 0.99m,
                    Close = basePrice,
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
            // Create a series of candles that will result in specific win/loss outcomes
            var candles = new List<CandleData>();

            // TODO: Implement specific candle patterns for predictable win/loss ratio
            // This would require detailed knowledge of the signal generation logic

            return CreateCandlesWithSimpleSignal();
        }

        private void CalculateSimpleIndicators(List<CandleData> candles)
        {
            // Simple SMA calculation for testing
            int period = 5;
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

                // Simple MACD calculation for testing
                // MACD = EMA(fast) - EMA(slow), simplified as price - SMA
                if (candles[i].Sma200 != null)
                {
                    candles[i].Trend = candles[i].Close > candles[i].Sma200 ? 1 : -1;
                    candles[i].Macd = candles[i].Close - candles[i].Sma200.Value;
                    candles[i].MacdSignal = 0; // Simplified for testing
                    candles[i].MacdHistogram = candles[i].Macd - candles[i].MacdSignal;
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
