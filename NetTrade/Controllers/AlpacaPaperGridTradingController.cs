using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Alpaca.Markets;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using NetTrade.Client;
using NetTrade.Requests;
using NetTrade.Service;

namespace NetTrade.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AlpacaPaperGridTradingController : ControllerBase
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly IGridBacktestService _gridService;
        private readonly NetTrade.Service.IAlpacaCryptoDataClient _alpacaCryptoDataClient;
        private readonly IAlpacaPaperClient _alpacaPaperClient;
        private readonly IServiceProvider _serviceProvider;
        private readonly IAlpacaTradingClient _alpacaTradingClient;

        private static readonly Dictionary<int, AlpacaGridTradingService> _runningBots = new();
        private static int _nextBotId = 1;

        public AlpacaPaperGridTradingController(
            IGridBacktestService gridService, 
            IAlpacaPaperClient alpacaPaperClient, 
            IServiceProvider serviceProvider,
            NetTrade.Service.IAlpacaCryptoDataClient alpacaCryptoDataClient)
        {
            _gridService = gridService;
            _alpacaPaperClient = alpacaPaperClient;
            _serviceProvider = serviceProvider;
            _alpacaCryptoDataClient = alpacaCryptoDataClient;
            _alpacaTradingClient = alpacaPaperClient.AlpacaTradingClient;
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartGridBot([FromBody] AlpacaGridBotRequest request)
        {
            try
            {
                if (request.GridTradingConfiguration == null)
                {
                    return BadRequest("GridTradingConfiguration is required");
                }

                _logger.Info((object)$"Starting Alpaca paper trading grid bot for {request.GridTradingConfiguration.Symbol}");

                // Create bot instance with configuration from request
                // Use existing dependencies from controller
                var bot = new AlpacaGridTradingService(
                    _alpacaCryptoDataClient,
                    _gridService,
                    request.GridTradingConfiguration,
                    _alpacaTradingClient,
                    request.InitialCapital,
                    request.CheckIntervalSeconds
                );

                // Assign ID and start the bot
                int botId = _nextBotId++;

                // Store the bot
                _runningBots[botId] = bot;

                // Start the bot
                await bot.StartAsync(CancellationToken.None);

                return Ok(new
                {
                    BotId = botId,
                    request.GridTradingConfiguration.Symbol,
                    StartTime = DateTime.UtcNow,
                    Status = "Running"
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error starting Alpaca grid bot");
                return StatusCode(500, $"Failed to start grid bot: {ex.Message}");
            }
        }

        [HttpPost("{id}/stop")]
        public async Task<IActionResult> StopGridBot(int id)
        {
            try
            {
                if (_runningBots.TryGetValue(id, out var bot))
                {
                    _logger.Info($"Stopping grid bot {id}");

                    // Stop the bot
                    await bot.StopAsync();
                    await bot.StopAsync(CancellationToken.None);

                    // Remove from running bots
                    _runningBots.Remove(id);

                    return Ok(new
                    {
                        Message = $"Grid bot {id} stopped successfully"
                    });
                }

                return NotFound($"Grid bot with ID {id} not found");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error stopping grid bot {id}");
                return StatusCode(500, $"Failed to stop grid bot: {ex.Message}");
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
                _logger.Error(ex, "Error getting running bots");
                return StatusCode(500, $"Failed to get running bots: {ex.Message}");
            }
        }
    }
}