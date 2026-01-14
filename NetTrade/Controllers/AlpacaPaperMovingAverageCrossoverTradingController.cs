using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Alpaca.Markets;
using Microsoft.AspNetCore.Mvc;
using NLog;
using NetTrade.Client;
using NetTrade.Requests;
using NetTrade.Service;
using ILogger = NLog.ILogger;

namespace NetTrade.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AlpacaPaperMovingAverageCrossoverTradingController : ControllerBase
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly IMovingAverageCrossoverStrategyService _maStrategyService;
        private readonly NetTrade.Service.IAlpacaCryptoDataClient _alpacaCryptoDataClient;
        private readonly IAlpacaPaperClient _alpacaPaperClient;
        private readonly IAlpacaTradingClient _alpacaTradingClient;

        private static readonly Dictionary<int, AlpacaMovingAverageCrossoverTradingService> _runningBots = new();
        private static int _nextBotId = 1;

        public AlpacaPaperMovingAverageCrossoverTradingController(
            IMovingAverageCrossoverStrategyService maStrategyService,
            IAlpacaPaperClient alpacaPaperClient,
            NetTrade.Service.IAlpacaCryptoDataClient alpacaCryptoDataClient)
        {
            _maStrategyService = maStrategyService;
            _alpacaPaperClient = alpacaPaperClient;
            _alpacaCryptoDataClient = alpacaCryptoDataClient;
            _alpacaTradingClient = alpacaPaperClient.AlpacaTradingClient;
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartMABot([FromBody] AlpacaMovingAverageCrossoverBotRequest request)
        {
            try
            {
                if (request.MovingAverageCrossoverStrategyConfiguration == null)
                {
                    return BadRequest("MovingAverageCrossoverStrategyConfiguration is required");
                }

                _logger.Info($"Starting Alpaca paper trading Moving Average Crossover bot for {request.MovingAverageCrossoverStrategyConfiguration.Symbol}");

                // Create bot instance with configuration from request
                var bot = new AlpacaMovingAverageCrossoverTradingService(
                    _alpacaCryptoDataClient,
                    _maStrategyService,
                    request.MovingAverageCrossoverStrategyConfiguration,
                    _alpacaTradingClient,
                    request.InitialCapital,
                    request.CheckIntervalSeconds
                );

                // Assign ID and start the bot
                int botId = _nextBotId++;

                // Store the bot
                _runningBots[botId] = bot;

                // Start the bot in background (fire and forget)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await bot.StartAsync(CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, $"Error in Moving Average Crossover bot {botId} execution");
                        _runningBots.Remove(botId);
                    }
                }, CancellationToken.None);

                return Ok(new
                {
                    BotId = botId,
                    request.MovingAverageCrossoverStrategyConfiguration.Symbol,
                    StartTime = DateTime.UtcNow,
                    Status = "Running"
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error starting Alpaca Moving Average Crossover bot");
                return StatusCode(500, $"Failed to start Moving Average Crossover bot: {ex.Message}");
            }
        }

        [HttpPost("{id}/stop")]
        public async Task<IActionResult> StopMABot(int id)
        {
            try
            {
                if (_runningBots.TryGetValue(id, out var bot))
                {
                    _logger.Info($"Stopping Moving Average Crossover bot {id}");

                    // Stop the bot
                    await bot.StopAsync(CancellationToken.None);

                    // Remove from running bots
                    _runningBots.Remove(id);

                    return Ok(new
                    {
                        Message = $"Moving Average Crossover bot {id} stopped successfully"
                    });
                }

                return NotFound($"Moving Average Crossover bot with ID {id} not found");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error stopping Moving Average Crossover bot {id}");
                return StatusCode(500, $"Failed to stop Moving Average Crossover bot: {ex.Message}");
            }
        }

        [HttpGet]
        public IActionResult GetAllRunningBots()
        {
            try
            {
                var bots = new List<object>();

                foreach (var kvp in _runningBots)
                {
                    bots.Add(new
                    {
                        BotId = kvp.Key,
                        Status = "Running"
                    });
                }

                return Ok(bots);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error getting running Moving Average Crossover bots");
                return StatusCode(500, $"Failed to get running bots: {ex.Message}");
            }
        }
    }
}

