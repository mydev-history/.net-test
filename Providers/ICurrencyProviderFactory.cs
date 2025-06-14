namespace CurrencyConverterApi.Providers
{
    public interface ICurrencyProviderFactory
    {
        ICurrencyProvider CreateProvider(string providerName);
    }
}
