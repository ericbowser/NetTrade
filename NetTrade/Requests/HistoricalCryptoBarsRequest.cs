using Alpaca.Markets;
using System;

namespace NetTrade.Requests;

public class BarsRequest 
{
    public DateTime From { get; set; } = DateTime.Now.AddDays(-1);
    public DateTime To { get; set; } = DateTime.Now;
    public TimeFrame Interval { get; set; } = Alpaca.Markets.TimeFrame.Day;
    public string[] Symbols { get; set; } = new[] { "BTC/USD" }; 
    public int Limit { get; set; } = 1000;
    public BarTimeFrame TimeFrame { get; set; } = new BarTimeFrame(1, BarTimeFrameUnit.Day);
    public SortDirection SortDirection { get; set; } = SortDirection.Descending;
}