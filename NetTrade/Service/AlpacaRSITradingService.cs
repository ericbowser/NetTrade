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
    public interface IAlpacaRSITradingService : IHostedService
    {
        Task<decimal> GetCurrentPriceAsync(string symbol);
        Task<decimal?> CalculateCurrentRSIAsync(string symbol);
    }

    public class AlpacaRSITradingService : IAlpacaRSITradingService
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly RSIStrategyConfiguration _rsiConfig;
        private readonly IAlpacaCryptoDataClient _alpacaCryptoDataClient;
        private readonly IRSIStrategyService _rsiStrategyService;
        private readonly IAlpacaTradingClient _tradingClient;
        private readonly TimeSpan _checkInterval;

        private decimal? _currentRSI;
        private decimal? _previousRSI;
        private decimal _currentPrice;
        private decimal _currentCapital;
        private decimal _assetHoldings;
        private bool _isRunning;
        private int _position = 0; // 0: no position, 1: long
        private decimal _entryPrice;
        private DateTime _entryTime;
        private Guid? _activeOrderId;

        public AlpacaRSITradingService(
            IAlpacaCryptoDataClient alpacaCryptoDataClient,
            IRSIStrategyService rsiStrategyService,
            RSIStrategyConfiguration rsiConfig,
            IAlpacaTradingClient tradingClient,
            decimal initialCapital = 1000,
            int checkIntervalSeconds = 60)
        {
            _alpacaCryptoDataClient = alpacaCryptoDataClient;
            _rsiStrategyService = rsiStrategyService;
            _rsiConfig = rsiConfig;
            _tradingClient = tradingClient;
            _currentCapital = initialCapital;
            _checkInterval = TimeSpan.FromSeconds(checkIntervalSeconds);

            _logger.Info("Alpaca RSI Trading Service initialized");
        }

        public async Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.Info($"Starting Alpaca RSI Trading Bot for {_rsiConfig.Symbol}");
            _isRunning = true;

            // Format symbol for Alpaca
            string symbol = _rsiConfig.Symbol.FormatSymbolForAlpaca();

            try
            {

                // Convert timeframe if needed
                if (string.IsNullOrEmpty(_rsiConfig.Timeframe) == false)
                {
                    _rsiConfig.BarTimeframe = GetTimeFrameFromString(_rsiConfig.Timeframe);
                }

                // Main monitoring loop
                while (!stoppingToken.IsCancellationRequested && _isRunning)
                {
                    try
                    {
                        // Get current price
                        _currentPrice = await GetCurrentPriceAsync(symbol);
                        _logger.Debug($"Current price for {symbol}: {_currentPrice}");

                        // Calculate current RSI
                        _currentRSI = await CalculateCurrentRSIAsync(symbol);

                        if (_currentRSI.HasValue)
                        {
                            _logger.Debug($"Current RSI for {symbol}: {_currentRSI.Value:F2}");

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

                            _previousRSI = _currentRSI;
                        }
                        else
                        {
                            _logger.Warn($"Could not calculate RSI for {symbol}");
                        }

                        // Calculate and log portfolio value
                        decimal portfolioValue = _currentCapital + (_assetHoldings * _currentPrice);
                        decimal profitLoss = portfolioValue - _currentCapital;
                        decimal profitLossPercent = _currentCapital > 0 ? (profitLoss / _currentCapital) * 100 : 0;

                        _logger.Info(
                            $"Portfolio value: ${portfolioValue:N2}, P&L: ${profitLoss:N2} ({profitLossPercent:N2}%), Position: {(_position == 1 ? "LONG" : "NONE")}, RSI: {_currentRSI?.ToString("F2") ?? "N/A"}");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Error in RSI trading monitoring cycle");
                    }

                    await Task.Delay(_checkInterval, stoppingToken);
                }
            }
            catch (TaskCanceledException)
            {
                _logger.Info("RSI trading bot gracefully stopped");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Fatal error in RSI trading bot");
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
            _logger.Info("Stopping RSI trading bot");
            _isRunning = false;
            
            string symbol = _rsiConfig.Symbol.FormatSymbolForAlpaca();
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

        public async Task<decimal?> CalculateCurrentRSIAsync(string symbol)
        {
            try
            {
                // Get recent historical data to calculate RSI
                // We need at least RSI period + 1 candles
                DateTime endDate = DateTime.UtcNow;
                DateTime startDate = endDate.AddHours(-24); // Get last 24 hours to ensure we have enough data

                var config = new RSIStrategyConfiguration
                {
                    Symbol = symbol,
                    StartDate = startDate,
                    EndDate = endDate,
                    BarTimeframe = _rsiConfig.BarTimeframe,
                    RSIPeriod = _rsiConfig.RSIPeriod
                };

                var candles = await _rsiStrategyService.GetHistoricalData(config);

                if (candles.Count < _rsiConfig.RSIPeriod + 1)
                {
                    _logger.Warn($"Not enough candles to calculate RSI. Need {_rsiConfig.RSIPeriod + 1}, got {candles.Count}");
                    return null;
                }

                // Calculate RSI using the same method as backtest
                CalculateRSI(candles, _rsiConfig.RSIPeriod);

                // Return the RSI of the most recent candle
                var lastCandle = candles.LastOrDefault(c => c.RSI.HasValue);
                return lastCandle?.RSI;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error calculating RSI for {symbol}");
                return null;
            }
        }

        private void CalculateRSI(List<RSICandleData> candles, int period)
        {
            if (candles.Count < period + 1)
                return;

            // Calculate initial average gain and loss
            decimal avgGain = 0;
            decimal avgLoss = 0;

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
                candles[period].RSI = 100;
            }

            // Calculate RSI for remaining candles using Wilder's smoothing method
            for (int i = period + 1; i < candles.Count; i++)
            {
                decimal change = candles[i].Close - candles[i - 1].Close;
                decimal gain = change > 0 ? change : 0;
                decimal loss = change < 0 ? Math.Abs(change) : 0;

                avgGain = (avgGain * (period - 1) + gain) / period;
                avgLoss = (avgLoss * (period - 1) + loss) / period;

                if (avgLoss != 0)
                {
                    decimal rs = avgGain / avgLoss;
                    candles[i].RSI = 100 - (100 / (1 + rs));
                }
                else
                {
                    candles[i].RSI = 100;
                }
            }
        }

        private async Task CheckEntryConditionsAsync(string symbol)
        {
            if (!_currentRSI.HasValue || !_previousRSI.HasValue)
                return;

            // Buy signal: RSI crosses below oversold level or is oversold and rising
            bool buySignal = false;
            
            if (_previousRSI.Value >= _rsiConfig.OversoldLevel && 
                _currentRSI.Value < _rsiConfig.OversoldLevel)
            {
                buySignal = true;
                _logger.Info($"RSI crossed below oversold level ({_rsiConfig.OversoldLevel}). RSI: {_currentRSI.Value:F2}");
            }
            else if (_currentRSI.Value < _rsiConfig.OversoldLevel && 
                     _currentRSI.Value > _previousRSI.Value)
            {
                buySignal = true;
                _logger.Info($"RSI is oversold and rising. RSI: {_currentRSI.Value:F2}");
            }

            if (buySignal)
            {
                await EnterLongPositionAsync(symbol);
            }
        }

        private async Task CheckExitConditionsAsync(string symbol)
        {
            if (!_currentRSI.HasValue)
                return;

            decimal pnlPercent = ((_currentPrice - _entryPrice) / _entryPrice) * 100;
            bool shouldExit = false;
            string exitReason = "";

            // Check stop loss
            if (pnlPercent <= -_rsiConfig.StopLossPercent)
            {
                shouldExit = true;
                exitReason = "STOP_LOSS";
            }
            // Check take profit
            else if (pnlPercent >= _rsiConfig.TakeProfitPercent)
            {
                shouldExit = true;
                exitReason = "TAKE_PROFIT";
            }
            // Check overbought RSI
            else if (_currentRSI.Value > _rsiConfig.OverboughtLevel)
            {
                shouldExit = true;
                exitReason = "OVERBOUGHT";
            }

            if (shouldExit)
            {
                _logger.Info($"Exit condition met: {exitReason}, PnL: {pnlPercent:F2}%, RSI: {_currentRSI.Value:F2}");
                await ExitLongPositionAsync(symbol, exitReason);
            }
        }

        private async Task EnterLongPositionAsync(string symbol)
        {
            try
            {
                // Calculate position size based on risk
                decimal stopLossPrice = _currentPrice * (1 - _rsiConfig.StopLossPercent / 100);
                decimal riskPerUnit = _currentPrice - stopLossPrice;

                if (riskPerUnit <= 0 || _currentCapital <= 0)
                {
                    _logger.Warn("Cannot calculate position size: invalid risk per unit or no capital");
                    return;
                }

                // Calculate maximum position size based on risk
                decimal riskAmount = _currentCapital * _rsiConfig.RiskPerTrade;
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

                    _logger.Info($"Entered long position at {_entryPrice}, Size: {positionSize:F6}, RSI: {_currentRSI?.ToString("F2") ?? "N/A"}");
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
                _ => new BarTimeFrame(15, BarTimeFrameUnit.Minute)
            };
        }
    }
}

