using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using StockMarketApp.APIWorker;
using Shared.Models;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Net;

namespace StockMarketApp.APIWorker.UnitTests
{
    public class UnitTest
    {
        private readonly Mock<ILogger<Worker>> _loggerMock = new Mock<ILogger<Worker>>();
        private readonly Mock<IConfiguration> _configMock = new Mock<IConfiguration>();
        private readonly Mock<CosmosClient> _cosmosClientMock = new Mock<CosmosClient>();
        private readonly Mock<Database> _dbMock = new Mock<Database>();
        private readonly Mock<Container> _containerMock = new Mock<Container>();

        private Worker CreateWorkerWithContainer()
        {
            _configMock.Setup(c => c["CosmosDb:Database"]).Returns("TestDb");
            _cosmosClientMock.Setup(c => c.GetDatabase(It.IsAny<string>())).Returns(_dbMock.Object);
            _dbMock.Setup(db => db.GetContainer(It.IsAny<string>())).Returns(_containerMock.Object);
            return new Worker(_loggerMock.Object, _configMock.Object, _cosmosClientMock.Object);
        }

        [Fact]
        public async Task GetSymbolsList_ReturnsSymbolList_Mocked()
        {
            // Arrange
            var worker = CreateWorkerWithContainer();
            var stockHistories = new List<StockHistory> {
                new StockHistory("AAPL"),
                new StockHistory("MSFT")
            };
            // Simulate GetExistingStockHistories via container mock
            var feedIteratorMock = new Mock<FeedIterator<StockHistory>>();
            feedIteratorMock.SetupSequence(f => f.HasMoreResults)
                .Returns(true)
                .Returns(false);
            feedIteratorMock.Setup(f => f.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FeedResponseStub<StockHistory>(stockHistories));
            _containerMock.Setup(c => c.GetItemQueryIterator<StockHistory>(string.Empty, string.Empty, null))
                .Returns(feedIteratorMock.Object);

            worker.GetExistingStockHistories(true, CancellationToken.None);

            var result = await worker.GetSymbolsList();
            Assert.Equal(new List<string> { "AAPL", "MSFT" }, result);
        }

        [Fact]
        public async Task GetFinnhubQuoteAsync_ThrowsOnInvalidApiKey_VerifyLogging()
        {
            var worker = CreateWorkerWithContainer();
            _configMock.Setup(c => c["Finnhub_API_Key"]).Returns((string)null);
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await worker.GetFinnhubQuoteAsync("AAPL"));
            _loggerMock.Verify(l => l.Log(
                It.Is<LogLevel>(lvl => lvl == LogLevel.Information || lvl == LogLevel.Error),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task FinnhubLookupSymbol_ReturnsNullOnHttpError_VerifyLogging()
        {
            var worker = CreateWorkerWithContainer();
            _configMock.Setup(c => c["Finnhub_API_Key"]).Returns("testkey");
            // Simulate HTTP error by using an invalid API key
            var result = await worker.FinnhubLookupSymbol("AAPL");
            Assert.Null(result);
            _loggerMock.Verify(l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task AddStockSymbolToCosmos_ThrowsIfAlreadyTracked_Mocked()
        {
            var worker = CreateWorkerWithContainer();
            var stockHistories = new List<StockHistory> {
                new StockHistory("AAPL") { IsActivelyTracked = true }
            };
            var feedIteratorMock = new Mock<FeedIterator<StockHistory>>();
            feedIteratorMock.SetupSequence(f => f.HasMoreResults)
                .Returns(true)
                .Returns(false);
            feedIteratorMock.Setup(f => f.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FeedResponseStub<StockHistory>(stockHistories));
            _containerMock.Setup(c => c.GetItemQueryIterator<StockHistory>(string.Empty, string.Empty, null))
                .Returns(feedIteratorMock.Object);

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await worker.AddStockSymbolToCosmos("AAPL"));
        }

        [Fact]
        public async Task AddStockSymbolToCosmos_AddsNewStock_VerifyLogging()
        {
            var worker = CreateWorkerWithContainer();
            var stockHistories = new List<StockHistory> { };
            var feedIteratorMock = new Mock<FeedIterator<StockHistory>>();
            feedIteratorMock.SetupSequence(f => f.HasMoreResults)
                .Returns(true)
                .Returns(false);
            feedIteratorMock.Setup(f => f.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FeedResponseStub<StockHistory>(stockHistories));
            _containerMock.Setup(c => c.GetItemQueryIterator<StockHistory>(string.Empty, string.Empty, null))
                .Returns(feedIteratorMock.Object);
            var itemResponseMock = new Mock<ItemResponse<StockHistory>>();
            itemResponseMock.SetupGet(r => r.StatusCode).Returns(HttpStatusCode.Created);
            itemResponseMock.SetupGet(r => r.RequestCharge).Returns(1.0);
            _containerMock.Setup(c => c.CreateItemAsync(It.IsAny<StockHistory>(), null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(itemResponseMock.Object);

            var symbol = await worker.AddStockSymbolToCosmos("MSFT");
            Assert.Equal("MSFT", symbol);
            _loggerMock.Verify(l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task PerformDailyQueries_ReturnsInt_Mocked()
        {
            var worker = CreateWorkerWithContainer();
            var stockHistories = new List<StockHistory> {
                new StockHistory("AAPL") { IsActivelyTracked = true, LastUpdated = DateTime.Today.AddDays(-1) },
                new StockHistory("MSFT") { IsActivelyTracked = true, LastUpdated = DateTime.Today.AddDays(-2) }
            };
            var feedIteratorMock = new Mock<FeedIterator<StockHistory>>();
            feedIteratorMock.SetupSequence(f => f.HasMoreResults)
                .Returns(true)
                .Returns(false);
            feedIteratorMock.Setup(f => f.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FeedResponseStub<StockHistory>(stockHistories));
            _containerMock.Setup(c => c.GetItemQueryIterator<StockHistory>(string.Empty, string.Empty, null))
                .Returns(feedIteratorMock.Object);
            var itemResponseMock = new Mock<ItemResponse<StockHistory>>();
            itemResponseMock.SetupGet(r => r.StatusCode).Returns(HttpStatusCode.OK);
            itemResponseMock.SetupGet(r => r.RequestCharge).Returns(1.0);
            _containerMock.Setup(c => c.ReadItemAsync<StockHistory>(It.IsAny<string>(), It.IsAny<PartitionKey>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(itemResponseMock.Object);
            _containerMock.Setup(c => c.ReplaceItemAsync(It.IsAny<StockHistory>(), It.IsAny<string>(), It.IsAny<PartitionKey>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(itemResponseMock.Object);

            var result = await worker.PerformDailyQueries();
            Assert.True(result >= 0);
        }

        [Fact]
        public async Task GetSelectedStockHistory_ReturnsNullOnInvalidSymbol_VerifyLogging()
        {
            var worker = CreateWorkerWithContainer();
            var result = await worker.GetSelectedStockHistory("");
            Assert.Null(result);
            _loggerMock.Verify(l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task MarkStockUntracked_ThrowsOnMissingSymbol_Mocked()
        {
            var worker = CreateWorkerWithContainer();
            // Simulate not found response
            var itemResponseMock = new Mock<ItemResponse<StockHistory>>();
            itemResponseMock.SetupGet(r => r.Resource).Returns((StockHistory)null);
            _containerMock.Setup(c => c.ReadItemAsync<StockHistory>(It.IsAny<string>(), It.IsAny<PartitionKey>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(itemResponseMock.Object);
            await Assert.ThrowsAsync<NullReferenceException>(async () => await worker.MarkStockUntracked("NOTEXIST"));
        }
    }

    // Helper stub for FeedResponse
    public class FeedResponseStub<T> : FeedResponse<T>, IEnumerable<T>
    {
        private readonly List<T> _items;
        public FeedResponseStub(List<T> items) { _items = items; }
        public override IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
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
}