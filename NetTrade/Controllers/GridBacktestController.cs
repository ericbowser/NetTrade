// NetTrade/Controllers/GridTradingController.cs
using Microsoft.AspNetCore.Mvc;
using NLog;
using NetTrade.Models;
using NetTrade.Service;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ILogger = NLog.ILogger;

namespace NetTrade.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GridBacktestController : ControllerBase
    {
        private readonly IGridBacktestService _gridBacktestService;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public GridBacktestController(IGridBacktestService gridBacktestService)
        {
            _gridBacktestService = gridBacktestService;
        }

        [HttpPost]
        [Route("backtest")]
        public async Task<ActionResult<GridBacktestResult>> RunBacktest([FromBody] GridBacktestRequest request)
        {
            try
            {
                _logger.Info("Running grid trading backtest with configuration: {0}",
                    $"{request.Configuration?.Symbol}, {request.Configuration?.Timeframe}, " +
                    $"Grid Levels: {request.Configuration?.GridLevels}, Grid Range: {request.Configuration?.GridRange}%");

                _logger.Info("Calling grid backtest service...");
                var result = await _gridBacktestService.RunBacktest(request);

                _logger.Info("Backtest service returned successfully. Result received.");
                _logger.Info("Backtest completed with result: Profit: {0}, Total Trades: {1}",
                    result?.TotalProfit ?? 0, result?.TotalTrades ?? 0);

                if (result == null)
                {
                    _logger.Error("Backtest service returned null result");
                    return StatusCode(500, "Backtest service returned null result");
                }

                _logger.Info("Serializing result to JSON...");
                var response = Ok(result);
                _logger.Info("Result serialized successfully. Returning response.");
                return response;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error running grid trading backtest");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("/api/grid/levels")]
        public async Task<ActionResult<List<GridLevel>>> GetGridLevels(
            [FromQuery] string symbol = "BTC/USD",
            [FromQuery] int gridLevels = 10,
            [FromQuery] decimal gridRange = 5,
            [FromQuery] decimal orderSize = 100)
        {
            try
            {
                var config = new GridTradingConfiguration
                {
                    Symbol = symbol,
                    GridLevels = gridLevels,
                    GridRange = gridRange,
                    OrderSize = orderSize
                };

                _logger.Info("Calculating grid levels for {0}, Levels: {1}, Range: {2}%",
                    config.Symbol, config.GridLevels, config.GridRange);

                var grid = await _gridBacktestService.CalculateGridLevels(config);

                return Ok(grid);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error calculating grid levels");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}