using Alpaca.Markets;
using System;
using System.Collections.Generic;

namespace NetTrade.Models
{
    public class MovingAverageCrossoverStrategyConfiguration
    {
        public string Symbol { get; set; } = "BTC/USD";
        public string Timeframe { get; set; } = "1Hour";
        public BarTimeFrame BarTimeframe { get; set; } = new BarTimeFrame(1, BarTimeFrameUnit.Hour);
        public int FastPeriod { get; set; } = 50; // Fast moving average period (e.g., 50)
        public int SlowPeriod { get; set; } = 200; // Slow moving average period (e.g., 200)
        public decimal RiskPerTrade { get; set; } = 0.02m; // 2% of account per trade
        public decimal TakeProfitPercent { get; set; } = 3.0m; // 3% take profit
        public decimal StopLossPercent { get; set; } = 2.0m; // 2% stop loss
        public DateTime StartDate { get; set; } = DateTime.UtcNow.AddDays(-30);
        public DateTime EndDate { get; set; } = DateTime.UtcNow;
    }

    public class MovingAverageCrossoverCandleData
    {
        public DateTime Timestamp { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
        public decimal? FastMA { get; set; } // Fast moving average
        public decimal? SlowMA { get; set; } // Slow moving average
        public int Signal { get; set; } // 0: no signal, 1: buy (golden cross), -1: sell (death cross)
        public int Position { get; set; } // 0: no position, 1: long
        public decimal EntryPrice { get; set; }
        public decimal ExitPrice { get; set; }
        public decimal PnL { get; set; }
        public decimal Equity { get; set; }
    }

    public class MovingAverageCrossoverTrade
    {
        public DateTime EntryTime { get; set; }
        public decimal EntryPrice { get; set; }
        public DateTime ExitTime { get; set; }
        public decimal ExitPrice { get; set; }
        public string Direction { get; set; } = string.Empty; // "LONG"
        public decimal Size { get; set; }
        public decimal PnL { get; set; }
        public decimal PnLPct { get; set; }
        public string Result { get; set; } = string.Empty; // "WIN" or "LOSS"
        public decimal Equity { get; set; }
        public decimal? FastMAAtEntry { get; set; }
        public decimal? SlowMAAtEntry { get; set; }
        public decimal? FastMAAtExit { get; set; }
        public decimal? SlowMAAtExit { get; set; }
    }

    public class MovingAverageCrossoverBacktestResult
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
        public decimal MaxDrawdown { get; set; }
        public decimal MaxDrawdownPercent { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<MovingAverageCrossoverTrade> Trades { get; set; } = new List<MovingAverageCrossoverTrade>();
        public MovingAverageCrossoverStrategyConfiguration Configuration { get; set; } = new MovingAverageCrossoverStrategyConfiguration();
    }

    public class MovingAverageCrossoverBacktestRequest
    {
        public MovingAverageCrossoverStrategyConfiguration Configuration { get; set; } = new MovingAverageCrossoverStrategyConfiguration();
        public decimal InitialCapital { get; set; } = 10000;
    }
}

