using Alpaca.Markets;
using System;
using System.Collections.Generic;

namespace NetTrade.Models
{
    public class BollingerBandsStrategyConfiguration
    {
        public string Symbol { get; set; } = "BTC/USD";
        public string Timeframe { get; set; } = "15Min";
        public BarTimeFrame BarTimeframe { get; set; } = new BarTimeFrame(15, BarTimeFrameUnit.Minute);
        public int Period { get; set; } = 20; // Standard Bollinger Bands period
        public decimal StandardDeviations { get; set; } = 2.0m; // Standard deviation multiplier (typically 2)
        public decimal RiskPerTrade { get; set; } = 0.02m; // 2% of account per trade
        public decimal TakeProfitPercent { get; set; } = 2.0m; // 2% take profit
        public decimal StopLossPercent { get; set; } = 1.5m; // 1.5% stop loss
        public DateTime StartDate { get; set; } = DateTime.UtcNow.AddDays(-30);
        public DateTime EndDate { get; set; } = DateTime.UtcNow;
    }

    public class BollingerBandsCandleData
    {
        public DateTime Timestamp { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
        public decimal? MiddleBand { get; set; } // SMA (Simple Moving Average)
        public decimal? UpperBand { get; set; } // Middle + (StdDev * Multiplier)
        public decimal? LowerBand { get; set; } // Middle - (StdDev * Multiplier)
        public decimal? StandardDeviation { get; set; }
        public int Signal { get; set; } // 0: no signal, 1: buy, -1: sell
        public int Position { get; set; } // 0: no position, 1: long, -1: short
        public decimal EntryPrice { get; set; }
        public decimal ExitPrice { get; set; }
        public decimal PnL { get; set; }
        public decimal Equity { get; set; }
    }

    public class BollingerBandsTrade
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
        public decimal? LowerBandAtEntry { get; set; }
        public decimal? UpperBandAtEntry { get; set; }
        public decimal? LowerBandAtExit { get; set; }
        public decimal? UpperBandAtExit { get; set; }
    }

    public class BollingerBandsBacktestResult
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
        public List<BollingerBandsTrade> Trades { get; set; } = new List<BollingerBandsTrade>();
        public BollingerBandsStrategyConfiguration Configuration { get; set; } = new BollingerBandsStrategyConfiguration();
    }

    public class BollingerBandsBacktestRequest
    {
        public BollingerBandsStrategyConfiguration Configuration { get; set; } = new BollingerBandsStrategyConfiguration();
        public decimal InitialCapital { get; set; } = 10000;
    }
}

