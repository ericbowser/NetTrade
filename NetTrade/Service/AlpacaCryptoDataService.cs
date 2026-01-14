using Alpaca.Markets;
using NLog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NetTrade.Client;

namespace NetTrade.Service
{
    public interface IAlpacaCryptoDataClient
    {
        Task<IReadOnlyList<IOrder>> ListOrdersAsync(ListOrdersRequest request);
        Task<IReadOnlyList<IAccountActivity>> GetAccountActivities(AccountActivitiesRequest request);
        Task<IMultiPage<IBar>> GetCryptoHistoricalDataAsync(HistoricalCryptoBarsRequest barsRequest);
        Task<IAccount> GetAccountAsync(CancellationToken cancellationToken = default);
        Task<IPortfolioHistory> GetPortfolioHistoryAsync(PortfolioHistoryRequest request);
        Task<IReadOnlyDictionary<string, IQuote>> ListLatestQuotesAsync(List<string>? symbols = null);
    }

    public sealed class AlpacaCryptoDataService : IAlpacaCryptoDataClient
    {
        private readonly IAlpacaPaperClient _alpacaPaperClient;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public AlpacaCryptoDataService(IAlpacaPaperClient alpacaPaperClient)
        {
            _alpacaPaperClient = alpacaPaperClient;
            _logger.Info("Received injected paper client with type {TradingClient} and {CryptoClient}",
                typeof(IAlpacaTradingClient).FullName, typeof(AlpacaCryptoDataService).FullName);
        }

        public async Task<IReadOnlyList<IOrder>> ListOrdersAsync(ListOrdersRequest listOrdersRequest)
        {
            try
            {
                var btcorders =
                    await _alpacaPaperClient.AlpacaTradingClient
                        .ListOrdersAsync(listOrdersRequest);
                return btcorders;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                throw;
            }
        }

        public async Task<IReadOnlyList<IAccountActivity>> GetAccountActivities(AccountActivitiesRequest request)
        {
            try
            {
                var activity =
                    await _alpacaPaperClient.AlpacaTradingClient?.ListAccountActivitiesAsync(request,
                        CancellationToken.None)!;

                return activity;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                throw;
            }
        }

        public async Task<IAccount> GetAccountAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var account = await _alpacaPaperClient.AlpacaTradingClient?.GetAccountAsync(cancellationToken)!;
                return account;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                throw;
            }
        }

        public async Task<IMultiPage<IBar>> GetCryptoHistoricalDataAsync(HistoricalCryptoBarsRequest barsRequest)
        {
            try
            {
                var bars = await _alpacaPaperClient.AlpacaCryptoDataClient.GetHistoricalBarsAsync(barsRequest);
                return bars;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                throw;
            }
        }

        public async Task<IPortfolioHistory> GetPortfolioHistoryAsync(PortfolioHistoryRequest request)
        {
            try
            {
                var portfolio =
                    await _alpacaPaperClient.AlpacaTradingClient?.GetPortfolioHistoryAsync(request,
                        CancellationToken.None)!;
                return portfolio;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                throw;
            }
        }

        public async Task<IReadOnlyDictionary<string, IQuote>> ListLatestQuotesAsync(List<string>? symbols = null)
        {
            try
            {
                // Use provided symbols or default to empty list
                var symbolsToRequest = symbols ?? new List<string>();
                
                var quotes = await _alpacaPaperClient.AlpacaCryptoDataClient.ListLatestQuotesAsync(
                    new LatestDataListRequest(symbolsToRequest));

                return quotes;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error fetching latest quotes");
                throw;
            }
        }
    }
}