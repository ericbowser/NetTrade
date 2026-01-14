using Alpaca.Markets;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NetTrade.Models
{
    public class GridTradingConfiguration
    {
        public string Symbol { get; set; } = "BTC/USD";
        public string Timeframe { get; set; } = "1Min";
        public int GridLevels { get; set; } // Number of grid levels
        public decimal GridRange { get; set; } // Range in percentage from center price
        public decimal OrderSize { get; set; } // Size of each order in USD
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; } = DateTime.UtcNow;
    }

    public class GridLevel
    {
        public int Level { get; set; }
        public decimal Price { get; set; }
        public OrderSide OrderSide { get; set; }
        public decimal OrderSize { get; set; }
    }

    public class GridTrade
    {
        public int GridLevel { get; set; }
        public decimal Price { get; set; }
        public decimal? EntryPrice { get; set; }
        public decimal? ExitPrice { get; set; }
        public decimal Size { get; set; }
        public OrderSide Direction { get; set; } // "BUY" or "SELL"
        public DateTime Timestamp { get; set; }
        public decimal PnL { get; set; }
        public decimal Equity { get; set; }
        public string Result { get; set; } = string.Empty; // "WIN", "LOSS", or empty for open positions
    }

    public class GridBacktestResult
    {
        public decimal InitialCapital { get; set; }
        public decimal FinalEquity { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal TotalProfitPercentage { get; set; }
        public int TotalTrades { get; set; }
        public int WinningTrades { get; set; }
        public int LosingTrades { get; set; }
        public decimal WinRate { get; set; }
        public decimal AverageWin { get; set; }
        public decimal AverageLoss { get; set; }
        public decimal ProfitFactor { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<GridTrade> Trades { get; set; } = new();
        [JsonIgnore]
        public List<GridLevel> GridLevels { get; set; } = new();
        public GridTradingConfiguration Configuration { get; set; } = new GridTradingConfiguration();
    }

    public class GridBacktestRequest
    {
        public GridTradingConfiguration? Configuration { get; set; }
        public decimal InitialCapital { get; set; } = 10000;
    }
}