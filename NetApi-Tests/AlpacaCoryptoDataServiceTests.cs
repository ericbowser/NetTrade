using Xunit;
using Moq;
using Alpaca.Markets;
using NetTrade.Client;
using NetTrade.Service;
using IAlpacaCryptoDataClient = Alpaca.Markets.IAlpacaCryptoDataClient;

namespace CoinApi_Tests
{
    public class AlpacaCryptoDataServiceTests
    {
        private readonly Mock<IAlpacaTradingClient> _mockTradingClient;
        private readonly Mock<IAlpacaCryptoDataClient> _mockAlpacaCryptoDataClient;
        private readonly Mock<IAlpacaPaperClient> _mockPaperClient;
        private readonly Mock<IOrder> _mockOrder;

        public AlpacaCryptoDataServiceTests()
        {
            _mockOrder = new Mock<IOrder>();
            _mockOrder.SetupGet(o => o.Symbol).Returns("BTC/USD");
            _mockOrder.Setup(x => x.OrderStatus).Returns(OrderStatus.Filled);

            _mockPaperClient = new Mock<IAlpacaPaperClient>();
            _mockAlpacaCryptoDataClient = new Mock<IAlpacaCryptoDataClient>();
            _mockTradingClient = new Mock<IAlpacaTradingClient>();
            
            _mockPaperClient.Setup(x => x.AlpacaCryptoDataClient).Returns(_mockAlpacaCryptoDataClient.Object);
            _mockPaperClient.Setup(x => x.AlpacaTradingClient).Returns(_mockTradingClient.Object);
        }

        // --- Test Methods ---

        [Fact]
        public async Task ListOrdersAsync_ShouldReturnOrders_WhenApiCallSucceeds()
        {
            // Arrange
            var expectedOrders = new List<IOrder> { _mockOrder.Object };
            ListOrdersRequest? capturedRequest = null;
            var callCount = 0;
            
            // Explicitly provide CancellationToken parameter to avoid expression tree issues with optional parameters
            _mockTradingClient.Setup(c => c.ListOrdersAsync(It.IsAny<ListOrdersRequest>(), It.IsAny<CancellationToken>()))
                .Callback<ListOrdersRequest, CancellationToken>((r, ct) => { capturedRequest = r; callCount++; })
                .ReturnsAsync(expectedOrders);

            var service = new AlpacaCryptoDataService(_mockPaperClient.Object);
            var request = new ListOrdersRequest();
            
            // Act
            var actualOrders = await service.ListOrdersAsync(request);

            // Assert
            Assert.NotNull(actualOrders);
            Assert.Single(actualOrders);
            Assert.Equal(OrderStatus.Filled, actualOrders.Single().OrderStatus);
            Assert.Equal("BTC/USD", actualOrders.Single().Symbol);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task ListOrdersAsync_ShouldLogErrorAndThrow_WhenApiCallFails()
        {
            // Arrange
            var expectedException = new ArgumentException("Bad symbol");
            var request = new ListOrdersRequest();
            var callCount = 0;

            // Explicitly provide CancellationToken parameter to avoid expression tree issues with optional parameters
            _mockTradingClient
                .Setup(c => c.ListOrdersAsync(It.IsAny<ListOrdersRequest>(), It.IsAny<CancellationToken>()))
                .Callback<ListOrdersRequest, CancellationToken>((r, ct) => callCount++)
                .ThrowsAsync(expectedException);

            var service = new AlpacaCryptoDataService(_mockPaperClient.Object);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<ArgumentException>(() =>
                service.ListOrdersAsync(request)
            );

            Assert.Same(expectedException, actualException);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task GetAccountAsync_ShouldReturnAccount_WhenApiCallSucceeds()
        {
            // Arrange
            var mockAccount = new Mock<IAccount>();
            mockAccount.SetupGet(a => a.AccountNumber).Returns("TEST123");
            mockAccount.SetupGet(a => a.Currency).Returns("USD");

            _mockTradingClient
                .Setup(c => c.GetAccountAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockAccount.Object);

            var service = new AlpacaCryptoDataService(_mockPaperClient.Object);

            // Act
            var actualAccount = await service.GetAccountAsync();

            // Assert
            Assert.NotNull(actualAccount);
            Assert.Equal("TEST123", actualAccount.AccountNumber);
            Assert.Equal("USD", actualAccount.Currency);
            _mockTradingClient.Verify(c => c.GetAccountAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAccountAsync_ShouldLogErrorAndThrow_WhenApiCallFails()
        {
            // Arrange
            var expectedException = new InvalidOperationException("API Error");
            var cancellationToken = CancellationToken.None;

            _mockTradingClient
                .Setup(c => c.GetAccountAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            var service = new AlpacaCryptoDataService(_mockPaperClient.Object);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.GetAccountAsync(cancellationToken)
            );

            Assert.Same(expectedException, actualException);
            _mockTradingClient.Verify(c => c.GetAccountAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAccountActivities_ShouldReturnActivities_WhenApiCallSucceeds()
        {
            // Arrange
            var mockActivity = new Mock<IAccountActivity>();
            mockActivity.SetupGet(a => a.ActivityType).Returns(AccountActivityType.Fill);
            var expectedActivities = new List<IAccountActivity> { mockActivity.Object };
            var request = new AccountActivitiesRequest();

            _mockTradingClient
                .Setup(c => c.ListAccountActivitiesAsync(
                    It.IsAny<AccountActivitiesRequest>(), 
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedActivities);

            var service = new AlpacaCryptoDataService(_mockPaperClient.Object);

            // Act
            var actualActivities = await service.GetAccountActivities(request);

            // Assert
            Assert.NotNull(actualActivities);
            Assert.Single(actualActivities);
            Assert.Equal(AccountActivityType.Fill, actualActivities.Single().ActivityType);
            _mockTradingClient.Verify(c => c.ListAccountActivitiesAsync(
                It.IsAny<AccountActivitiesRequest>(), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAccountActivities_ShouldLogErrorAndThrow_WhenApiCallFails()
        {
            // Arrange
            var expectedException = new HttpRequestException("Network Error");
            var request = new AccountActivitiesRequest();

            _mockTradingClient
                .Setup(c => c.ListAccountActivitiesAsync(
                    It.IsAny<AccountActivitiesRequest>(), 
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            var service = new AlpacaCryptoDataService(_mockPaperClient.Object);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<HttpRequestException>(() =>
                service.GetAccountActivities(request)
            );

            Assert.Same(expectedException, actualException);
            _mockTradingClient.Verify(c => c.ListAccountActivitiesAsync(
                It.IsAny<AccountActivitiesRequest>(), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetCryptoHistoricalDataAsync_ShouldReturnBars_WhenApiCallSucceeds()
        {
            // Arrange
            var barsRequest = new HistoricalCryptoBarsRequest("BTC/USD", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, BarTimeFrame.Day);
            var mockBar = new Mock<IBar>();
            mockBar.SetupGet(b => b.Symbol).Returns("BTC/USD");
            var barsList = new List<IBar> { mockBar.Object };
            var expectedBars = new Dictionary<string, IReadOnlyList<IBar>>
            {
                { "BTC/USD", barsList }
            };
            var mockMultiPage = new Mock<IMultiPage<IBar>>();
            mockMultiPage.Setup(p => p.Items).Returns(expectedBars);

            _mockAlpacaCryptoDataClient
                .Setup(c => c.GetHistoricalBarsAsync(
                    It.IsAny<HistoricalCryptoBarsRequest>(), 
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockMultiPage.Object);

            var service = new AlpacaCryptoDataService(_mockPaperClient.Object);

            // Act
            var actualBars = await service.GetCryptoHistoricalDataAsync(barsRequest);

            // Assert
            Assert.NotNull(actualBars);
            Assert.NotNull(actualBars.Items);
            Assert.Single(actualBars.Items);
            Assert.True(actualBars.Items.ContainsKey("BTC/USD"));
            Assert.Single(actualBars.Items["BTC/USD"]);
            Assert.Equal("BTC/USD", actualBars.Items["BTC/USD"].First().Symbol);
            _mockAlpacaCryptoDataClient.Verify(c => c.GetHistoricalBarsAsync(
                It.IsAny<HistoricalCryptoBarsRequest>(), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetCryptoHistoricalDataAsync_ShouldLogErrorAndThrow_WhenApiCallFails()
        {
            // Arrange
            var barsRequest = new HistoricalCryptoBarsRequest("BTC/USD", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, BarTimeFrame.Day);
            var expectedException = new HttpRequestException("Network Error");

            _mockAlpacaCryptoDataClient
                .Setup(c => c.GetHistoricalBarsAsync(
                    It.IsAny<HistoricalCryptoBarsRequest>(), 
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            var service = new AlpacaCryptoDataService(_mockPaperClient.Object);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<HttpRequestException>(() =>
                service.GetCryptoHistoricalDataAsync(barsRequest)
            );

            Assert.Same(expectedException, actualException);
            _mockAlpacaCryptoDataClient.Verify(c => c.GetHistoricalBarsAsync(
                It.IsAny<HistoricalCryptoBarsRequest>(), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetPortfolioHistoryAsync_ShouldReturnPortfolioHistory_WhenApiCallSucceeds()
        {
            // Arrange
            var request = new PortfolioHistoryRequest();
            var mockPortfolioHistory = new Mock<IPortfolioHistory>();
            mockPortfolioHistory.SetupGet(p => p.TimeFrame).Returns(TimeFrame.Day);

            _mockTradingClient
                .Setup(c => c.GetPortfolioHistoryAsync(
                    It.IsAny<PortfolioHistoryRequest>(), 
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockPortfolioHistory.Object);

            var service = new AlpacaCryptoDataService(_mockPaperClient.Object);

            // Act
            var actualPortfolio = await service.GetPortfolioHistoryAsync(request);

            // Assert
            Assert.NotNull(actualPortfolio);
            Assert.Equal(TimeFrame.Day, actualPortfolio.TimeFrame);
            _mockTradingClient.Verify(c => c.GetPortfolioHistoryAsync(
                It.IsAny<PortfolioHistoryRequest>(), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetPortfolioHistoryAsync_ShouldLogErrorAndThrow_WhenApiCallFails()
        {
            // Arrange
            var request = new PortfolioHistoryRequest();
            var expectedException = new InvalidOperationException("API Error");

            _mockTradingClient
                .Setup(c => c.GetPortfolioHistoryAsync(
                    It.IsAny<PortfolioHistoryRequest>(), 
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            var service = new AlpacaCryptoDataService(_mockPaperClient.Object);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.GetPortfolioHistoryAsync(request)
            );

            Assert.Same(expectedException, actualException);
            _mockTradingClient.Verify(c => c.GetPortfolioHistoryAsync(
                It.IsAny<PortfolioHistoryRequest>(), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ListLatestQuotesAsync_ShouldReturnQuotes_WhenApiCallSucceeds()
        {
            // Arrange
            var symbols = new List<string> { "BTC/USD" };
            var mockQuote = new Mock<IQuote>();
            mockQuote.SetupGet(q => q.Symbol).Returns("BTC/USD");
            var expectedQuotes = new Dictionary<string, IQuote>
            {
                { "BTC/USD", mockQuote.Object }
            };

            _mockAlpacaCryptoDataClient
                .Setup(c => c.ListLatestQuotesAsync(
                    It.IsAny<LatestDataListRequest>(), 
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedQuotes);

            var service = new AlpacaCryptoDataService(_mockPaperClient.Object);

            // Act
            var actualQuotes = await service.ListLatestQuotesAsync(symbols);

            // Assert
            Assert.NotNull(actualQuotes);
            Assert.Single(actualQuotes);
            Assert.True(actualQuotes.ContainsKey("BTC/USD"));
            Assert.Equal("BTC/USD", actualQuotes["BTC/USD"].Symbol);
            _mockAlpacaCryptoDataClient.Verify(c => c.ListLatestQuotesAsync(
                It.IsAny<LatestDataListRequest>(), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ListLatestQuotesAsync_ShouldReturnQuotes_WhenNoSymbolsProvided()
        {
            // Arrange
            var expectedQuotes = new Dictionary<string, IQuote>();

            _mockAlpacaCryptoDataClient
                .Setup(c => c.ListLatestQuotesAsync(
                    It.IsAny<LatestDataListRequest>(), 
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedQuotes);

            var service = new AlpacaCryptoDataService(_mockPaperClient.Object);

            // Act
            var actualQuotes = await service.ListLatestQuotesAsync(null);

            // Assert
            Assert.NotNull(actualQuotes);
            Assert.Empty(actualQuotes);
            _mockAlpacaCryptoDataClient.Verify(c => c.ListLatestQuotesAsync(
                It.IsAny<LatestDataListRequest>(), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ListLatestQuotesAsync_ShouldLogErrorAndThrow_WhenApiCallFails()
        {
            // Arrange
            var symbols = new List<string> { "BTC/USD" };
            var expectedException = new HttpRequestException("Network Error");

            _mockAlpacaCryptoDataClient
                .Setup(c => c.ListLatestQuotesAsync(
                    It.IsAny<LatestDataListRequest>(), 
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            var service = new AlpacaCryptoDataService(_mockPaperClient.Object);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<HttpRequestException>(() =>
                service.ListLatestQuotesAsync(symbols)
            );

            Assert.Same(expectedException, actualException);
            _mockAlpacaCryptoDataClient.Verify(c => c.ListLatestQuotesAsync(
                It.IsAny<LatestDataListRequest>(), 
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
