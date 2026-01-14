using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NLog;
using NetTrade.Service;

namespace NetTrade.BackTest
{
    public class ScalpingBot : BackgroundService
    {
        private readonly string _symbol;
        private readonly decimal _tradeAmount;
        private readonly bool _isLive;
        private readonly TimeSpan _checkInterval;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly IScalpingStrategyService _scalpingService;

        public ScalpingBot(
            IScalpingStrategyService scalpingService,
            string symbol = "BTC/USD",
            decimal tradeAmount = 100,
            bool isLive = false,
            int checkIntervalSeconds = 60) // Check every minute
        {
            _symbol = symbol;
            _tradeAmount = tradeAmount;
            _isLive = isLive;
            _checkInterval = TimeSpan.FromSeconds(checkIntervalSeconds);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.Info($"Starting One-Minute Scalping Bot for {_symbol}");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.Info($"Checking for trading signals on {_symbol}");

                    try
                    {
                        
                        /*var trade = await _scalpingService.ExecuteStrategy(_symbol, _tradeAmount, _isLive);
                        
                        if (trade != null)
                        {
                            _logger.LogInformation($"Executed {trade.Direction} trade for {_symbol} at {trade.EntryPrice}");
                        }
                        else
                        {
                            _logger.LogInformation($"No trading signal for {_symbol} at this time");
                        }*/
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, $"Error executing strategy for {_symbol}");
                    }

                    await Task.Delay(_checkInterval, stoppingToken);
                }
            }
            catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unhandled exception in bot execution");
            }
            finally
            {
            }
        }
    }
}