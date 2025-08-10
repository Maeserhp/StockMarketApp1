using System.Text.Json.Serialization;

namespace Shared.Models
{
    public class Quote
    {
        [JsonPropertyName("c")]
        public decimal CurrentPrice { get; set; }

        [JsonPropertyName("h")]
        public decimal HighPrice { get; set; }

        [JsonPropertyName("l")]
        public decimal LowPrice { get; set; }

        [JsonPropertyName("o")]
        public decimal OpenPrice { get; set; }

        [JsonPropertyName("pc")]
        public decimal PreviousClosePrice { get; set; }
    }
}
