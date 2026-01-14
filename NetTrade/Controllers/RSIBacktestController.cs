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
    public class RSIBacktestController : ControllerBase
    {
        private readonly IRSIStrategyService _rsiStrategyService;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public RSIBacktestController(IRSIStrategyService rsiStrategyService)
        {
            _rsiStrategyService = rsiStrategyService;
        }

        [HttpPost]
        [Route("backtest")]
        public async Task<ActionResult<RSIBacktestResult>> RunBacktest([FromBody] RSIBacktestRequest request)
        {
            try
            {
                _logger.Info("Running RSI backtest with configuration: {Symbol}, RSI Period: {Period}, " +
                    "Oversold: {Oversold}, Overbought: {Overbought}",
                    request.Configuration?.Symbol,
                    request.Configuration?.RSIPeriod,
                    request.Configuration?.OversoldLevel,
                    request.Configuration?.OverboughtLevel);

                var result = await _rsiStrategyService.RunBacktest(request);

                if (result == null)
                {
                    _logger.Error("RSI backtest service returned null result");
                    return StatusCode(500, "RSI backtest service returned null result");
                }

                _logger.Info("RSI backtest completed with result: Profit: {Profit}, Total Trades: {Trades}, Win Rate: {WinRate}%",
                    result.TotalProfit, result.TotalTrades, result.WinRate);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error running RSI backtest");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}

