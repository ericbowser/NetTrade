using Alpaca.Markets;
using System;
using System.Collections.Generic;


namespace NetTrade.Models
{
    public class ScalpingStrategyConfiguration
    {
        public string Symbol { get; set; } = "BTC/USD";
        public string Timeframe { get; set; } = "1Min";
        public BarTimeFrame BarTimeframe { get; set; } = new BarTimeFrame(1, BarTimeFrameUnit.Minute);
        public int SmaPeriod { get; set; } = 200;
        public int MacdFastPeriod { get; set; } = 12;
        public int MacdSlowPeriod { get; set; } = 26;
        public int MacdSignalPeriod { get; set; } = 9;
        public decimal RiskPerTrade { get; set; } = 0.02m; // 1% of account per trade
        public decimal TakeProfitPips { get; set; } = 8; // 0.15% for BTC
        public decimal StopLossPips { get; set; } = 8; // 0.10% for BTC
        public bool UseHeikinAshi { get; set; } = true; // Use Heikin-Ashi candles
        public DateTime StartDate { get; set; } = DateTime.UtcNow.AddDays(-2);
        public DateTime EndDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// keep track of epoch tics?
        /// </summary>
        public long Ticks { get; private set; }
    }

    public class CandleData
    {
        public DateTime Timestamp { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
        public decimal? Sma200 { get; set; }
        public decimal? Macd { get; set; }
        public decimal? MacdSignal { get; set; }
        public decimal? MacdHistogram { get; set; }
        public int Trend { get; set; } // 1: uptrend, -1: downtrend
        public int Signal { get; set; } // 0: no signal, 1: buy, -1: sell
        public int Position { get; set; } // 0: no position, 1: long, -1: short
        public decimal EntryPrice { get; set; }
        public decimal ExitPrice { get; set; }
        public decimal PnL { get; set; }
        public decimal Equity { get; set; }
    }

    public class Trade
    {
        public DateTime EntryTime { get; set; }
        public decimal EntryPrice { get; set; }
        public DateTime ExitTime { get; set; }
        public decimal ExitPrice { get; set; }
        public string Direction { get; set; } = string.Empty;// "LONG" or "SHORT"
        public decimal Size { get; set; }
        public decimal PnL { get; set; }
        public decimal PnLPct { get; set; }
        public string Result { get; set; } = string.Empty; // "WIN" or "LOSS"
        public decimal Equity { get; set; }
    }

    public class BacktestResult
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
        public List<Trade> Trades { get; set; } = new List<Trade>();
        public ScalpingStrategyConfiguration Configuration { get; set; } = new ScalpingStrategyConfiguration();
    }

    public class BacktestRequest
    {
        public ScalpingStrategyConfiguration Configuration { get; set; } = new ScalpingStrategyConfiguration();
        public decimal InitialCapital { get; set; } = 10000;
    }
}