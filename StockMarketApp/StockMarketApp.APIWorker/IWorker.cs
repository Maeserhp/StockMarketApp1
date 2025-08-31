using Shared.Models;

namespace StockMarketApp.APIWorker
{
    public interface IWorker
    {
        Task<List<string>> GetSymbolsList(bool getActiveOnly = true, CancellationToken cancellationToken = default);
        Task<Quote> GetFinnhubQuoteAsync(string symbol);
        Task<StockSearchResponse> FinnhubLookupSymbol(string query);
        Task<string> AddStockSymbolToCosmos(string symbol, CancellationToken cancellationToken = default);
        Task<int> PerformDailyQueries(CancellationToken cancellationToken = default);
        Task<StockHistory?> GetSelectedStockHistory(string symbol, CancellationToken cancellationToken = default);
        Task MarkStockUntracked(string symbol, CancellationToken cancellationToken = default);
        //Task<List<StockHistory>> GetExistingStockHistories(bool getActiveOnly = true, CancellationToken cancellationToken = new CancellationToken());
        //Task UpsertQuoteHistory(string symbol, Quote quote, List<StockHistory> existingStockHistories);
    }
}
