using System.Net.Http;

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
        var service = RemotingProxyFactory.Create<TService>(connection);
        return service;
    }
}
