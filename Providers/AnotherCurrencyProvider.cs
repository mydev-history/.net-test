namespace CurrencyConverterApi.Providers
{
    public class AnotherCurrencyProvider : ICurrencyProvider
    {
        // Implement the methods for this provider...
        public async Task<decimal> GetExchangeRateAsync(string baseCurrency, string targetCurrency)
        {
            // Example implementation for another provider
            return 1.2m; // Return a dummy exchange rate for simplicity
        }

        public async Task<Dictionary<string, decimal>> GetLatestExchangeRatesAsync(string baseCurrency)
        {
            // Return dummy data for the latest rates
            return new Dictionary<string, decimal>
            {
                { "EUR", 1.1m },
                { "GBP", 0.9m }
            };
        }

        public async Task<Dictionary<string, Dictionary<string, decimal>>> GetHistoricalExchangeRatesAsync(string baseCurrency, DateTime startDate, DateTime endDate)
        {
            // Return dummy data for historical rates
            return new Dictionary<string, Dictionary<string, decimal>>
            {
                { "2025-06-01", new Dictionary<string, decimal> { { "EUR", 1.1m }, { "GBP", 0.9m } } },
                { "2025-06-02", new Dictionary<string, decimal> { { "EUR", 1.2m }, { "GBP", 1.0m } } }
            };
        }
    }
}
