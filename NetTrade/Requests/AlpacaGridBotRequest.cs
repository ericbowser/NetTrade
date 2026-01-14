using NetTrade.Models;

namespace NetTrade.Requests;

public class AlpacaGridBotRequest
{
    public GridTradingConfiguration GridTradingConfiguration { get; set; } = new GridTradingConfiguration();
    public decimal InitialCapital { get; set; } = 1000;
    public int CheckIntervalSeconds { get; set; } = 30;
}