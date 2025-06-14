namespace CurrencyConverterApi.Providers
{
    public class CurrencyProviderFactory : ICurrencyProviderFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public CurrencyProviderFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ICurrencyProvider CreateProvider(string providerName)
        {
            switch (providerName.ToLower())
            {
                case "frankfurter":
                    return _serviceProvider.GetRequiredService<FrankfurterCurrencyProvider>();
                case "another":
                    return _serviceProvider.GetRequiredService<AnotherCurrencyProvider>();
                default:
                    throw new ArgumentException("Invalid provider name", nameof(providerName));
            }
        }
    }
}
