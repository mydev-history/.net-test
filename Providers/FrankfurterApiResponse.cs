namespace CurrencyConverterApi.Providers
{
    public class FrankfurterApiResponse
    {
        public decimal? amount { get; set; }  // "amount": 1.0
        public string? Base { get; set; }     // "base": "USD"
        public string? date { get; set; }     // "date": "2025-06-13"
        public Dictionary<string, decimal>? rates { get; set; }  // "rates": { "AUD": 1.5442, ... }
    }
}
