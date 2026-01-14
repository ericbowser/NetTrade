using NetTrade.Models;

namespace NetTrade.Requests;

public class AlpacaMovingAverageCrossoverBotRequest
{
    public MovingAverageCrossoverStrategyConfiguration MovingAverageCrossoverStrategyConfiguration { get; set; } = new MovingAverageCrossoverStrategyConfiguration();
    public decimal InitialCapital { get; set; } = 1000;
    public int CheckIntervalSeconds { get; set; } = 300; // Check every 5 minutes for MA crossover
}

