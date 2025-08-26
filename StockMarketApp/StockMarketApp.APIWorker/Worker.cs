using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Shared.Models;
using System.Linq;
using System.Security.Policy;
using System.Text.Json;

namespace StockMarketApp.APIWorker
{
    public class Worker //: BackgroundService //Seems like this doesn't really need to be a background service as none of it's functions run coninuoulsy.
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private readonly CosmosClient _cosmosClient;

        private readonly Uri _baseUri = new Uri("https://finnhub.io/api/v1/");
        private readonly string _stockHistoryContainerName = "StockHistory";


        private Database _db;

        public Worker(ILogger<Worker> logger, IConfiguration configuration, CosmosClient cosmosClient)
        {
            _logger = logger;
            _configuration = configuration;
            _cosmosClient = cosmosClient;

            _db = _cosmosClient.GetDatabase(_configuration["CosmosDb:Database"]);
        }

        public async Task<List<string>> GetSymbolsList(CancellationToken cancellationToken = new CancellationToken())
        {
            var stockHistoryContainer = _db.GetContainer(_stockHistoryContainerName);

            List<StockHistory> StockHistories = new List<StockHistory>();

            //var query = new QueryDefinition("SELECT * FROM c");
            FeedIterator<StockHistory> iterator = stockHistoryContainer.GetItemQueryIterator<StockHistory>();

            //iterates through each page of results
            while (iterator.HasMoreResults)
            {
                FeedResponse<StockHistory> response = await iterator.ReadNextAsync(cancellationToken);
                foreach (var item in response)
                {
                    _logger.LogInformation($"Found item: {item}");
                    StockHistories.Add(item);
                }
            }

            List<string> symbols = new List<string>() { };
            foreach (var history in StockHistories) { 
                symbols.Add(history.id);
            }
            return symbols;
        }

        public async Task<Quote> GetFinnhubQuoteAsync(string symbol)
        {
            _logger.LogInformation($"Attempting to get a quote for {symbol}");

            var apiKey = _configuration["Finnhub_API_Key"];
            if (apiKey != null)
            {
                _logger.LogInformation("Succsessfuly obtained FinnHub API KEY");
            }

            //string url = $"https://finnhub.io/api/v1/quote?symbol={symbol}&token={apiKey}";
            string url = new Uri(_baseUri, $"quote?symbol={Uri.EscapeDataString(symbol)}&token={Uri.EscapeDataString(apiKey)}").ToString();

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
        /// Searches stocks that are similar to the query 
        /// </summary>
        /// <param name="query"></param>
        /// <returns>a list of stockSymbols that are simialr to the query</returns>
        public async Task<StockSearchResponse> FinnhubLookupSymbol(string query)
        {
            _logger.LogInformation($"Attempting to get a quote for {query}");

            var apiKey = _configuration["Finnhub_API_Key"];
            if (apiKey != null)
            {
                _logger.LogInformation("Succsessfuly obtained FinnHub API KEY");
            }

            var exchange = "US";
            string url = new Uri(_baseUri, $"search?q={Uri.EscapeDataString(query)}&exchange={Uri.EscapeDataString(exchange)}&token={Uri.EscapeDataString(apiKey)}").ToString();
            using var httpClient = new HttpClient();

            HttpResponseMessage response;
            try
            {
                _logger.LogInformation("Sending request to Finnhub: {Url}", url);
                response = await httpClient.GetAsync(url);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to Finnhub failed.");
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Finnhub returned non-success status code: {StatusCode}", response.StatusCode);
                return null;
            }

            _logger.LogInformation("Finnhub request successful with status code {StatusCode}", response.StatusCode);


            string json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<StockSearchResponse>(json);
          
            return data;
        }

        /// <summary>
        /// Add new stocks to the stock list that will have their price queried daily
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="stockSymbolContainer"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<string> AddStockSymbolToCosmos(string symbol, CancellationToken cancellationToken = new CancellationToken())
        {
            try
            {
                var stockHistoryContainer = _db.GetContainer(_stockHistoryContainerName);
                StockHistory stock = new Shared.Models.StockHistory(symbol);
                ItemResponse<StockHistory> response = await stockHistoryContainer.CreateItemAsync(stock);

                _logger.LogInformation($"Stock History '{symbol}', attempted to be added with statusCode '{response.StatusCode}'. '{response.RequestCharge}' RU's were used for this call");
                return symbol;
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error: {ex.Message}");
                throw ex;
            }
        }


        public async Task<int> PerformDailyQueries(CancellationToken cancellationToken = new CancellationToken())
        {
            var existingStockHistories = await GetExistingStockHistories(cancellationToken);

            var stocksToUpdate = existingStockHistories.Where(x => x.LastUpdated < DateTime.Today).ToList();

            var stocksUpdatedCount = 0;
            foreach (var stock in stocksToUpdate)
            {
                var result = await GetFinnhubQuoteAsync(stock.id);
                _logger.LogInformation($"Current price for {stock.id}: {result.CurrentPrice}");

                await UpsertQuoteHistory(stock.id, result, existingStockHistories);
                stocksUpdatedCount++;   
            }
            return stocksUpdatedCount;
        }


        private async Task UpsertQuoteHistory(string symbol, Quote quote, List<StockHistory> existingStockHistories)
        {
            var quoteHistoryContainer = _db.GetContainer(_stockHistoryContainerName);

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
                    existingStockHistory.LastUpdated = DateTime.Today;
                    response = await quoteHistoryContainer.ReplaceItemAsync(existingStockHistory, existingStockHistory.id, new PartitionKey(existingStockHistory.id));
                    _logger.LogInformation($"Stock History for '{symbol}', attempted to UPDATE with statusCode '{response.StatusCode}'. '{response.RequestCharge}' RU's were used for this call");

                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"Error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Gets the stock histories of all the stocks currently being tracked
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<List<StockHistory>> GetExistingStockHistories(CancellationToken cancellationToken)
        {
            var StockHistoryContainer = _db.GetContainer(_stockHistoryContainerName);

            List<StockHistory> stocks = new List<StockHistory>();

            FeedIterator<StockHistory> iterator = StockHistoryContainer.GetItemQueryIterator<StockHistory>();

            //iterates through each page of results
            while (iterator.HasMoreResults)
            {
                FeedResponse<StockHistory> response = await iterator.ReadNextAsync(cancellationToken);
                foreach (var item in response)
                {
                    _logger.LogInformation($"Found Tracked Stock: {item}");
                    stocks.Add(item);
                }
            }

            return stocks;
        }

        /// <summary>
        /// Gets the stock history for a specific stock symbol
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<StockHistory?> GetSelectedStockHistory(string symbol, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                _logger.LogWarning("GetSelectedStockHistory was called with an invalid symbol.");
                return null;
            }

            var stockHistoryContainer = _db?.GetContainer(_stockHistoryContainerName);
            if (stockHistoryContainer == null)
            {
                _logger.LogError("Stock history container is null. Check database initialization.");
                return null;
            }

            try
            {
                ItemResponse<StockHistory> response = await stockHistoryContainer.ReadItemAsync<StockHistory>(
                    symbol,
                    new PartitionKey(symbol),
                    cancellationToken: cancellationToken
                );

                if (response?.Resource == null)
                {
                    _logger.LogWarning("No stock history found for symbol: {Symbol}", symbol);
                    return null;
                }

                _logger.LogInformation("Found history for Stock: {StockId}", response.Resource.id);
                return response.Resource;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving stock history for symbol: {Symbol}", symbol);
                throw; 
            }
        }


    }
}
