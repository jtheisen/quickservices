namespace QuickRemoting;

public class RequestingRemotingConnection : IRemotingConnection
{
    ILogger<RequestingRemotingConnection>? logger;

    private readonly string url;
    private readonly Func<HttpClient> clientFactory;
    private readonly int maxAttempts;

    public RequestingRemotingConnection(String url, Func<HttpClient> clientFactory, int maxAttempts = 1)
    {
        this.url = url;
        this.clientFactory = clientFactory;
        this.maxAttempts = maxAttempts;
    }

    public async Task<ResponseEnvelope> Call(RequestEnvelope args)
    {
        var content = new StringContent(args.Arguments.AssertNotNull("Arguments shouldn't be null"), Encoding.UTF8, "application/json");

        content.Headers.Add(Constants.InterfaceHeaderName, args.InterfaceName);
        content.Headers.Add(Constants.MethodHeaderName, args.MethodName);

        var client = clientFactory();

        Exception? exception = null;

        HttpResponseMessage? response = null;
        String? responseBody = null;
        for (int i = 0; i < maxAttempts; i++)
        {
            try
            {
                response = await client.PostAsync(url, content);
                responseBody = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    return new ResponseEnvelope
                    {
                        Result = responseBody
                    };
                }
                else if ((Int32)response.StatusCode == 418)
                {
                    return new ResponseEnvelope
                    {
                        Exception = new QuickServiceServerException(responseBody),
                        Result = responseBody
                    };
                }
                else
                {
                    logger?.LogWarning($"Retry call {i}/{maxAttempts} {(int)response.StatusCode} {response.ReasonPhrase} {responseBody}");
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                }
            }
            catch (Exception ex)
            {
                exception = ex;
                logger?.LogWarning(ex, $"Exception during attempt {i}");
                await Task.Delay(TimeSpan.FromMilliseconds(500));
            }
        }
        var message = $"Gave up after {maxAttempts} attempts.";
        logger?.LogError(message);
        return new ResponseEnvelope
        {
            Exception = exception,
            Result = response?.ReasonPhrase ?? responseBody ?? message
        };
    }
}
