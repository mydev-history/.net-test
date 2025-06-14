using CurrencyConverterApi.Providers;

namespace CurrencyConverterApi.Services
{
    public class CurrencyConverterService : ICurrencyConverterService
    {
        private readonly ICurrencyProvider _currencyProvider;

        public CurrencyConverterService(ICurrencyProvider currencyProvider)
        {
            _currencyProvider = currencyProvider;
        }

        public async Task<decimal> ConvertAsync(string baseCurrency, string targetCurrency, decimal amount)
        {
            var exchangeRate = await _currencyProvider.GetExchangeRateAsync(baseCurrency, targetCurrency);
            return amount * exchangeRate;
        }

        public async Task<Dictionary<string, decimal>> GetLatestRatesAsync(string baseCurrency)
        {
            return await _currencyProvider.GetLatestExchangeRatesAsync(baseCurrency);
        }

        public async Task<Dictionary<string, Dictionary<string, decimal>>> GetHistoricalExchangeRatesAsync(string baseCurrency, DateTime startDate, DateTime endDate)
        {
            return await _currencyProvider.GetHistoricalExchangeRatesAsync(baseCurrency, startDate, endDate);
        }
    }
}
