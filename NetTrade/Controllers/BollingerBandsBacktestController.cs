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
    public class BollingerBandsBacktestController : ControllerBase
    {
        private readonly IBollingerBandsStrategyService _bollingerBandsStrategyService;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public BollingerBandsBacktestController(IBollingerBandsStrategyService bollingerBandsStrategyService)
        {
            _bollingerBandsStrategyService = bollingerBandsStrategyService;
        }

        [HttpPost]
        [Route("backtest")]
        public async Task<ActionResult<BollingerBandsBacktestResult>> RunBacktest([FromBody] BollingerBandsBacktestRequest request)
        {
            try
            {
                _logger.Info("Running Bollinger Bands backtest with configuration: {Symbol}, Period: {Period}, " +
                    "Standard Deviations: {StdDev}",
                    request.Configuration?.Symbol,
                    request.Configuration?.Period,
                    request.Configuration?.StandardDeviations);

                var result = await _bollingerBandsStrategyService.RunBacktest(request);

                if (result == null)
                {
                    _logger.Error("Bollinger Bands backtest service returned null result");
                    return StatusCode(500, "Bollinger Bands backtest service returned null result");
                }

                _logger.Info("Bollinger Bands backtest completed with result: Profit: {Profit}, Total Trades: {Trades}, Win Rate: {WinRate}%",
                    result.TotalProfit, result.TotalTrades, result.WinRate);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error running Bollinger Bands backtest");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}

