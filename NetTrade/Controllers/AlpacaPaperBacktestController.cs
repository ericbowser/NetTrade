using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alpaca.Markets;
using Microsoft.AspNetCore.Mvc;
using NLog;
using NetTrade.Requests;
using static Alpaca.Markets.AccountActivityType;
using IAlpacaCryptoDataClient = NetTrade.Service.IAlpacaCryptoDataClient;
using ILogger = NLog.ILogger;

namespace NetTrade.Controllers
{
    [Route("[controller]")]
    public class AlpacaPaperBacktestController : ControllerBase
    {
        private readonly IAlpacaCryptoDataClient _alpacaCryptoDataClient;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private readonly string? _apiKey;
        private readonly string? _apiSecret;

        public AlpacaPaperBacktestController(IAlpacaCryptoDataClient alpacaCryptoDataClient)
        {
            _alpacaCryptoDataClient = alpacaCryptoDataClient;
        }

        [HttpGet]
        [Route("/api/alpaca/account")]
        public async Task<IAccount> GetAccount()
        {
            try
            {
                var account = await _alpacaCryptoDataClient.GetAccountAsync();
                return account;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                throw;
            }
        }

        [HttpGet]
        [Route("/api/alpaca/getLatestTrades")]
        public async Task<IReadOnlyList<IOrder>> GetLatestTrades()
        {
            try
            {
                var orders = await 
                    _alpacaCryptoDataClient
                        .ListOrdersAsync(
                            new ListOrdersRequest().WithSymbol("BTC/USD").WithSymbol("BTC/USDC")
                            );
                return orders;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                throw;
            }
        }

        [HttpPost]
        [Route("/api/alpaca/historicalBars")]
        public async Task<IMultiPage<IBar>> GetHistoricalBars(BarsRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), "Request cannot be null.");
            }

            if (request.Symbols == null || !request.Symbols.Any())
            {
                throw new ArgumentNullException(nameof(request.Symbols), "Symbols cannot be null or empty.");
            }

            try
            {
                var barsRequest =
                    new HistoricalCryptoBarsRequest(request.Symbols, request.From, request.To, request.TimeFrame);

                var bars = await _alpacaCryptoDataClient.GetCryptoHistoricalDataAsync(barsRequest);
                return bars;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                throw;
            }
        }

        [HttpGet]
        [Route("/api/alpaca/transactions")]
        public async Task<IReadOnlyList<IAccountActivity>> GetTransactions(AccountActivitiesRequest request)
        {
            try
            {
                var type = new List<AccountActivityType>();
                type.Add(FeeInUsd);
                type.Add(CashDeposit);
                type.Add(Fill);
                type.Add(CashDeposit);
                type.Add(CashWithdrawal);
                type.Add(Transaction);
                var x = new AccountActivitiesRequest(type);
                // x.Date.Value.AddDays(-3);
                
                var activityWithParams = await _alpacaCryptoDataClient.GetAccountActivities(x);
                return activityWithParams;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                throw;
            }
        }

        [HttpGet]
        [Route("/api/alpaca/portfolio")]
        [Obsolete("Obsolete")]
        public async Task<IPortfolioHistory> GetPortfolioHistoryAsync()
        {
            try
            {
                var history = await _alpacaCryptoDataClient.GetPortfolioHistoryAsync(new PortfolioHistoryRequest
                {
                    TimeFrame = TimeFrame.Day,
                    ExtendedHours = true
                });
                return history;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                throw;
            }
        }
    }
}