// CoinAPI/Models/OrderRequests.cs

using Alpaca.Markets;
using NetTrade.Models;

namespace NetTrade.Requests
{
    public class MarketOrderRequest
    {
        public string ProductId { get; set; } = "BTC-USD";
        public string Side { get; set; } = "BUY";
        public decimal Size { get; set; } = 0.00M;
    }

    public class LimitOrderRequest
    {
        public string ProductId { get; set; } = "BTC-USD";
        public OrderSide OrderSide { get; set; } = OrderSide.Buy;
        public decimal Size { get; set; }
        public decimal LimitPrice { get; set; }
    }

    public class StartGridBotRequest
    {
        public GridTradingConfiguration? Configuration { get; set; }
        public decimal InitialCapital { get; set; } = 1000;
        public bool IsLive { get; set; } = false;
    }
}