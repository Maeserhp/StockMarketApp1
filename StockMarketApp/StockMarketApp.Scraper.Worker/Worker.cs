using HtmlAgilityPack;
using Shared.Models;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Azure.Cosmos;

namespace StockMarketApp.Scraper.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private readonly CosmosClient _cosmosClient;
        private Database _db;

        public Worker(ILogger<Worker> logger, IConfiguration configuration, CosmosClient cosmosClient)
        {
            _logger = logger;
            _configuration = configuration;
            _cosmosClient = cosmosClient;

            _db = _cosmosClient.GetDatabase(_configuration["CosmosDb:Database"]);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var stocksToQuery = await GetSymbolsList(stoppingToken);
            var existingStockHistories = await GetExistingStockHistories(stoppingToken);

            foreach (var stock in stocksToQuery)
            {
                //await AddStockSymbolsToCosmos(stock, stockSymbolContainer, stoppingToken);

                var result = await GetFinnhubQuoteAsync(stock.id);
                _logger.LogInformation($"Current price for {stock.id}: {result.CurrentPrice}");

                await UpsertQuoteHistory(stock.id, result, existingStockHistories);
            }
        }


        public async Task UpsertQuoteHistory(string symbol, Quote quote, List<StockHistory> existingStockHistories)
        {
            var quoteHistoryContainer = _db.GetContainer("StockHistory");

            StockHistory existingStockHistory = existingStockHistories.FirstOrDefault(x => x.id == symbol);

            if (existingStockHistory == null)
            {
                //The history for this stock has already started, so we can update the existing Stock History record
                StockHistory newHistory = new StockHistory(symbol, quote);

                try
                {
                    ItemResponse<StockHistory> response = await quoteHistoryContainer.CreateItemAsync(newHistory);

                    _logger.LogInformation($"Stock History for '{symbol}', attempted to ADD with statusCode '{response.StatusCode}'. '{response.RequestCharge}' RU's were used for this call");
                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"Error: {ex.Message}");
                }
            }
            else
            {
                //The history for this stock hasn't been started yet
                try
                {
                    ItemResponse<StockHistory> response = await quoteHistoryContainer.ReadItemAsync<StockHistory>(symbol, new PartitionKey(symbol));
                    existingStockHistory.QuoteHistory.Add(quote);
                    response = await quoteHistoryContainer.ReplaceItemAsync(existingStockHistory, existingStockHistory.id, new PartitionKey(existingStockHistory.id));
                    _logger.LogInformation($"Stock History for '{symbol}', attempted to UPDATE with statusCode '{response.StatusCode}'. '{response.RequestCharge}' RU's were used for this call");

                }
                catch (Exception ex) { 
                    _logger.LogInformation($"Error: {ex.Message}");
                }
            }
        }


        public async Task<List<StockHistory>> GetExistingStockHistories(CancellationToken cancellationToken)
        {
            var StockHistoryContainer = _db.GetContainer("StockHistory");

            List<StockHistory> stocks = new List<StockHistory>();

            FeedIterator<StockHistory> iterator = StockHistoryContainer.GetItemQueryIterator<StockHistory>();

            //iterates through each page of results
            while (iterator.HasMoreResults)
            {
                FeedResponse<StockHistory> response = await iterator.ReadNextAsync(cancellationToken);
                foreach (var item in response)
                {
                    _logger.LogInformation($"Found item: {item}");
                    stocks.Add(item);
                }
            }

            return stocks;
        }


        public async Task<List<StockSymbol>> GetSymbolsList(CancellationToken cancellationToken)
        {
            var stockSymbolContainer = _db.GetContainer("StockSymbols");

            List<StockSymbol> symbols = new List<StockSymbol>();

            //var query = new QueryDefinition("SELECT * FROM c");
            FeedIterator<StockSymbol> iterator = stockSymbolContainer.GetItemQueryIterator<StockSymbol>();

            //iterates through each page of results
            while (iterator.HasMoreResults)
            {
                FeedResponse<StockSymbol> response = await iterator.ReadNextAsync(cancellationToken);
                foreach (var item in response)
                {
                    _logger.LogInformation($"Found item: {item}");
                    symbols.Add( item );
                }
            }

            return symbols;
        }

        public async Task<Quote> GetFinnhubQuoteAsync(string symbol)
        {
            _logger.LogInformation($"Attempting to get a quote for {symbol}");

            var apiKey = _configuration["Finnhub_API_Key"];
            if (apiKey != null) {
                _logger.LogInformation("Succsessfuly obtained FinnHub API KEY");
            }

            string url = $"https://finnhub.io/api/v1/quote?symbol={symbol}&token={apiKey}";
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(url);
            _logger.LogInformation("Request sent to FinnHub");

            var responseMessage = response.EnsureSuccessStatusCode();
            _logger.LogInformation($"FinnHub responded with: {responseMessage.StatusCode} - {responseMessage.Content}");

            string json = await response.Content.ReadAsStringAsync();
            var quote = JsonSerializer.Deserialize<Quote>(json);
            quote.Date = DateTime.Today;
            return quote;
        }

        /// <summary>
        /// Add new stocks to the stock list that will have their price queried daily
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="stockSymbolContainer"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task AddStockSymbolsToCosmos(string symbol, CancellationToken cancellationToken)
        {
            try
            {
                var stockSymbolContainer = _db.GetContainer("StockSymbols");
                StockSymbol stock = new Shared.Models.StockSymbol(symbol);
                ItemResponse<StockSymbol> response = await stockSymbolContainer.CreateItemAsync(stock);

                _logger.LogInformation($"Stock Symbol '{symbol}', attempted to be added with statusCode '{response.StatusCode}'. '{response.RequestCharge}' RU's were used for this call");
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error: {ex.Message}");
            }
        }

    }
}
