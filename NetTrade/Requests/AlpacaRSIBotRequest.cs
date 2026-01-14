using NetTrade.Models;

namespace NetTrade.Requests;

public class AlpacaRSIBotRequest
{
    public RSIStrategyConfiguration RSIStrategyConfiguration { get; set; } = new RSIStrategyConfiguration();
    public decimal InitialCapital { get; set; } = 1000;
    public int CheckIntervalSeconds { get; set; } = 60; // Check every minute for RSI
}

