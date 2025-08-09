namespace StockMarketApp.Scraper.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                    _logger.LogInformation("My custom log!!!!!!!!!!!!!!!!");
                }
                await Task.Delay(10000, stoppingToken); // wait 10 second
            }
        }
    }
}
