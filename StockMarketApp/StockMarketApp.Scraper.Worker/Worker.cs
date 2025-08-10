using HtmlAgilityPack;
using Shared.Models;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StockMarketApp.Scraper.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        private string apiKey = "d2bttn9r01qvh3vdaei0d2bttn9r01qvh3vdaeig";

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //NOTE: Enable this loop if you want the process to loop
            //while (!stoppingToken.IsCancellationRequested)
            //{

            //TODO: Store this list of symbols in CosmosDB and read them in here.
            var stocksToGet = new List<string>() { "AAPL", "TSLA", "MMM", "CVX", "KO"};

            foreach (var stock in stocksToGet)
            {
                await GetFinnhubQuoteAsync(stock);
            }


            //await Task.Delay(10000, stoppingToken); // wait 10 second before restarting the loop
            //}
        }

        public async Task GetFinnhubQuoteAsync(string symbol)
        {
            string apiKey = "d2bttn9r01qvh3vdaei0d2bttn9r01qvh3vdaeig";
            string url = $"https://finnhub.io/api/v1/quote?symbol={symbol}&token={apiKey}";

            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(url);

            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();

            var quote = JsonSerializer.Deserialize<Quote>(json);
            //TODO: Save results to CosmosDB
            _logger.LogInformation($"Current price for {symbol}: {quote.CurrentPrice}");
        }
    }
}
