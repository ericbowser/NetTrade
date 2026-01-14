using Alpaca.Markets;
using Microsoft.Extensions.Hosting;
using NLog;
using NetTrade.Helpers;
using NetTrade.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ILogger = NLog.ILogger;

namespace NetTrade.Service
{
    public interface IAlpacaBollingerBandsTradingService : IHostedService
    {
        Task<decimal> GetCurrentPriceAsync(string symbol);
        Task<(decimal? UpperBand, decimal? MiddleBand, decimal? LowerBand)> CalculateCurrentBollingerBandsAsync(string symbol);
    }

    public class AlpacaBollingerBandsTradingService : IAlpacaBollingerBandsTradingService
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly BollingerBandsStrategyConfiguration _bbConfig;
        private readonly IAlpacaCryptoDataClient _alpacaCryptoDataClient;
        private readonly IBollingerBandsStrategyService _bbStrategyService;
        private readonly IAlpacaTradingClient _tradingClient;
        private readonly TimeSpan _checkInterval;

        private decimal? _currentUpperBand;
        private decimal? _currentMiddleBand;
        private decimal? _currentLowerBand;
        private decimal? _previousLowerBand;
        private decimal _currentPrice;
        private decimal _previousPrice;
        private decimal _currentCapital;
        private decimal _assetHoldings;
        private bool _isRunning;
        private int _position = 0; // 0: no position, 1: long
        private decimal _entryPrice;
        private DateTime _entryTime;
        private Guid? _activeOrderId;

        public AlpacaBollingerBandsTradingService(
            IAlpacaCryptoDataClient alpacaCryptoDataClient,
            IBollingerBandsStrategyService bbStrategyService,
            BollingerBandsStrategyConfiguration bbConfig,
            IAlpacaTradingClient tradingClient,
            decimal initialCapital = 1000,
            int checkIntervalSeconds = 300)
        {
            _alpacaCryptoDataClient = alpacaCryptoDataClient;
            _bbStrategyService = bbStrategyService;
            _bbConfig = bbConfig;
            _tradingClient = tradingClient;
            _currentCapital = initialCapital;
            _checkInterval = TimeSpan.FromSeconds(checkIntervalSeconds);

            _logger.Info("Alpaca Bollinger Bands Trading Service initialized");
        }

        public async Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.Info($"Starting Alpaca Bollinger Bands Trading Bot for {_bbConfig.Symbol}");
            _isRunning = true;

            // Format symbol for Alpaca
            string symbol = _bbConfig.Symbol.FormatSymbolForAlpaca();

            try
            {
                // Convert timeframe if needed
                if (string.IsNullOrEmpty(_bbConfig.Timeframe) == false)
                {
                    _bbConfig.BarTimeframe = GetTimeFrameFromString(_bbConfig.Timeframe);
                }

                // Main monitoring loop
                while (!stoppingToken.IsCancellationRequested && _isRunning)
                {
                    try
                    {
                        // Get current price
                        _currentPrice = await GetCurrentPriceAsync(symbol);
                        _logger.Debug($"Current price for {symbol}: {_currentPrice}");

                        // Calculate current Bollinger Bands
                        var (upperBand, middleBand, lowerBand) = await CalculateCurrentBollingerBandsAsync(symbol);
                        _currentUpperBand = upperBand;
                        _currentMiddleBand = middleBand;
                        _currentLowerBand = lowerBand;

                        if (_currentUpperBand.HasValue && _currentMiddleBand.HasValue && _currentLowerBand.HasValue)
                        {
                            _logger.Debug($"Current Bollinger Bands for {symbol}: Upper: {_currentUpperBand.Value:F2}, Middle: {_currentMiddleBand.Value:F2}, Lower: {_currentLowerBand.Value:F2}");

                            // Check exit conditions first
                            if (_position == 1) // Long position
                            {
                                await CheckExitConditionsAsync(symbol);
                            }

                            // Check entry conditions
                            if (_position == 0)
                            {
                                await CheckEntryConditionsAsync(symbol);
                            }

                            _previousLowerBand = _currentLowerBand;
                            _previousPrice = _currentPrice;
                        }
                        else
                        {
                            _logger.Warn($"Could not calculate Bollinger Bands for {symbol}");
                        }

                        // Calculate and log portfolio value
                        decimal portfolioValue = _currentCapital + (_assetHoldings * _currentPrice);
                        decimal profitLoss = portfolioValue - _currentCapital;
                        decimal profitLossPercent = _currentCapital > 0 ? (profitLoss / _currentCapital) * 100 : 0;

                        _logger.Info(
                            $"Portfolio value: ${portfolioValue:N2}, P&L: ${profitLoss:N2} ({profitLossPercent:N2}%), Position: {(_position == 1 ? "LONG" : "NONE")}, Price: {_currentPrice:F2}, Lower Band: {_currentLowerBand?.ToString("F2") ?? "N/A"}, Upper Band: {_currentUpperBand?.ToString("F2") ?? "N/A"}");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Error in Bollinger Bands trading monitoring cycle");
                    }

                    await Task.Delay(_checkInterval, stoppingToken);
                }
            }
            catch (TaskCanceledException)
            {
                _logger.Info("Bollinger Bands trading bot gracefully stopped");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Fatal error in Bollinger Bands trading bot");
            }
            finally
            {
                // Close any open positions
                await ClosePositionAsync(symbol);
                _isRunning = false;
            }
        }

        public async Task StopAsync(CancellationToken stoppingToken = default)
        {
            _logger.Info("Stopping Bollinger Bands trading bot");
            _isRunning = false;
            
            string symbol = _bbConfig.Symbol.FormatSymbolForAlpaca();
            await ClosePositionAsync(symbol);
        }

        public async Task<decimal> GetCurrentPriceAsync(string symbol)
        {
            try
            {
                var quote = await _alpacaCryptoDataClient.ListLatestQuotesAsync(new List<string> { symbol });
                
                if (!quote.ContainsKey(symbol))
                {
                    _logger.Error($"Symbol {symbol} not found in quotes");
                    throw new KeyNotFoundException($"Symbol {symbol} not found in quotes");
                }
                
                var symbolQuote = quote[symbol];
                var asks = symbolQuote.AskPrice;
                var bids = symbolQuote.BidPrice;
                var currentPrice = (asks + bids) / 2;
                return currentPrice;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error getting current price for {symbol}");
                throw;
            }
        }

        public async Task<(decimal? UpperBand, decimal? MiddleBand, decimal? LowerBand)> CalculateCurrentBollingerBandsAsync(string symbol)
        {
            try
            {
                // Get recent historical data to calculate Bollinger Bands
                // We need at least Period candles
                DateTime endDate = DateTime.UtcNow;
                DateTime startDate = endDate.AddDays(-7); // Get last 7 days to ensure we have enough data

                var config = new BollingerBandsStrategyConfiguration
                {
                    Symbol = symbol,
                    StartDate = startDate,
                    EndDate = endDate,
                    BarTimeframe = _bbConfig.BarTimeframe,
                    Period = _bbConfig.Period,
                    StandardDeviations = _bbConfig.StandardDeviations
                };

                var candles = await _bbStrategyService.GetHistoricalData(config);

                if (candles.Count < _bbConfig.Period)
                {
                    _logger.Warn($"Not enough candles to calculate Bollinger Bands. Need {_bbConfig.Period}, got {candles.Count}");
                    return (null, null, null);
                }

                // Calculate Bollinger Bands using the same method as backtest
                CalculateBollingerBands(candles, _bbConfig.Period, _bbConfig.StandardDeviations);

                // Return the bands of the most recent candle
                var lastCandle = candles.LastOrDefault(c => c.UpperBand.HasValue && c.MiddleBand.HasValue && c.LowerBand.HasValue);
                return (lastCandle?.UpperBand, lastCandle?.MiddleBand, lastCandle?.LowerBand);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error calculating Bollinger Bands for {symbol}");
                return (null, null, null);
            }
        }

        private void CalculateBollingerBands(List<BollingerBandsCandleData> candles, int period, decimal standardDeviations)
        {
            if (candles.Count < period)
                return;

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
        }

        private async Task CheckEntryConditionsAsync(string symbol)
        {
            if (!_currentLowerBand.HasValue || !_previousLowerBand.HasValue)
                return;

            // Mean reversion strategy: Buy when price crosses below or touches lower band
            bool buySignal = false;
            
            // Buy signal: Price crosses below or touches lower band
            if (_currentPrice <= _currentLowerBand.Value && _previousPrice > _previousLowerBand.Value)
            {
                buySignal = true;
                _logger.Info($"Price crossed below lower band. Price: {_currentPrice:F2}, Lower Band: {_currentLowerBand.Value:F2}");
            }
            // Alternative: Buy when price is below lower band and starts rising
            else if (_currentPrice < _currentLowerBand.Value && _currentPrice > _previousPrice)
            {
                buySignal = true;
                _logger.Info($"Price is below lower band and rising. Price: {_currentPrice:F2}, Lower Band: {_currentLowerBand.Value:F2}");
            }

            if (buySignal)
            {
                await EnterLongPositionAsync(symbol);
            }
        }

        private async Task CheckExitConditionsAsync(string symbol)
        {
            if (!_currentUpperBand.HasValue || !_currentLowerBand.HasValue)
                return;

            decimal pnlPercent = ((_currentPrice - _entryPrice) / _entryPrice) * 100;
            bool shouldExit = false;
            string exitReason = "";

            // Check stop loss
            if (pnlPercent <= -_bbConfig.StopLossPercent)
            {
                shouldExit = true;
                exitReason = "STOP_LOSS";
            }
            // Check take profit
            else if (pnlPercent >= _bbConfig.TakeProfitPercent)
            {
                shouldExit = true;
                exitReason = "TAKE_PROFIT";
            }
            // Check upper band (mean reversion exit)
            else if (_currentPrice >= _currentUpperBand.Value)
            {
                shouldExit = true;
                exitReason = "UPPER_BAND";
            }

            if (shouldExit)
            {
                _logger.Info($"Exit condition met: {exitReason}, PnL: {pnlPercent:F2}%, Price: {_currentPrice:F2}, Upper Band: {_currentUpperBand.Value:F2}");
                await ExitLongPositionAsync(symbol, exitReason);
            }
        }

        private async Task EnterLongPositionAsync(string symbol)
        {
            try
            {
                // Calculate position size based on risk
                decimal stopLossPrice = _currentPrice * (1 - _bbConfig.StopLossPercent / 100);
                decimal riskPerUnit = _currentPrice - stopLossPrice;

                if (riskPerUnit <= 0 || _currentCapital <= 0)
                {
                    _logger.Warn("Cannot calculate position size: invalid risk per unit or no capital");
                    return;
                }

                // Calculate maximum position size based on risk
                decimal riskAmount = _currentCapital * _bbConfig.RiskPerTrade;
                decimal maxPositionSizeByRisk = riskAmount / riskPerUnit;

                // Calculate maximum position size based on available capital
                decimal maxPositionSizeByCapital = _currentCapital / _currentPrice;

                // Use the smaller of the two
                decimal positionSize = Math.Min(maxPositionSizeByRisk, maxPositionSizeByCapital);

                if (positionSize <= 0)
                {
                    _logger.Warn("Calculated position size is zero or negative");
                    return;
                }

                decimal cost = positionSize * _currentPrice;

                // Ensure we don't exceed capital
                if (cost > _currentCapital)
                {
                    positionSize = maxPositionSizeByCapital;
                    cost = positionSize * _currentPrice;
                }

                if (cost <= _currentCapital && positionSize > 0)
                {
                    // Place buy order
                    var order = await _tradingClient.PostOrderAsync(
                        new NewOrderRequest(
                            symbol,
                            OrderQuantity.Fractional(positionSize),
                            OrderSide.Buy,
                            OrderType.Market,
                            TimeInForce.Day)
                    );

                    _activeOrderId = order.OrderId;
                    _logger.Info($"Placed buy order: {order.OrderId}, Size: {positionSize:F6}, Price: {_currentPrice}, Cost: ${cost:N2}");

                    // Update position tracking (assuming order fills immediately for market orders)
                    _currentCapital -= cost;
                    _assetHoldings = positionSize;
                    _position = 1;
                    _entryPrice = _currentPrice;
                    _entryTime = DateTime.UtcNow;

                    _logger.Info($"Entered long position at {_entryPrice}, Size: {positionSize:F6}, Lower Band: {_currentLowerBand?.ToString("F2") ?? "N/A"}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error entering long position");
            }
        }

        private async Task ExitLongPositionAsync(string symbol, string reason)
        {
            try
            {
                if (_assetHoldings <= 0)
                {
                    _logger.Warn("No position to exit");
                    return;
                }

                // Place sell order
                var order = await _tradingClient.PostOrderAsync(
                    new NewOrderRequest(
                        symbol,
                        OrderQuantity.Fractional(_assetHoldings),
                        OrderSide.Sell,
                        OrderType.Market,
                        TimeInForce.Day)
                );

                _logger.Info($"Placed sell order: {order.OrderId}, Size: {_assetHoldings:F6}, Price: {_currentPrice}, Reason: {reason}");

                // Calculate PnL
                decimal exitValue = _assetHoldings * _currentPrice;
                decimal pnl = exitValue - (_assetHoldings * _entryPrice);
                decimal pnlPercent = ((_currentPrice - _entryPrice) / _entryPrice) * 100;

                // Update position tracking (assuming order fills immediately for market orders)
                _currentCapital += exitValue;
                _assetHoldings = 0;
                _position = 0;
                _activeOrderId = null;

                _logger.Info($"Exited long position. Entry: {_entryPrice}, Exit: {_currentPrice}, PnL: ${pnl:N2} ({pnlPercent:F2}%), Reason: {reason}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error exiting long position");
            }
        }

        private async Task ClosePositionAsync(string symbol)
        {
            if (_position == 1 && _assetHoldings > 0)
            {
                _logger.Info("Closing position on bot stop");
                await ExitLongPositionAsync(symbol, "BOT_STOPPED");
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
                _ => new BarTimeFrame(15, BarTimeFrameUnit.Minute) // Default to 15Min
            };
        }
    }
}

