// CoinAPI/Service/AlpacaGridTradingService.cs

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

namespace NetTrade.Service
{
    public interface IAlpacaGridTradingService : IHostedService
    {
        Task<decimal> GetCurrentPriceAsync(LatestOrderBooksRequest request);
        Task PlaceInitialGridOrdersAsync(string symbol);
        Task HandleFilledOrderAsync(int levelIndex, string symbol);
    }

    public class AlpacaGridTradingService : IAlpacaGridTradingService, IServiceProvider
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly GridTradingConfiguration _gridTradingConfig;
        private readonly IAlpacaCryptoDataClient _alpacaCryptoDataClient;
        private readonly IGridBacktestService _gridBacktestService;
        private readonly decimal _initialCapital;
        private readonly IAlpacaTradingClient _tradingClient;
        private readonly TimeSpan _checkInterval;

        private List<GridLevel> _gridLevels = new();
        private decimal _currentPrice;
        private decimal _currentCapital;
        private decimal _assetHoldings;
        private bool _isRunning;
        private Dictionary<int, Guid> _activeOrders;

        // Fix for CS8618: Initialize '_gridTradingConfig', '_tradingClient', and '_activeOrders' in the constructor.

        public AlpacaGridTradingService(
            IAlpacaCryptoDataClient alpacaCryptoDataClient,
            IGridBacktestService gridBacktestService,
            GridTradingConfiguration gridTradingConfig, // Added parameter for '_gridTradingConfig'
            IAlpacaTradingClient tradingClient,         // Added parameter for '_tradingClient'
            decimal initialCapital = 1000,
            int checkIntervalSeconds = 30)
        {
            _alpacaCryptoDataClient = alpacaCryptoDataClient;
            _gridBacktestService = gridBacktestService;
            _gridTradingConfig = gridTradingConfig;     // Initialize '_gridTradingConfig'
            _tradingClient = tradingClient;             // Initialize '_tradingClient'
            _initialCapital = initialCapital;
            _currentCapital = initialCapital;
            _checkInterval = TimeSpan.FromSeconds(checkIntervalSeconds);
            _activeOrders = new Dictionary<int, Guid>(); // Initialize '_activeOrders'

            _logger.Info("Alpaca Grid Trading Service initialized");
        }

        public async Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.Info($"Starting Alpaca Grid Trading Bot for {_gridTradingConfig.Symbol}");
            _isRunning = true;

            try
            {
                // Format symbol for Alpaca
                string symbol = _gridTradingConfig.Symbol.FormatSymbolForAlpaca();

                // Calculate grid levels
                _gridLevels = await _gridBacktestService.CalculateGridLevels(_gridTradingConfig);
                _logger.Info($"Calculated {_gridLevels.Count} grid levels");

                // Get current price
                _currentPrice = await GetCurrentPriceAsync(new LatestOrderBooksRequest(new[] { symbol }));
                _logger.Info($"Current price for {symbol}: {_currentPrice}");

                // Sync with existing orders and positions before placing new orders
                await SyncWithExistingStateAsync(symbol);

                // Place initial grid orders (will skip levels that already have orders)
                await PlaceInitialGridOrdersAsync(symbol);

                // Main monitoring loop
                while (!stoppingToken.IsCancellationRequested && _isRunning)
                {
                    try
                    {
                        // Update current price
                        decimal newPrice = await GetCurrentPriceAsync(new LatestOrderBooksRequest(new[] { symbol }));

                        if (newPrice != _currentPrice)
                        {
                            _logger.Info($"Price changed from {_currentPrice} to {newPrice}");
                            _currentPrice = newPrice;

                            // Check if orders have been filled and place new orders
                            await CheckAndUpdateOrdersAsync(symbol);
                        }

                        // Calculate and log portfolio value periodically
                        decimal portfolioValue = _currentCapital + (_assetHoldings * _currentPrice);
                        decimal profitLoss = portfolioValue - _initialCapital;
                        decimal profitLossPercent = (profitLoss / _initialCapital) * 100;

                        _logger.Info(
                            $"Portfolio value: ${portfolioValue:N2}, P&L: ${profitLoss:N2} ({profitLossPercent:N2}%)");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Error in grid trading monitoring cycle");
                    }

                    await Task.Delay(_checkInterval, stoppingToken);
                }
            }
            catch (TaskCanceledException)
            {
                _logger.Info("Grid trading bot gracefully stopped");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Fatal error in grid trading bot");
            }
            finally
            {
                // Cancel any remaining orders
                await CancelAllActiveOrdersAsync();
                _isRunning = false;
            }
        }

        public async Task StopAsync(CancellationToken stoppingToken = default)
        {
            _logger.Info("Stopping grid trading bot");
            _isRunning = false;
            await CancelAllActiveOrdersAsync();
        }

        public async Task<decimal> GetCurrentPriceAsync(LatestOrderBooksRequest request)
        {
            try
            {
                var symbol = request.Symbols.Single();
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
                _logger.Info($"Current price for {symbol}: {currentPrice} (bid: {bids}, ask: {asks})");
                return currentPrice;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error getting current price for {request.Symbols.Single()}");
                throw;
            }
        }

        private async Task SyncWithExistingStateAsync(string symbol)
        {
            try
            {
                _logger.Info("Syncing with existing orders and positions for {Symbol}", symbol);

                // Get all open orders for this symbol
                var allOrders = await _tradingClient.ListOrdersAsync(new ListOrdersRequest());
                var openOrders = allOrders
                    .Where(o => o.Symbol == symbol && 
                                (o.OrderStatus == OrderStatus.New || 
                                 o.OrderStatus == OrderStatus.PartiallyFilled ||
                                 o.OrderStatus == OrderStatus.Accepted))
                    .ToList();

                _logger.Info("Found {Count} existing open orders for {Symbol}", openOrders.Count, symbol);

                // Match existing orders to grid levels
                // Use a tolerance of 0.5% or $0.01, whichever is larger (for very low-priced assets)
                decimal priceTolerance = Math.Max(_currentPrice * 0.005m, 0.01m);
                int matchedOrders = 0;
                var matchedLevels = new HashSet<int>(); // Track which levels already have orders

                foreach (var order in openOrders)
                {
                    if (order.LimitPrice == null)
                    {
                        _logger.Warn("Found open order without limit price: {OrderId}, {Side}", order.OrderId, order.OrderSide);
                        continue;
                    }

                    // Find the closest matching grid level for this order
                    var matchingLevel = _gridLevels
                        .Where(level => 
                            level.OrderSide == order.OrderSide &&
                            !matchedLevels.Contains(level.Level) &&
                            Math.Abs(level.Price - order.LimitPrice.Value) <= priceTolerance)
                        .OrderBy(level => Math.Abs(level.Price - order.LimitPrice.Value))
                        .FirstOrDefault();

                    if (matchingLevel != null)
                    {
                        _logger.Info("Matched existing {Side} order at price {Price} to grid level {Level} (grid price: {GridPrice})",
                            order.OrderSide, order.LimitPrice, matchingLevel.Level, matchingLevel.Price);
                        
                        _activeOrders[matchingLevel.Level] = order.OrderId;
                        matchedLevels.Add(matchingLevel.Level);
                        matchedOrders++;
                    }
                    else
                    {
                        _logger.Warn("Found open order that doesn't match any grid level: {Side} at {Price}. " +
                            "This order will remain but won't be managed by the grid bot.",
                            order.OrderSide, order.LimitPrice);
                    }
                }

                // Sync capital and asset holdings from positions
                var positions = await _tradingClient.ListPositionsAsync();
                var symbolPosition = positions.FirstOrDefault(p => p.Symbol == symbol);

                if (symbolPosition != null)
                {
                    _assetHoldings = (decimal)symbolPosition.Quantity;
                    _logger.Info("Found existing position: {Quantity} {Symbol}", _assetHoldings, symbol);
                }
                else
                {
                    _assetHoldings = 0;
                }

                // Get account balance to calculate current capital
                var account = await _tradingClient.GetAccountAsync();
                decimal totalEquity = account.Equity ?? 0;
                decimal positionValue = symbolPosition?.MarketValue ?? 0;
                _currentCapital = totalEquity - positionValue;

                _logger.Info("Synced state - Capital: {Capital}, Asset Holdings: {Holdings}, Matched Orders: {Matched}/{Total}",
                    _currentCapital, _assetHoldings, matchedOrders, openOrders.Count);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error syncing with existing state. Will proceed with fresh start.");
                // Continue with initial capital if sync fails
            }
        }

        public async Task PlaceInitialGridOrdersAsync(string symbol)
        {
            _logger.Info($"Placing initial grid orders for {symbol}. Current price: {_currentPrice}, Grid levels: {_gridLevels.Count}");
            
            int ordersPlaced = 0;
            int ordersSkipped = 0;
            int ordersAlreadyExist = 0;

            foreach (var level in _gridLevels)
            {
                try
                {
                    // Skip if this level already has an active order
                    if (_activeOrders.ContainsKey(level.Level))
                    {
                        _logger.Debug($"Grid level {level.Level} already has an active order. Skipping.");
                        ordersAlreadyExist++;
                        continue;
                    }

                    // Place buy orders below current price, sell orders above
                    bool shouldPlaceOrder = (level.OrderSide == OrderSide.Buy && level.Price < _currentPrice) ||
                                           (level.OrderSide == OrderSide.Sell && level.Price > _currentPrice);

                    if (!shouldPlaceOrder)
                    {
                        _logger.Debug($"Skipping {level.OrderSide} order at level {level.Level}, price: {level.Price} (current price: {_currentPrice})");
                        ordersSkipped++;
                        continue;
                    }

                    // Calculate quantity based on level's order size
                    decimal quantity = level.OrderSize / level.Price;
                    
                    // Check if we have enough capital for buy orders or assets for sell orders
                    if (level.OrderSide == OrderSide.Buy && _currentCapital < level.OrderSize)
                    {
                        _logger.Warn($"Insufficient capital to place buy order at level {level.Level}. Required: {level.OrderSize}, Available: {_currentCapital}");
                        ordersSkipped++;
                        continue;
                    }
                    
                    if (level.OrderSide == OrderSide.Sell && _assetHoldings < quantity)
                    {
                        _logger.Warn($"Insufficient assets to place sell order at level {level.Level}. Required: {quantity}, Available: {_assetHoldings}");
                        ordersSkipped++;
                        continue;
                    }

                    _logger.Info($"Placing {level.OrderSide} order at level {level.Level}, price: {level.Price}, quantity: {quantity}, order size: {level.OrderSize}");

                    // Place limit order
                    var order = await _tradingClient.PostOrderAsync(
                        new NewOrderRequest(
                            symbol,
                            OrderQuantity.Fractional(quantity),
                            level.OrderSide,
                            OrderType.Limit,
                            TimeInForce.Gtc)
                        {
                            LimitPrice = level.Price
                        }
                    );

                    _activeOrders[level.Level] = order.OrderId;
                    ordersPlaced++;
                    _logger.Info($"Successfully placed {level.OrderSide} order at level {level.Level}, price: {level.Price}, order ID: {order.OrderId}");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Error placing initial grid order at level {level.Level}, price: {level.Price}, side: {level.OrderSide}");
                }
            }
            
            _logger.Info($"Finished placing initial grid orders. Placed: {ordersPlaced}, Already Exist: {ordersAlreadyExist}, Skipped: {ordersSkipped}, Total levels: {_gridLevels.Count}");
        }

         public async Task CheckAndUpdateOrdersAsync(string symbol)
        {
            try
            {
                // Get all orders to check status
                var orders = await _tradingClient.ListOrdersAsync(new ListOrdersRequest());
                var openOrderIds = orders.Select(o => o.OrderId).ToHashSet();

                // Identify filled orders
                var filledLevels = new List<int>();
                foreach (var kvp in _activeOrders)
                {
                    // If our order ID is not in open orders, it was filled
                    if (!openOrderIds.Contains(kvp.Value))
                    {
                        filledLevels.Add(kvp.Key);
                    }
                }

                // Process filled orders
                foreach (var level in filledLevels)
                {
                    await HandleFilledOrderAsync(level, symbol);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error checking and updating orders");
            }
        }

        public async Task HandleFilledOrderAsync(int levelIndex, string symbol)
        {
            try
            {
                // Get the grid level information
                var filledLevel = _gridLevels.FirstOrDefault(l => l.Level == levelIndex);
                if (filledLevel == null) return;

                Guid orderId = _activeOrders[levelIndex];
                _activeOrders.Remove(levelIndex);

                _logger.Info(
                    $"Order filled at level {levelIndex}, price: {filledLevel.Price}, side: {filledLevel.OrderSide}");

                // Update portfolio
                if (filledLevel.OrderSide == OrderSide.Buy)
                {
                    // Buy order filled - spent capital, gained assets
                    _currentCapital -= filledLevel.OrderSize;
                    _assetHoldings += filledLevel.OrderSize / filledLevel.Price;

                    // Place a sell order at the next higher level
                    int nextLevelIndex = levelIndex + 1;

                    if (nextLevelIndex < _gridLevels.Count)
                    {
                        var nextLevel = _gridLevels[nextLevelIndex];
                        decimal quantity = filledLevel.OrderSize / filledLevel.Price;

                        _logger.Info(
                            "Posting order to Alpaca for {Symbol} with order side {OrderSide}, and the quantity {Quantity}",
                            symbol,
                            filledLevel.OrderSide,
                            quantity);

                        var order = await _tradingClient.PostOrderAsync(
                            new NewOrderRequest(
                                symbol,
                                OrderQuantity.Fractional(quantity),
                                OrderSide.Sell,
                                OrderType.Limit,
                                TimeInForce.Gtc)
                            {
                                LimitPrice = nextLevel.Price
                            }
                        );

                        _activeOrders[nextLevelIndex] = order.OrderId;
                        _logger.Info($"Placed SELL order at level {nextLevelIndex}, price: {nextLevel.Price}");
                    }
                }
                else
                {
                    // Sell order filled - gained capital, spent assets
                    _currentCapital += filledLevel.OrderSize;
                    _assetHoldings -= filledLevel.OrderSize / filledLevel.Price;

                    // Place a buy order at the next lower level
                    int nextLevelIndex = levelIndex - 1;

                    if (nextLevelIndex >= 0)
                    {
                        var nextLevel = _gridLevels[nextLevelIndex];
                        decimal quantity = filledLevel.OrderSize / nextLevel.Price;

                        var order = await _tradingClient.PostOrderAsync(
                            new NewOrderRequest(
                                symbol,
                                OrderQuantity.Fractional(quantity),
                                OrderSide.Buy,
                                OrderType.Limit,
                                TimeInForce.Gtc
                            )
                            {
                                LimitPrice = nextLevel.Price
                            }
                        );

                        _activeOrders[nextLevelIndex] = order.OrderId;
                        _logger.Info($"Placed BUY order at level {nextLevelIndex}, price: {nextLevel.Price}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error handling filled order at level {levelIndex}");
            }
        }

        private async Task CancelAllActiveOrdersAsync()
        {
            _logger.Info("Cancelling all active orders");

            foreach (var orderId in _activeOrders.Values)
            {
                try
                {
                    await _tradingClient.CancelOrderAsync(orderId);
                    _logger.Info($"Cancelled order {orderId}");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Error cancelling order {orderId}");
                }
            }

            _activeOrders.Clear();
        }


        public object? GetService(Type serviceType)
        {
            try
            {
                return serviceType.Assembly.CreateInstance(serviceType.FullName);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error getting service");
                throw;
            }
        }
    }
}