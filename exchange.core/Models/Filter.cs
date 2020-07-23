using System.Text.Json.Serialization;

namespace exchange.core.Models
{
    public class Filter
    {
        [JsonPropertyName("filterType")]
        public string FilterType { get; set; }
        [JsonPropertyName("minPrice")]
        public string MinPrice { get; set; }
        [JsonPropertyName("maxPrice")]
        public string MaxPrice { get; set; }
        [JsonPropertyName("tickSize")]
        public string TickSize { get; set; }
        [JsonPropertyName("multiplierUp")]
        public string MultiplierUp { get; set; }
        [JsonPropertyName("multiplierDown")]
        public string MultiplierDown { get; set; }
        [JsonPropertyName("avgPriceMins")]
        public int AveragePriceMin { get; set; }
    }
}
