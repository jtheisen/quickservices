using Microsoft.Extensions.DependencyInjection;

namespace QuickRemoting;

public static class QuickRemotingExtensions
{
    public static TService GetQuickService<TService>(string baseUrl, Func<HttpClient> httpClientFactory) where TService : class
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new Exception($"Base url not set for QuickService '{typeof(TService).Name}'");
        }

        var connection = new RequestingRemotingConnection(
            url: baseUrl.TrimEnd('/') + Constants.DefaultPath,
            clientFactory:
            () => httpClientFactory()
                //.WithWvIdentityForwarding()
                //.WithCultureForwarding()
                ,
            maxAttempts: 10
        );
        var service = DispatchProxyFactory.Create<TService>(connection);
        return service;
    }

    public static void AddQuickRemotingService<TService>(this IServiceCollection services, String? path = null) where TService : class
    {
        services.AddScoped(
            typeof(TService),
            sp =>
            {
                var connection = new RequestingRemotingConnection(
                    path ?? Constants.DefaultPath,
                    () => sp.GetRequiredService<HttpClient>()
                //.WithWvIdentityForwarding()
                //.WithCultureForwarding()
                );
                return DispatchProxyFactory.Create<TService>(connection);
            }
        );
    }
}
