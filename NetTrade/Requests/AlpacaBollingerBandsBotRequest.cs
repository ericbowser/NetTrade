using NetTrade.Models;

namespace NetTrade.Requests;

public class AlpacaBollingerBandsBotRequest
{
    public BollingerBandsStrategyConfiguration BollingerBandsStrategyConfiguration { get; set; } = new BollingerBandsStrategyConfiguration();
    public decimal InitialCapital { get; set; } = 1000;
    public int CheckIntervalSeconds { get; set; } = 300; // Check every 5 minutes for Bollinger Bands
}

