using Microsoft.AspNetCore.Mvc;
using NLog;
using NetTrade.Models;
using NetTrade.Service;
using System;
using System.Threading.Tasks;
using ILogger = NLog.ILogger;

namespace NetTrade.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MovingAverageCrossoverBacktestController : ControllerBase
    {
        private readonly IMovingAverageCrossoverStrategyService _movingAverageCrossoverStrategyService;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public MovingAverageCrossoverBacktestController(IMovingAverageCrossoverStrategyService movingAverageCrossoverStrategyService)
        {
            _movingAverageCrossoverStrategyService = movingAverageCrossoverStrategyService;
        }

        [HttpPost]
        [Route("backtest")]
        public async Task<ActionResult<MovingAverageCrossoverBacktestResult>> RunBacktest([FromBody] MovingAverageCrossoverBacktestRequest request)
        {
            try
            {
                _logger.Info("Running Moving Average Crossover backtest with configuration: {Symbol}, Fast Period: {FastPeriod}, " +
                    "Slow Period: {SlowPeriod}",
                    request.Configuration?.Symbol,
                    request.Configuration?.FastPeriod,
                    request.Configuration?.SlowPeriod);

                var result = await _movingAverageCrossoverStrategyService.RunBacktest(request);

                if (result == null)
                {
                    _logger.Error("Moving Average Crossover backtest service returned null result");
                    return StatusCode(500, "Moving Average Crossover backtest service returned null result");
                }

                _logger.Info("Moving Average Crossover backtest completed with result: Profit: {Profit}, Total Trades: {Trades}, Win Rate: {WinRate}%",
                    result.TotalProfit, result.TotalTrades, result.WinRate);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error running Moving Average Crossover backtest");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}

