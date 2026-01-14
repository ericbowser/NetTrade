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
    public interface IAlpacaMovingAverageCrossoverTradingService : IHostedService
    {
        Task<decimal> GetCurrentPriceAsync(string symbol);
        Task<(decimal? FastMA, decimal? SlowMA)> CalculateCurrentMAsAsync(string symbol);
    }

    public class AlpacaMovingAverageCrossoverTradingService : IAlpacaMovingAverageCrossoverTradingService
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly MovingAverageCrossoverStrategyConfiguration _maConfig;
        private readonly IAlpacaCryptoDataClient _alpacaCryptoDataClient;
        private readonly IMovingAverageCrossoverStrategyService _maStrategyService;
        private readonly IAlpacaTradingClient _tradingClient;
        private readonly TimeSpan _checkInterval;

        private decimal? _currentFastMA;
        private decimal? _currentSlowMA;
        private decimal? _previousFastMA;
        private decimal? _previousSlowMA;
        private decimal _currentPrice;
        private decimal _currentCapital;
        private decimal _assetHoldings;
        private bool _isRunning;
        private int _position = 0; // 0: no position, 1: long
        private decimal _entryPrice;
        private DateTime _entryTime;
        private Guid? _activeOrderId;

        public AlpacaMovingAverageCrossoverTradingService(
            IAlpacaCryptoDataClient alpacaCryptoDataClient,
            IMovingAverageCrossoverStrategyService maStrategyService,
            MovingAverageCrossoverStrategyConfiguration maConfig,
            IAlpacaTradingClient tradingClient,
            decimal initialCapital = 1000,
            int checkIntervalSeconds = 300)
        {
            _alpacaCryptoDataClient = alpacaCryptoDataClient;
            _maStrategyService = maStrategyService;
            _maConfig = maConfig;
            _tradingClient = tradingClient;
            _currentCapital = initialCapital;
            _checkInterval = TimeSpan.FromSeconds(checkIntervalSeconds);

            _logger.Info("Alpaca Moving Average Crossover Trading Service initialized");
        }

        public async Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.Info($"Starting Alpaca Moving Average Crossover Trading Bot for {_maConfig.Symbol}");
            _isRunning = true;

            // Format symbol for Alpaca
            string symbol = _maConfig.Symbol.FormatSymbolForAlpaca();

            try
            {
                // Convert timeframe if needed
                if (string.IsNullOrEmpty(_maConfig.Timeframe) == false)
                {
                    _maConfig.BarTimeframe = GetTimeFrameFromString(_maConfig.Timeframe);
                }

                // Main monitoring loop
                while (!stoppingToken.IsCancellationRequested && _isRunning)
                {
                    try
                    {
                        // Get current price
                        _currentPrice = await GetCurrentPriceAsync(symbol);
                        _logger.Debug($"Current price for {symbol}: {_currentPrice}");

                        // Calculate current MAs
                        var (fastMA, slowMA) = await CalculateCurrentMAsAsync(symbol);
                        _currentFastMA = fastMA;
                        _currentSlowMA = slowMA;

                        if (_currentFastMA.HasValue && _currentSlowMA.HasValue)
                        {
                            _logger.Debug($"Current Fast MA ({_maConfig.FastPeriod}): {_currentFastMA.Value:F2}, Slow MA ({_maConfig.SlowPeriod}): {_currentSlowMA.Value:F2}");

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

                            _previousFastMA = _currentFastMA;
                            _previousSlowMA = _currentSlowMA;
                        }
                        else
                        {
                            _logger.Warn($"Could not calculate Moving Averages for {symbol}");
                        }

                        // Calculate and log portfolio value
                        decimal portfolioValue = _currentCapital + (_assetHoldings * _currentPrice);
                        decimal profitLoss = portfolioValue - _currentCapital;
                        decimal profitLossPercent = _currentCapital > 0 ? (profitLoss / _currentCapital) * 100 : 0;

                        _logger.Info(
                            $"Portfolio value: ${portfolioValue:N2}, P&L: ${profitLoss:N2} ({profitLossPercent:N2}%), Position: {(_position == 1 ? "LONG" : "NONE")}, Fast MA: {_currentFastMA?.ToString("F2") ?? "N/A"}, Slow MA: {_currentSlowMA?.ToString("F2") ?? "N/A"}");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Error in Moving Average Crossover trading monitoring cycle");
                    }

                    await Task.Delay(_checkInterval, stoppingToken);
                }
            }
            catch (TaskCanceledException)
            {
                _logger.Info("Moving Average Crossover trading bot gracefully stopped");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Fatal error in Moving Average Crossover trading bot");
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
            _logger.Info("Stopping Moving Average Crossover trading bot");
            _isRunning = false;
            
            string symbol = _maConfig.Symbol.FormatSymbolForAlpaca();
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

        public async Task<(decimal? FastMA, decimal? SlowMA)> CalculateCurrentMAsAsync(string symbol)
        {
            try
            {
                // Get recent historical data to calculate MAs
                // We need at least max(FastPeriod, SlowPeriod) candles
                DateTime endDate = DateTime.UtcNow;
                DateTime startDate = endDate.AddDays(-7); // Get last 7 days to ensure we have enough data

                var config = new MovingAverageCrossoverStrategyConfiguration
                {
                    Symbol = symbol,
                    StartDate = startDate,
                    EndDate = endDate,
                    BarTimeframe = _maConfig.BarTimeframe,
                    FastPeriod = _maConfig.FastPeriod,
                    SlowPeriod = _maConfig.SlowPeriod
                };

                var candles = await _maStrategyService.GetHistoricalData(config);

                int maxPeriod = Math.Max(_maConfig.FastPeriod, _maConfig.SlowPeriod);
                if (candles.Count < maxPeriod)
                {
                    _logger.Warn($"Not enough candles to calculate MAs. Need {maxPeriod}, got {candles.Count}");
                    return (null, null);
                }

                // Calculate MAs using the same method as backtest
                CalculateMovingAverages(candles, _maConfig.FastPeriod, _maConfig.SlowPeriod);

                // Return the MAs of the most recent candle
                var lastCandle = candles.LastOrDefault(c => c.FastMA.HasValue && c.SlowMA.HasValue);
                return (lastCandle?.FastMA, lastCandle?.SlowMA);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error calculating Moving Averages for {symbol}");
                return (null, null);
            }
        }

        private void CalculateMovingAverages(List<MovingAverageCrossoverCandleData> candles, int fastPeriod, int slowPeriod)
        {
            int maxPeriod = Math.Max(fastPeriod, slowPeriod);
            
            if (candles.Count < maxPeriod)
                return;

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
        }

        private async Task CheckEntryConditionsAsync(string symbol)
        {
            if (!_currentFastMA.HasValue || !_currentSlowMA.HasValue ||
                !_previousFastMA.HasValue || !_previousSlowMA.HasValue)
                return;

            // Golden Cross: Fast MA crosses above Slow MA (buy signal)
            bool buySignal = false;
            
            if (_previousFastMA.Value <= _previousSlowMA.Value && 
                _currentFastMA.Value > _currentSlowMA.Value)
            {
                buySignal = true;
                _logger.Info($"Golden Cross detected! Fast MA ({_currentFastMA.Value:F2}) crossed above Slow MA ({_currentSlowMA.Value:F2})");
            }

            if (buySignal)
            {
                await EnterLongPositionAsync(symbol);
            }
        }

        private async Task CheckExitConditionsAsync(string symbol)
        {
            if (!_currentFastMA.HasValue || !_currentSlowMA.HasValue)
                return;

            decimal pnlPercent = ((_currentPrice - _entryPrice) / _entryPrice) * 100;
            bool shouldExit = false;
            string exitReason = "";

            // Check stop loss
            if (pnlPercent <= -_maConfig.StopLossPercent)
            {
                shouldExit = true;
                exitReason = "STOP_LOSS";
            }
            // Check take profit
            else if (pnlPercent >= _maConfig.TakeProfitPercent)
            {
                shouldExit = true;
                exitReason = "TAKE_PROFIT";
            }
            // Check death cross (Fast MA crosses below Slow MA)
            else if (_previousFastMA.HasValue && _previousSlowMA.HasValue &&
                     _previousFastMA.Value >= _previousSlowMA.Value &&
                     _currentFastMA.Value < _currentSlowMA.Value)
            {
                shouldExit = true;
                exitReason = "DEATH_CROSS";
            }

            if (shouldExit)
            {
                _logger.Info($"Exit condition met: {exitReason}, PnL: {pnlPercent:F2}%, Fast MA: {_currentFastMA.Value:F2}, Slow MA: {_currentSlowMA.Value:F2}");
                await ExitLongPositionAsync(symbol, exitReason);
            }
        }

        private async Task EnterLongPositionAsync(string symbol)
        {
            try
            {
                // Calculate position size based on risk
                decimal stopLossPrice = _currentPrice * (1 - _maConfig.StopLossPercent / 100);
                decimal riskPerUnit = _currentPrice - stopLossPrice;

                if (riskPerUnit <= 0 || _currentCapital <= 0)
                {
                    _logger.Warn("Cannot calculate position size: invalid risk per unit or no capital");
                    return;
                }

                // Calculate maximum position size based on risk
                decimal riskAmount = _currentCapital * _maConfig.RiskPerTrade;
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

                    _logger.Info($"Entered long position at {_entryPrice}, Size: {positionSize:F6}, Fast MA: {_currentFastMA?.ToString("F2") ?? "N/A"}, Slow MA: {_currentSlowMA?.ToString("F2") ?? "N/A"}");
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
                _ => BarTimeFrame.Hour // Default to 1Hour for trend following
            };
        }
    }
}

