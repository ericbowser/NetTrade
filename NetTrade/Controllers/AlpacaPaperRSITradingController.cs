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
    public class AlpacaPaperRSITradingController : ControllerBase
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly IRSIStrategyService _rsiStrategyService;
        private readonly NetTrade.Service.IAlpacaCryptoDataClient _alpacaCryptoDataClient;
        private readonly IAlpacaPaperClient _alpacaPaperClient;
        private readonly IAlpacaTradingClient _alpacaTradingClient;

        private static readonly Dictionary<int, AlpacaRSITradingService> _runningBots = new();
        private static int _nextBotId = 1;

        public AlpacaPaperRSITradingController(
            IRSIStrategyService rsiStrategyService,
            IAlpacaPaperClient alpacaPaperClient,
            NetTrade.Service.IAlpacaCryptoDataClient alpacaCryptoDataClient)
        {
            _rsiStrategyService = rsiStrategyService;
            _alpacaPaperClient = alpacaPaperClient;
            _alpacaCryptoDataClient = alpacaCryptoDataClient;
            _alpacaTradingClient = alpacaPaperClient.AlpacaTradingClient;
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartRSIBot([FromBody] AlpacaRSIBotRequest request)
        {
            try
            {
                if (request.RSIStrategyConfiguration == null)
                {
                    return BadRequest("RSIStrategyConfiguration is required");
                }

                _logger.Info($"Starting Alpaca paper trading RSI bot for {request.RSIStrategyConfiguration.Symbol}");

                // Create bot instance with configuration from request
                var bot = new AlpacaRSITradingService(
                    _alpacaCryptoDataClient,
                    _rsiStrategyService,
                    request.RSIStrategyConfiguration,
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
                        _logger.Error(ex, $"Error in RSI bot {botId} execution");
                        _runningBots.Remove(botId);
                    }
                }, CancellationToken.None);

                return Ok(new
                {
                    BotId = botId,
                    request.RSIStrategyConfiguration.Symbol,
                    StartTime = DateTime.UtcNow,
                    Status = "Running"
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error starting Alpaca RSI bot");
                return StatusCode(500, $"Failed to start RSI bot: {ex.Message}");
            }
        }

        [HttpPost("{id}/stop")]
        public async Task<IActionResult> StopRSIBot(int id)
        {
            try
            {
                if (_runningBots.TryGetValue(id, out var bot))
                {
                    _logger.Info($"Stopping RSI bot {id}");

                    // Stop the bot
                    await bot.StopAsync(CancellationToken.None);

                    // Remove from running bots
                    _runningBots.Remove(id);

                    return Ok(new
                    {
                        Message = $"RSI bot {id} stopped successfully"
                    });
                }

                return NotFound($"RSI bot with ID {id} not found");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error stopping RSI bot {id}");
                return StatusCode(500, $"Failed to stop RSI bot: {ex.Message}");
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
                _logger.Error(ex, "Error getting running RSI bots");
                return StatusCode(500, $"Failed to get running bots: {ex.Message}");
            }
        }
    }
}

