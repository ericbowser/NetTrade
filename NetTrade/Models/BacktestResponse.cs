// In C:\Projects\CoinApi\CoinAPI\Models\BacktestResponse.cs

using System;
using System.Collections.Generic;

namespace NetTrade.Models
{
    public class BacktestResponse
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
    }
}
