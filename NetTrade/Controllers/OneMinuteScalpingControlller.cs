using Microsoft.AspNetCore.Mvc;
using NLog;
using NetTrade.Models;
using NetTrade.Service;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetTrade.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ScalpingController : ControllerBase
    {
        private readonly IScalpingStrategyService _scalpingStrategyService;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public ScalpingController(IScalpingStrategyService scalpingStrategyService)
        {
            _scalpingStrategyService = scalpingStrategyService;
        }

        [HttpPost]
        [Route("/api/scalping/backtest")]
        public async Task<ActionResult<BacktestResult>> RunBacktest([FromBody] BacktestRequest request)
        {
            try
            {
                _logger.Info("Running backtest with configuration: {0}", 
                    $"{request.Configuration.Symbol}, {request.Configuration.Timeframe}, " +
                    $"From: {request.Configuration.StartDate}, To: {request.Configuration.EndDate}");
                
                var result = await _scalpingStrategyService.RunBackTest(request);
                
                _logger.Info("Backtest completed with result: Profit: {0}, Win Rate: {1}%", 
                    result.TotalProfit, result.WinRate);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error running backtest");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("/api/scalping/historicalData")]
        public async Task<ActionResult<List<CandleData>>> GetHistoricalData(
            [FromQuery] string symbol, 
            [FromQuery] string timeframe, 
            [FromQuery] bool useHeikinAshi,
            [FromQuery] string startDate,
            [FromQuery] string endDate)
        {
            try
            {
                var config = new ScalpingStrategyConfiguration
                {
                    Symbol = symbol,
                    Timeframe = timeframe,
                    UseHeikinAshi = useHeikinAshi
                };
                
                // Parse dates if provided
                if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var parsedStartDate))
                {
                    config.StartDate = parsedStartDate;
                }
                
                if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var parsedEndDate))
                {
                    config.EndDate = parsedEndDate;
                }
                
                var safeSymbol = (config.Symbol ?? string.Empty)
                    .Replace("\r", string.Empty)
                    .Replace("\n", string.Empty);

                _logger.Info("Fetching historical data for {0}, {1}, From: {2}, To: {3}", 
                    safeSymbol, config.Timeframe, config.StartDate, config.EndDate);
                
                var data = await _scalpingStrategyService.GetHistoricalData(config);
                
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error fetching historical data");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}