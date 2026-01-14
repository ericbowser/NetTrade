using Alpaca.Markets;
using Microsoft.AspNetCore.Mvc;
using NLog;
using NetTrade.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetTrade.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AlpacaPaperController : ControllerBase
    {
        private readonly IAlpacaPaperClient _alpacaPaperClient;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public AlpacaPaperController(IAlpacaPaperClient alpacaPaperClient)
        {
            _alpacaPaperClient = alpacaPaperClient;
        }

        [HttpGet("account")]
        public async Task<IActionResult> GetAccount()
        {
            try
            {
                var account = await _alpacaPaperClient.AlpacaTradingClient.GetAccountAsync();
                return Ok(new
                {
                    AccountNumber = account.AccountNumber,
                    Equity = account.Equity,
                    BuyingPower = account.BuyingPower,
                    TradingBlocked = account.IsTradingBlocked,
                    AccountBlocked = account.IsAccountBlocked
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error getting account");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("positions")]
        public async Task<IActionResult> GetPositions()
        {
            try
            {
                var positions = await _alpacaPaperClient.AlpacaTradingClient.ListPositionsAsync();
                return Ok(positions.Select(p => new
                {
                    Symbol = p.Symbol,
                    Quantity = p.Quantity,
                    AverageEntryPrice = p.AverageEntryPrice,
                    CurrentPrice = p.AssetCurrentPrice,
                    MarketValue = p.MarketValue,
                    UnrealizedProfitLoss = p.UnrealizedProfitLoss,
                    UnrealizedProfitLossPercent = p.UnrealizedProfitLossPercent
                }));
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error getting positions");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders([FromQuery] OrderStatus? status = null)
        {
            try
            {
                var request = new ListOrdersRequest();
                // Note: ListOrdersRequest doesn't support WithStatus, filtering by status would need to be done client-side
                
                var orders = await _alpacaPaperClient.AlpacaTradingClient.ListOrdersAsync(request);
                
                // Filter by status if provided
                var filteredOrders = status.HasValue 
                    ? orders.Where(o => o.OrderStatus == status.Value) 
                    : orders;
                    
                return Ok(filteredOrders.Select(o => new
                {
                    OrderId = o.OrderId,
                    Symbol = o.Symbol,
                    Side = o.OrderSide,
                    Type = o.OrderType,
                    Quantity = o.Quantity,
                    FilledQuantity = o.FilledQuantity,
                    LimitPrice = o.LimitPrice,
                    StopPrice = o.StopPrice,
                    Status = o.OrderStatus,
                    TimeInForce = o.TimeInForce,
                    SubmittedAt = o.SubmittedAtUtc,
                    FilledAt = o.FilledAtUtc
                }));
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error getting orders");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPost("buy")]
        public async Task<IActionResult> PlaceBuyOrder([FromBody] MarketOrderRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Symbol))
                {
                    return BadRequest(new { Error = "Symbol is required" });
                }

                if (request.Quantity <= 0 && request.Notional <= 0)
                {
                    return BadRequest(new { Error = "Either Quantity or Notional must be greater than 0" });
                }

                var orderRequest = new NewOrderRequest(
                    request.Symbol,
                    request.Quantity > 0 
                        ? OrderQuantity.Fractional(request.Quantity) 
                        : OrderQuantity.Notional(request.Notional),
                    OrderSide.Buy,
                    OrderType.Market,
                    TimeInForce.Day);

                var order = await _alpacaPaperClient.AlpacaTradingClient.PostOrderAsync(orderRequest);

                _logger.Info($"Buy order placed: {order.OrderId} for {request.Symbol}");

                return Ok(new
                {
                    OrderId = order.OrderId,
                    Symbol = order.Symbol,
                    Side = order.OrderSide,
                    Type = order.OrderType,
                    Quantity = order.Quantity,
                    Status = order.OrderStatus,
                    SubmittedAt = order.SubmittedAtUtc
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error placing buy order for {request.Symbol}");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPost("sell")]
        public async Task<IActionResult> PlaceSellOrder([FromBody] MarketOrderRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Symbol))
                {
                    return BadRequest(new { Error = "Symbol is required" });
                }

                if (request.Quantity <= 0 && request.Notional <= 0)
                {
                    return BadRequest(new { Error = "Either Quantity or Notional must be greater than 0" });
                }

                var orderRequest = new NewOrderRequest(
                    request.Symbol,
                    request.Quantity > 0 
                        ? OrderQuantity.Fractional(request.Quantity) 
                        : OrderQuantity.Notional(request.Notional),
                    OrderSide.Sell,
                    OrderType.Market,
                    TimeInForce.Day);

                var order = await _alpacaPaperClient.AlpacaTradingClient.PostOrderAsync(orderRequest);

                _logger.Info($"Sell order placed: {order.OrderId} for {request.Symbol}");

                return Ok(new
                {
                    OrderId = order.OrderId,
                    Symbol = order.Symbol,
                    Side = order.OrderSide,
                    Type = order.OrderType,
                    Quantity = order.Quantity,
                    Status = order.OrderStatus,
                    SubmittedAt = order.SubmittedAtUtc
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error placing sell order for {request.Symbol}");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPost("limit")]
        public async Task<IActionResult> PlaceLimitOrder([FromBody] LimitOrderRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Symbol))
                {
                    return BadRequest(new { Error = "Symbol is required" });
                }

                if (request.Quantity <= 0)
                {
                    return BadRequest(new { Error = "Quantity must be greater than 0" });
                }

                if (request.LimitPrice <= 0)
                {
                    return BadRequest(new { Error = "LimitPrice must be greater than 0" });
                }

                var orderRequest = new NewOrderRequest(
                    request.Symbol,
                    OrderQuantity.Fractional(request.Quantity),
                    request.Side,
                    OrderType.Limit,
                    TimeInForce.Gtc)
                {
                    LimitPrice = request.LimitPrice
                };

                var order = await _alpacaPaperClient.AlpacaTradingClient.PostOrderAsync(orderRequest);

                _logger.Info($"Limit order placed: {order.OrderId} for {request.Symbol} at {request.LimitPrice}");

                return Ok(new
                {
                    OrderId = order.OrderId,
                    Symbol = order.Symbol,
                    Side = order.OrderSide,
                    Type = order.OrderType,
                    Quantity = order.Quantity,
                    LimitPrice = order.LimitPrice,
                    Status = order.OrderStatus,
                    SubmittedAt = order.SubmittedAtUtc
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error placing limit order for {request.Symbol}");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpDelete("orders/{orderId}")]
        public async Task<IActionResult> CancelOrder(Guid orderId)
        {
            try
            {
                await _alpacaPaperClient.AlpacaTradingClient.CancelOrderAsync(orderId);
                _logger.Info($"Order cancelled: {orderId}");
                return Ok(new { Message = "Order cancelled successfully", OrderId = orderId });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error cancelling order {orderId}");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpDelete("orders")]
        public async Task<IActionResult> CancelAllOrders()
        {
            try
            {
                // Get all open orders (New, PartiallyFilled, PendingNew, AcceptedForBidding)
                var request = new ListOrdersRequest();
                var orders = await _alpacaPaperClient.AlpacaTradingClient.ListOrdersAsync(request);
                
                // Filter to only cancellable orders
                var cancellableOrders = orders.Where(o => 
                    o.OrderStatus == OrderStatus.New || 
                    o.OrderStatus == OrderStatus.PartiallyFilled ||
                    o.OrderStatus == OrderStatus.PendingNew ||
                    o.OrderStatus == OrderStatus.AcceptedForBidding
                ).ToList();

                if (cancellableOrders.Count == 0)
                {
                    return Ok(new { Message = "No open orders to cancel", CancelledCount = 0 });
                }

                var cancelledCount = 0;
                var errors = new List<string>();

                foreach (var order in cancellableOrders)
                {
                    try
                    {
                        await _alpacaPaperClient.AlpacaTradingClient.CancelOrderAsync(order.OrderId);
                        _logger.Info($"Order cancelled: {order.OrderId}");
                        cancelledCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, $"Error cancelling order {order.OrderId}");
                        errors.Add($"Failed to cancel order {order.OrderId}: {ex.Message}");
                    }
                }

                return Ok(new 
                { 
                    Message = $"Cancelled {cancelledCount} of {cancellableOrders.Count} orders",
                    CancelledCount = cancelledCount,
                    TotalCount = cancellableOrders.Count,
                    Errors = errors
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error cancelling all orders");
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }

    public class MarketOrderRequest
    {
        public string Symbol { get; set; } = "BTC/USD";
        public decimal Quantity { get; set; }
        public decimal Notional { get; set; }
    }

    public class LimitOrderRequest
    {
        public string Symbol { get; set; } = "BTC/USD";
        public OrderSide Side { get; set; } = OrderSide.Buy;
        public decimal Quantity { get; set; }
        public decimal LimitPrice { get; set; }
    }
}

