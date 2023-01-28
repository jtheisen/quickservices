using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QuickRemoting;

public class ExecutingRemotingConnection : IRemotingConnection
{
    ILogger<ExecutingRemotingConnection>? logger;

    private readonly IServiceProvider services;
    private readonly Assembly assembly;
    private readonly JsonSerializerSettings settings;
    private readonly JsonSerializer serializer;

    public ExecutingRemotingConnection(IServiceProvider services, Assembly assembly, JsonSerializerSettings? settings = null)
    {
        this.services = services;
        this.assembly = assembly;
        this.settings = settings ?? RemotingProxyFactory.settings;
        serializer = JsonSerializer.Create(this.settings);
    }

    public async Task<ResponseEnvelope> Call(RequestEnvelope request)
    {
        if (request.InterfaceName is null) throw new Exception($"Request came with an unset interface name");
        if (request.MethodName is null) throw new Exception($"Request came with an unset method name");
        if (request.Arguments is null) throw new Exception($"Request came with an unset arguments");

        var message = $"{request.InterfaceName}.{request.MethodName}";

        logger?.LogDebug(message);

        var type = assembly.GetType(request.InterfaceName);

        if (type == null)
        {
            throw new Exception($"Service interface {request.InterfaceName} unknown");
        }

        var service = services.GetService(type);

        if (service == null)
        {
            throw new Exception($"No implementation found for service interface {request.InterfaceName}");
        }

        var method = type.GetMethod(request.MethodName);

        if (method is null) throw new Exception($"Can't find method {request.MethodName} on type {type}");

        try
        {
            var argumentsAsTokens = JsonConvert.DeserializeObject<JToken[]>(request.Arguments);

            if (argumentsAsTokens is null) throw new Exception($"Passed arguments were null");

            var arguments = ConvertArguments(method, argumentsAsTokens);

            dynamic untypedResult = method.Invoke(service, arguments.ToArray())
                ?? throw new Exception("Result of a method invokation yielded no return value");

            await untypedResult;

            var jtokenResult = method.ReturnType == typeof(Task) ? null : untypedResult.Result != null ? JToken.FromObject(untypedResult.Result) : null;

            logger?.LogInformation(message);

            return new ResponseEnvelope { Result = JsonConvert.SerializeObject(jtokenResult, settings) };
        }
        catch (Exception ex)
        {
            logger?.LogInformation(ex, message);

            return new ResponseEnvelope { IsError = true, Result = ex.Message };
        }
    }

    private Object?[] ConvertArguments(MethodInfo method, JToken[] argumentsAsTokens)
    {
        var arguments = new List<Object?>();
        var parameters = method.GetParameters();
        for (int i = 0; i < parameters.Length; ++i)
        {
            var token = argumentsAsTokens[i];
            var parameterType = parameters[i].ParameterType;
            var argument = token.ToObject(parameterType, serializer);
            arguments.Add(argument);
        }
        return arguments.ToArray();
    }
}
