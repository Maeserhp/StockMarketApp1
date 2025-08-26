using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using Shared.Models;


namespace StockMarketApp.APIWorker.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class StocksController : ControllerBase
    {
        private readonly ILogger<StocksController> _logger;
        private readonly Worker _worker;

        public StocksController(ILogger<StocksController> logger, Worker worker)
        {
            _logger = logger;
            _worker = worker;
        }
         

        // GET api/<StocksController>/
        [HttpGet("todays-quote/{symbol}")]
        public async Task<ActionResult<Quote>> GetQuote(string symbol)
        {
            // Basic validation
            if (string.IsNullOrEmpty(symbol))
            {
                return BadRequest("Symbol cannot be empty."); // HTTP 400
            }

            try
            {
                var result = await _worker.GetFinnhubQuoteAsync(symbol);
                if (result == null || result.CurrentPrice <= 0)
                {
                    return NotFound($"Stock symbol '{symbol}' not found."); // HTTP 404
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log the exception here
                // For example: _logger.LogError(ex, "Error getting stock quote.");
                return StatusCode(500, "An error occurred while retrieving the stock quote."); // HTTP 500
            }
        }

        // GET api/<StocksController>/
        [HttpGet("tracked-stocks")]
        public async Task<ActionResult<List<StockSymbol>>> GetStocks()
        {

            try
            {
                var result = await _worker.GetSymbolsList();
                if (result == null)
                {
                    return NotFound($"No Stocks are currently tracked."); // HTTP 404
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log the exception here
                // For example: _logger.LogError(ex, "Error getting stock quote.");
                return StatusCode(500, "An error occurred while retrieving tracked stocks."); // HTTP 500
            }
        }

        [HttpGet("stock-history/{symbol}")]
        public async Task<ActionResult<StockHistory>> GetStockHistory(string symbol)
        {
            // Basic validation
            if (string.IsNullOrEmpty(symbol))
            {
                return BadRequest("Symbol cannot be empty."); // HTTP 400
            }

            try
            {
                var result = await _worker.GetSelectedStockHistory(symbol);

                if (result == null)
                {
                    return NotFound($"Stock symbol '{symbol}' not found."); // HTTP 404
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving the stock quote."); // HTTP 500
            }
        }

        // PUT api/<StocksController>/5
        [HttpPost("daily-query-update")]
        public async Task<IActionResult> DailyUpdate()
        {
            try
            {
                var stocksUpdatedCount = await _worker.PerformDailyQueries();
                return Ok($"{stocksUpdatedCount} Daily stock queries initiated and completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during the daily stock update.");
                return StatusCode(500, "An error occurred while processing the request.");
            }
        }

        // GET api/<StocksController>/5
        [HttpGet("search-stocks/{query}")]
        public async Task<ActionResult<StockSearchResponse>> SearchStocks(string query)
        {
            try
            {
                var resultOptions = await _worker.FinnhubLookupSymbol(query);

                if (resultOptions == null)
                {
                    return NotFound($"Stock symbol '{resultOptions}' not found."); // HTTP 404
                }

                return Ok(resultOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while adding the new symbol.");
                return StatusCode(500, "An error occurred while adding the new symbol.");
            }
        }


        // PUT api/<StocksController>/5
        [HttpPost("add-new-symbol/{symbol}")]
        public async Task<IActionResult> AddNewSymbol(string symbol)
        {
            try
            {
                var symbolAdded = await _worker.AddStockSymbolToCosmos(symbol);
                return Ok($"The symbol '{symbolAdded}' was successfully added.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while adding the new symbol.");
                return StatusCode(500, "An error occurred while adding the new symbol.");
            }
        }

        // PUT api/<StocksController>/5
        [HttpPost("stop-tracking-stock/{symbol}")]
        public async Task<IActionResult> StopTrackingStock(string symbol)
        {
            try
            {
                await _worker.MarkStockUntracked(symbol);
                return Ok($"The symbol '{symbol}' is no longer being trackedd.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while adding the new symbol.");
                return StatusCode(500, "An error occurred while adding the new symbol.");
            }
        }

    }
}
