using CurrencyConverterApi.Services;
using CurrencyConverterApi.Providers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CurrencyConverterApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CurrencyConverterController : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly ICurrencyProviderFactory _currencyProviderFactory;

        public CurrencyConverterController(IAuthenticationService authenticationService, ICurrencyProviderFactory currencyProviderFactory)
        {
            _authenticationService = authenticationService;
            _currencyProviderFactory = currencyProviderFactory;
        }

        // Define a list of currencies to exclude
        private static readonly string[] ExcludedCurrencies = { "TRY", "PLN", "THB", "MXN" };

        // Login Endpoint to authenticate the user and return a JWT Token with roles
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            // Simplified authentication logic (replace with actual authentication logic, e.g., check from a database)
            if (request.Username == "admin" && request.Password == "adminpassword")  // Example admin credentials
            {
                var token = _authenticationService.GenerateJwtToken(request.Username, "Admin");
                return Ok(new { Token = token });
            }
            else if (request.Username == "user" && request.Password == "userpassword")  // Example user credentials
            {
                var token = _authenticationService.GenerateJwtToken(request.Username, "User");
                return Ok(new { Token = token });
            }

            return Unauthorized("Invalid credentials.");
        }

        // Endpoint to get the latest exchange rates - secured with JWT and restricted to 'Admin' role
        [HttpGet("latest")]
        [Authorize(Roles = "Admin")]  // Enforce RBAC by allowing only Admin role
        public async Task<IActionResult> GetLatestExchangeRates(string baseCurrency, string provider = "Frankfurter")
        {
            if (string.IsNullOrEmpty(baseCurrency))
            {
                return BadRequest("Base currency is required.");
            }

            // Check if the base currency is in the excluded list
            if (ExcludedCurrencies.Contains(baseCurrency.ToUpper()))
            {
                return BadRequest($"The currency {baseCurrency} is not allowed.");
            }

            try
            {
                // Dynamically select the currency provider based on the 'provider' parameter
                var currencyProvider = _currencyProviderFactory.CreateProvider(provider);
                var rates = await currencyProvider.GetLatestExchangeRatesAsync(baseCurrency);
                return Ok(rates);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Endpoint to convert currency - secured with JWT and restricted to 'User' role
        [HttpPost("convert")]
        [Authorize(Roles = "User,Admin")]  // Enforce RBAC by allowing both User and Admin roles
        public async Task<IActionResult> ConvertCurrency([FromBody] ConversionRequest request)
        {
            if (ExcludedCurrencies.Contains(request.BaseCurrency.ToUpper()) || ExcludedCurrencies.Contains(request.TargetCurrency.ToUpper()))
            {
                return BadRequest($"The currency {request.BaseCurrency} or {request.TargetCurrency} is not allowed.");
            }

            try
            {
                var currencyProvider = _currencyProviderFactory.CreateProvider("Frankfurter");
                var exchangeRate = await currencyProvider.GetExchangeRateAsync(request.BaseCurrency, request.TargetCurrency);
                var convertedAmount = request.Amount * exchangeRate;
                return Ok(convertedAmount);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Endpoint to get historical exchange rates - secured with JWT and restricted to 'Admin' role
        [HttpGet("historical")]
        [Authorize(Roles = "Admin")]  // Enforce RBAC by allowing only Admin role
        public async Task<IActionResult> GetHistoricalExchangeRates(string baseCurrency, DateTime startDate, DateTime endDate, int page = 1, int pageSize = 10)
        {
            if (startDate > endDate)
            {
                return BadRequest("Start date cannot be later than end date.");
            }

            if (ExcludedCurrencies.Contains(baseCurrency.ToUpper()))
            {
                return BadRequest($"The currency {baseCurrency} is not allowed.");
            }

            try
            {
                var allRates = await _currencyProviderFactory.CreateProvider("Frankfurter").GetHistoricalExchangeRatesAsync(baseCurrency, startDate, endDate);

                // Apply pagination to the results
                var paginatedRates = allRates
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToDictionary(entry => entry.Key, entry => entry.Value);

                return Ok(new
                {
                    TotalCount = allRates.Count,
                    Page = page,
                    PageSize = pageSize,
                    Data = paginatedRates
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }

    // DTO for login request (contains username and password)
    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    // DTO for currency conversion request
    public class ConversionRequest
    {
        public string BaseCurrency { get; set; }
        public string TargetCurrency { get; set; }
        public decimal Amount { get; set; }
    }
}
