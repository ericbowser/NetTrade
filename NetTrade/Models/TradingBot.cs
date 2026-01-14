using System;
using System.Collections.Generic;

namespace NetTrade.Models
{
    public class TradingBot
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }
        public decimal TradeAmount { get; set; }
        public bool IsLive { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? StopTime { get; set; }
        public bool IsActive { get; set; }
        public string Strategy { get; set; }
        public BotStatus Status { get; set; }
        public List<TradeRecord> TradeHistory { get; set; } = new List<TradeRecord>();
        public decimal CurrentBalance { get; set; }
        public decimal StartingBalance { get; set; }
        public int TotalTrades { get; set; }
        public int WinningTrades { get; set; }
        public int LosingTrades { get; set; }
        public decimal WinRate => TotalTrades > 0 ? (decimal)WinningTrades / TotalTrades * 100 : 0;
        public decimal ProfitLoss => CurrentBalance - StartingBalance;
        public decimal ProfitLossPercentage => StartingBalance > 0 ? (ProfitLoss / StartingBalance) * 100 : 0;
    }

    public enum BotStatus
    {
        Stopped,
        Running,
        Paused,
        Error
    }

    public class TradeRecord
    {
        public int Id { get; set; }
        public int BotId { get; set; }
        public DateTime EntryTime { get; set; }
        public decimal EntryPrice { get; set; }
        public DateTime? ExitTime { get; set; }
        public decimal? ExitPrice { get; set; }
        public string Direction { get; set; } = string.Empty;
        public decimal Size { get; set; }
        public decimal? PnL { get; set; }
        public decimal? PnLPct { get; set; }
        public string Result { get; set; } = string.Empty; // "WIN", "LOSS", or "OPEN"
        public decimal StopLoss { get; set; }
        public decimal TakeProfit { get; set; }
        public string ExitReason { get; set; } = string.Empty; // "StopLoss", "TakeProfit", "Signal", "Manual"
    }

    public class BotConfiguration
    {
        public string Name { get; set; } = "One-Minute Scalping Bot";
        public string Symbol { get; set; } = "BTC/USD";
        public decimal TradeAmount { get; set; } = 100;
        public bool IsLive { get; set; } = false;
        public decimal InitialBalance { get; set; } = 10000;
        public ScalpingStrategyConfiguration StrategyConfig { get; set; } = new ScalpingStrategyConfiguration
        {
            UseHeikinAshi = true,
            Timeframe = "1Min"
        };
    }

    public class BotStatusResponse
    {
        public TradingBot Bot { get; set; } = new TradingBot();
        public TradeRecord CurrentTrade { get; set; } = new TradeRecord();
        public List<TradeRecord> RecentTrades { get; set; } = new List<TradeRecord>();
        public List<CandleData> RecentCandles { get; set; } = new List<CandleData>();
        public string LastError { get; set; } = string.Empty;
    }
}