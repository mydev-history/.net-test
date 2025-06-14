using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;

namespace CurrencyConverterApi.Providers
{
    public class FrankfurterCurrencyProvider : ICurrencyProvider
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<FrankfurterCurrencyProvider> _logger;

        public FrankfurterCurrencyProvider(HttpClient httpClient, IMemoryCache memoryCache, ILogger<FrankfurterCurrencyProvider> logger)
        {
            _httpClient = httpClient;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public async Task<decimal> GetExchangeRateAsync(string baseCurrency, string targetCurrency)
        {
            var correlationId = Guid.NewGuid().ToString();
            var cacheKey = $"{baseCurrency}_{targetCurrency}_rate";

            if (!_memoryCache.TryGetValue(cacheKey, out decimal exchangeRate))
            {
                try
                {
                    _logger.LogInformation($"[CorrelationId: {correlationId}] Fetching exchange rate from Frankfurter API for {baseCurrency} to {targetCurrency}");

                    var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"https://api.frankfurter.app/latest?base={baseCurrency}&symbols={targetCurrency}");
                    requestMessage.Headers.Add("X-Correlation-ID", correlationId);

                    var response = await _httpClient.SendAsync(requestMessage);

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var data = JsonSerializer.Deserialize<FrankfurterApiResponse>(content);

                        exchangeRate = data?.rates?.GetValueOrDefault(targetCurrency) ?? 0;

                        _memoryCache.Set(cacheKey, exchangeRate, TimeSpan.FromHours(1));

                        _logger.LogInformation($"[CorrelationId: {correlationId}] Successfully fetched exchange rate from Frankfurter API: {exchangeRate}");
                    }
                    else
                    {
                        _logger.LogError($"[CorrelationId: {correlationId}] Failed to fetch exchange rate from Frankfurter API. Status Code: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[CorrelationId: {correlationId}] Error while fetching exchange rate from Frankfurter API: {ex.Message}");
                }
            }

            return exchangeRate;
        }

        public async Task<Dictionary<string, decimal>> GetLatestExchangeRatesAsync(string baseCurrency)
        {
            var correlationId = Guid.NewGuid().ToString();
            var cacheKey = $"{baseCurrency}_latest_rates";

            if (!_memoryCache.TryGetValue(cacheKey, out Dictionary<string, decimal> rates))
            {
                try
                {
                    _logger.LogInformation($"[CorrelationId: {correlationId}] Fetching latest exchange rates from Frankfurter API for base currency: {baseCurrency}");

                    var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"https://api.frankfurter.app/latest?base={baseCurrency}");
                    requestMessage.Headers.Add("X-Correlation-ID", correlationId);

                    var response = await _httpClient.SendAsync(requestMessage);

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var data = JsonSerializer.Deserialize<FrankfurterApiResponse>(content);

                        rates = data?.rates ?? new Dictionary<string, decimal>();

                        _memoryCache.Set(cacheKey, rates, TimeSpan.FromHours(1));

                        _logger.LogInformation($"[CorrelationId: {correlationId}] Successfully fetched latest exchange rates from Frankfurter API");
                    }
                    else
                    {
                        _logger.LogError($"[CorrelationId: {correlationId}] Failed to fetch latest exchange rates from Frankfurter API. Status Code: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[CorrelationId: {correlationId}] Error while fetching latest exchange rates from Frankfurter API: {ex.Message}");
                }
            }

            return rates;
        }

        // Implement the missing GetHistoricalExchangeRatesAsync method here
        public async Task<Dictionary<string, Dictionary<string, decimal>>> GetHistoricalExchangeRatesAsync(string baseCurrency, DateTime startDate, DateTime endDate)
        {
            var correlationId = Guid.NewGuid().ToString();
            var allRates = new Dictionary<string, Dictionary<string, decimal>>();

            try
            {
                _logger.LogInformation($"[CorrelationId: {correlationId}] Fetching historical exchange rates from Frankfurter API for {baseCurrency} from {startDate.ToShortDateString()} to {endDate.ToShortDateString()}");

                // Iterate through each day in the date range
                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"https://api.frankfurter.app/{date:yyyy-MM-dd}?base={baseCurrency}");
                    requestMessage.Headers.Add("X-Correlation-ID", correlationId);

                    var response = await _httpClient.SendAsync(requestMessage);

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var data = JsonSerializer.Deserialize<FrankfurterApiResponse>(content);

                        // Store the rates for the specific date
                        allRates.Add(date.ToString("yyyy-MM-dd"), data?.rates ?? new Dictionary<string, decimal>());
                    }
                    else
                    {
                        _logger.LogError($"[CorrelationId: {correlationId}] Failed to fetch historical exchange rates for {date:yyyy-MM-dd}. Status Code: {response.StatusCode}");
                    }
                }

                _logger.LogInformation($"[CorrelationId: {correlationId}] Successfully fetched historical exchange rates from Frankfurter API");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[CorrelationId: {correlationId}] Error while fetching historical exchange rates from Frankfurter API: {ex.Message}");
            }

            return allRates;
        }
    }
}
