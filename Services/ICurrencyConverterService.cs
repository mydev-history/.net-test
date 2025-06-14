namespace CurrencyConverterApi.Services
{
    public interface ICurrencyConverterService
    {
        Task<decimal> ConvertAsync(string baseCurrency, string targetCurrency, decimal amount);
        Task<Dictionary<string, decimal>> GetLatestRatesAsync(string baseCurrency);
        Task<Dictionary<string, Dictionary<string, decimal>>> GetHistoricalExchangeRatesAsync(string baseCurrency, DateTime startDate, DateTime endDate);
    }
}
