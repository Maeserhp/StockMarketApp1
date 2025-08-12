using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Models
{
    public class StockSymbol
    {
        public string id { get; set; }
        //public string Symbol { get; set; } // This is changed to the Id so that cosmosDB can automatically enforce unique symbols
        public string Currency { get; set; } 
        public string Description { get; set; } 
        public string DisplaySymbol { get; set; } 
        public string Figi { get; set; } 
        public string Mic { get; set; } 
        public string Type { get; set; } 

        public StockSymbol(string id) {
            this.id = id;
            this.Currency = "";
            this.Description = "";
            this.DisplaySymbol = "";
            this.Figi = "";
            this.Mic = "";
            this.Type = "";
        }
    }
}
