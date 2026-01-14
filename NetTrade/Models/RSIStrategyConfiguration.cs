using Alpaca.Markets;
using System;
using System.Collections.Generic;

namespace NetTrade.Models
{
    public class RSIStrategyConfiguration
    {
        public string Symbol { get; set; } = "BTC/USD";
        public string Timeframe { get; set; } = "15Min";
        public BarTimeFrame BarTimeframe { get; set; } = new BarTimeFrame(15, BarTimeFrameUnit.Minute);
        public int RSIPeriod { get; set; } = 14; // Standard RSI period
        public decimal OversoldLevel { get; set; } = 30m; // RSI level for oversold (buy signal)
        public decimal OverboughtLevel { get; set; } = 70m; // RSI level for overbought (sell signal)
        public decimal RiskPerTrade { get; set; } = 0.02m; // 2% of account per trade
        public decimal TakeProfitPercent { get; set; } = 2.0m; // 2% take profit
        public decimal StopLossPercent { get; set; } = 1.5m; // 1.5% stop loss
        public bool UseRSIDivergence { get; set; } = false; // Optional: Use RSI divergence signals
        public DateTime StartDate { get; set; } = DateTime.UtcNow.AddDays(-30);
        public DateTime EndDate { get; set; } = DateTime.UtcNow;
    }

    public class RSICandleData
    {
        public DateTime Timestamp { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
        public decimal? RSI { get; set; }
        public int Signal { get; set; } // 0: no signal, 1: buy, -1: sell
        public int Position { get; set; } // 0: no position, 1: long, -1: short
        public decimal EntryPrice { get; set; }
        public decimal ExitPrice { get; set; }
        public decimal PnL { get; set; }
        public decimal Equity { get; set; }
    }

    public class RSITrade
    {
        public DateTime EntryTime { get; set; }
        public decimal EntryPrice { get; set; }
        public DateTime ExitTime { get; set; }
        public decimal ExitPrice { get; set; }
        public string Direction { get; set; } = string.Empty; // "LONG" or "SHORT"
        public decimal Size { get; set; }
        public decimal PnL { get; set; }
        public decimal PnLPct { get; set; }
        public string Result { get; set; } = string.Empty; // "WIN" or "LOSS"
        public decimal Equity { get; set; }
        public decimal? RSIAtEntry { get; set; }
        public decimal? RSIAtExit { get; set; }
    }

    public class RSIBacktestResult
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
        public List<RSITrade> Trades { get; set; } = new List<RSITrade>();
        public RSIStrategyConfiguration Configuration { get; set; } = new RSIStrategyConfiguration();
    }

    public class RSIBacktestRequest
    {
        public RSIStrategyConfiguration Configuration { get; set; } = new RSIStrategyConfiguration();
        public decimal InitialCapital { get; set; } = 10000;
    }
}

