using Microsoft.AspNetCore.Mvc;
using NLog;
using NetTrade.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetTrade.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AlpacaTestController : ControllerBase
    {
        private readonly IAlpacaCryptoDataClient _alpacaCryptoDataClient;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public AlpacaTestController(IAlpacaCryptoDataClient alpacaCryptoDataClient)
        {
            _alpacaCryptoDataClient = alpacaCryptoDataClient;
        }

        [HttpGet("account")]
        public async Task<IActionResult> GetAccount()
        {
            try
            {
                var account = await _alpacaCryptoDataClient.GetAccountAsync();
                return Ok(new
                {
                    AccountNumber = account.AccountNumber,
                    Equity = account.Equity,
                    BuyingPower = account.BuyingPower,
                    TradingBlocked = account.IsTradingBlocked,
                    AccountBlocked = account.IsAccountBlocked
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error getting account");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpGet("connection-test")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                var account = await _alpacaCryptoDataClient.GetAccountAsync();
                return Ok(new
                {
                    Success = true,
                    Message = "Alpaca Paper Trading connection successful",
                    AccountNumber = account.AccountNumber,
                    Equity = account.Equity
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Connection test failed");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Alpaca Paper Trading connection failed",
                    Error = ex.Message
                });
            }
        }

        [HttpGet("quotes/{symbol}")]
        public async Task<IActionResult> GetLatestQuote(string symbol)
        {
            try
            {
                var quotes = await _alpacaCryptoDataClient.ListLatestQuotesAsync(new List<string> { symbol });
                
                if (quotes != null && quotes.Count > 0)
                {
                    var quote = quotes.Values.FirstOrDefault();
                    if (quote != null)
                    {
                        return Ok(new
                        {
                            Symbol = symbol,
                            AskPrice = quote.AskPrice,
                            BidPrice = quote.BidPrice,
                            AskSize = quote.AskSize,
                            BidSize = quote.BidSize,
                            Timestamp = quote.TimestampUtc
                        });
                    }
                }
                
                return NotFound($"No quote found for symbol: {symbol}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error getting quote for {symbol}");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
    }
}
