public interface ICurrencyProvider
{
    Task<decimal> GetExchangeRateAsync(string baseCurrency, string targetCurrency);
    Task<Dictionary<string, decimal>> GetLatestExchangeRatesAsync(string baseCurrency);
    Task<Dictionary<string, Dictionary<string, decimal>>> GetHistoricalExchangeRatesAsync(string baseCurrency, DateTime startDate, DateTime endDate);  // This is the missing method
}
