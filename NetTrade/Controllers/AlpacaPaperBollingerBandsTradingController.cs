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
    public class AlpacaPaperBollingerBandsTradingController : ControllerBase
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly IBollingerBandsStrategyService _bbStrategyService;
        private readonly NetTrade.Service.IAlpacaCryptoDataClient _alpacaCryptoDataClient;
        private readonly IAlpacaPaperClient _alpacaPaperClient;
        private readonly IAlpacaTradingClient _alpacaTradingClient;

        private static readonly Dictionary<int, AlpacaBollingerBandsTradingService> _runningBots = new();
        private static int _nextBotId = 1;

        public AlpacaPaperBollingerBandsTradingController(
            IBollingerBandsStrategyService bbStrategyService,
            IAlpacaPaperClient alpacaPaperClient,
            NetTrade.Service.IAlpacaCryptoDataClient alpacaCryptoDataClient)
        {
            _bbStrategyService = bbStrategyService;
            _alpacaPaperClient = alpacaPaperClient;
            _alpacaCryptoDataClient = alpacaCryptoDataClient;
            _alpacaTradingClient = alpacaPaperClient.AlpacaTradingClient;
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartBollingerBandsBot([FromBody] AlpacaBollingerBandsBotRequest request)
        {
            try
            {
                if (request.BollingerBandsStrategyConfiguration == null)
                {
                    return BadRequest("BollingerBandsStrategyConfiguration is required");
                }

                _logger.Info($"Starting Alpaca paper trading Bollinger Bands bot for {request.BollingerBandsStrategyConfiguration.Symbol}");

                // Create bot instance with configuration from request
                var bot = new AlpacaBollingerBandsTradingService(
                    _alpacaCryptoDataClient,
                    _bbStrategyService,
                    request.BollingerBandsStrategyConfiguration,
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
                        _logger.Error(ex, $"Error in Bollinger Bands bot {botId} execution");
                        _runningBots.Remove(botId);
                    }
                }, CancellationToken.None);

                return Ok(new
                {
                    BotId = botId,
                    request.BollingerBandsStrategyConfiguration.Symbol,
                    StartTime = DateTime.UtcNow,
                    Status = "Running"
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error starting Alpaca Bollinger Bands bot");
                return StatusCode(500, $"Failed to start Bollinger Bands bot: {ex.Message}");
            }
        }

        [HttpPost("{id}/stop")]
        public async Task<IActionResult> StopBollingerBandsBot(int id)
        {
            try
            {
                if (_runningBots.TryGetValue(id, out var bot))
                {
                    _logger.Info($"Stopping Bollinger Bands bot {id}");

                    // Stop the bot
                    await bot.StopAsync(CancellationToken.None);

                    // Remove from running bots
                    _runningBots.Remove(id);

                    return Ok(new
                    {
                        Message = $"Bollinger Bands bot {id} stopped successfully"
                    });
                }

                return NotFound($"Bollinger Bands bot with ID {id} not found");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error stopping Bollinger Bands bot {id}");
                return StatusCode(500, $"Failed to stop Bollinger Bands bot: {ex.Message}");
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
                _logger.Error(ex, "Error getting running Bollinger Bands bots");
                return StatusCode(500, $"Failed to get running bots: {ex.Message}");
            }
        }
    }
}

