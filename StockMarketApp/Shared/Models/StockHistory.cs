using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Models
{
    public class StockHistory
    {
        public string id { get; set; }
        public bool IsActivelyTracked { get; set; } = true;
        public DateTime CreatedOn { get; set; }
        public DateTime LastUpdated { get; set; }
        public List<Quote> QuoteHistory { get; set; }


        public StockHistory(string id, Quote firstQuote)
        {
            this.id = id;
            this.CreatedOn = DateTime.Today;
            this.LastUpdated = DateTime.Today;
            this.QuoteHistory = new List<Quote>() { firstQuote };
        }
        public StockHistory(string id)
        {
            this.id = id;
            this.CreatedOn = DateTime.Today;
            this.QuoteHistory = new List<Quote>();
        }

        public StockHistory()
        {
            // Needed for JSON deserialization
        }
    }
}
