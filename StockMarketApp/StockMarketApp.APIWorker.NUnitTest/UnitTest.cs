using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shared.Models;
using System.Net;
using System.Text.Json;

namespace StockMarketApp.APIWorker.NUnitTest
{
    [TestFixture]
    public class Tests
    {
        private ILogger<Worker> _loggerMock;
        private IConfiguration _configMock;
        private CosmosClient _cosmosClientMock;
        private HttpClient _httpClientMock;
        private Database _dbMock;
        private Container _containerMock;

        [SetUp]
        public void Setup()
        {
            _loggerMock = Substitute.For<ILogger<Worker>>();
            _configMock = Substitute.For<IConfiguration>();
            _cosmosClientMock = Substitute.For<CosmosClient>();
            _httpClientMock = Substitute.For<HttpClient>();
            _dbMock = Substitute.For<Database>();
            _containerMock = Substitute.For<Container>();

            _configMock["CosmosDb:Database"].Returns("TestDb");
            _cosmosClientMock.GetDatabase(Arg.Any<string>()).Returns(_dbMock);
            _dbMock.GetContainer(Arg.Any<string>()).Returns(_containerMock);
        }

        [TearDown]
        public void TearDown()
        {
            _cosmosClientMock?.Dispose();
            _httpClientMock?.Dispose();
        }

        [Test]
        public async Task GetSymbolsList_ReturnsExpectedSymbols()
        {
            // Arrange
            var myStockHistoryList = new List<StockHistory>
            {
                new StockHistory("AAPL"),
                new StockHistory("PEP")
            };

            var feedIteratorMock = Substitute.For<FeedIterator<StockHistory>>();
            feedIteratorMock.HasMoreResults.Returns(true, false);
            feedIteratorMock.ReadNextAsync(Arg.Any<CancellationToken>())
                .Returns(new FeedResponseStub<StockHistory>(myStockHistoryList));
            _containerMock.GetItemQueryIterator<StockHistory>()
                .Returns(feedIteratorMock);

            var worker = new Worker(_loggerMock, _configMock, _cosmosClientMock, _httpClientMock);


            // Act
            var symbols = await worker.GetSymbolsList(true);

            // Assert
            symbols.Should().Contain("AAPL");
            symbols.Should().Contain("PEP");
        }

        [Test]
        public async Task AddStockSymbolToCosmos_ThrowsIfAlreadyTracked()
        {
            // Arrange
            var worker = new Worker(_loggerMock, _configMock, _cosmosClientMock, _httpClientMock);
            var stockHistories = new List<StockHistory> {
                new StockHistory("AAPL") { IsActivelyTracked = true }
            };
            var feedIteratorMock = Substitute.For<FeedIterator<StockHistory>>();
            feedIteratorMock.HasMoreResults.Returns(true, false);
            feedIteratorMock.ReadNextAsync(Arg.Any<CancellationToken>())
                .Returns(new FeedResponseStub<StockHistory>(stockHistories));
            _containerMock.GetItemQueryIterator<StockHistory>().Returns(feedIteratorMock);

            // Act & Assert
            await FluentActions.Invoking(() => worker.AddStockSymbolToCosmos("AAPL"))
                .Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task AddStockSymbolToCosmos_AddsNewStock()
        {
            // Arrange
            var worker = new Worker(_loggerMock, _configMock, _cosmosClientMock, _httpClientMock);
            var stockHistories = new List<StockHistory> { };
            var feedIteratorMock = Substitute.For<FeedIterator<StockHistory>>();
            feedIteratorMock.HasMoreResults.Returns(true, false);
            feedIteratorMock.ReadNextAsync(Arg.Any<CancellationToken>())
                .Returns(new FeedResponseStub<StockHistory>(stockHistories));

            _containerMock.GetItemQueryIterator<StockHistory>().Returns(feedIteratorMock);
            var itemResponseMock = Substitute.For<ItemResponse<StockHistory>>();
            itemResponseMock.StatusCode.Returns(HttpStatusCode.Created);
            itemResponseMock.RequestCharge.Returns(1.0);
            _containerMock.CreateItemAsync(Arg.Any<StockHistory>(), null, null, Arg.Any<CancellationToken>())
                .Returns(itemResponseMock);

            // Act
            var symbol = await worker.AddStockSymbolToCosmos("MSFT");

            // Assert
            symbol.Should().Be("MSFT");
        }

        [Test]
        public async Task GetSelectedStockHistory_ReturnsNullOnInvalidSymbol()
        {
            // Arrange
            var worker = new Worker(_loggerMock, _configMock, _cosmosClientMock, _httpClientMock);

            // Act
            var result = await worker.GetSelectedStockHistory("");

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task MarkStockUntracked_ThrowsOnMissingSymbol()
        {
            // Arrange
            var worker = new Worker(_loggerMock, _configMock, _cosmosClientMock, _httpClientMock);
            var itemResponseMock = Substitute.For<ItemResponse<StockHistory>>();
            itemResponseMock.Resource.Returns((StockHistory)null);
            _containerMock.ReadItemAsync<StockHistory>(Arg.Any<string>(), Arg.Any<PartitionKey>(), null, Arg.Any<CancellationToken>())
                .Returns(itemResponseMock);

            // Act & Assert
            await FluentActions.Invoking(() => worker.MarkStockUntracked("NOTEXIST"))
                .Should().ThrowAsync<NullReferenceException>();
        }

    }

    // Helper stub for FeedResponse
    public class FeedResponseStub<T> : FeedResponse<T>, IEnumerable<T>
    {
        private readonly List<T> _items;
        public FeedResponseStub(List<T> items) { _items = items; }
        public override IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
        public override int Count => _items.Count;
        public override string ContinuationToken => null;
        public override Headers Headers => null;
        public override double RequestCharge => 0;
        public override string ActivityId => null;
        public override IEnumerable<T> Resource => _items;
        public override CosmosDiagnostics Diagnostics => default(CosmosDiagnostics);
        public override string IndexMetrics => string.Empty;
        public override HttpStatusCode StatusCode => HttpStatusCode.OK;
    }

    public class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _sendAsync;

        public TestHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync)
        {
            _sendAsync = sendAsync;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _sendAsync(request, cancellationToken);
        }
    }
}