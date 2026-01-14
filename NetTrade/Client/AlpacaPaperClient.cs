using System;
using Alpaca.Markets;
using Microsoft.Extensions.Configuration;
using NLog;

namespace NetTrade.Client
{
    public interface IAlpacaPaperClient
    {
        IAlpacaCryptoDataClient AlpacaCryptoDataClient { get; }
        IAlpacaTradingClient AlpacaTradingClient { get; }
    }
    
    public class AlpacaPaperClient : IAlpacaPaperClient, IDisposable
    {
        private readonly IAlpacaTradingClient _alpacaTradingClient;
        private readonly IAlpacaCryptoDataClient _alpacaCryptoDataClient;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private bool _disposedValue;

        public AlpacaPaperClient(IConfiguration config)
        {
            var c = config.GetSection("AlpacaPaper");
            var apiKey = c["ApiKey"];
            var apiSecret = c["Secret"];

            _alpacaTradingClient = Environments.Paper.GetAlpacaTradingClient(
                new SecretKey(apiKey, apiSecret)
            );

            _alpacaCryptoDataClient = Environments.Paper.GetAlpacaCryptoDataClient(
                new SecretKey(apiKey, apiSecret)
            );
            
            
            
            _logger.Info("AlpacaPaperClient created..");
        }

        public IAlpacaCryptoDataClient AlpacaCryptoDataClient => _alpacaCryptoDataClient;

        public IAlpacaTradingClient AlpacaTradingClient => _alpacaTradingClient;
        
        
        public void Dispose()
        {
            _alpacaTradingClient.Dispose();
            _alpacaCryptoDataClient.Dispose();
        }
    }
}