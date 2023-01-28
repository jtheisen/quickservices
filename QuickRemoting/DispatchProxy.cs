using Newtonsoft.Json;

namespace QuickRemoting;

class QuickServiceDispatchProxy : DispatchProxy
{
    internal IRemotingConnection Connection { get; set; } = null!;
    internal JsonSerializerSettings Settings { get; set; } = null!;

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod is null) throw new Exception("No target method to proxy");
        if (targetMethod.DeclaringType is null) throw new Exception("Target method has no declaring type");

        var request = new RequestEnvelope
        {
            Arguments = JsonConvert.SerializeObject(args, Formatting.Indented, Settings),
            InterfaceName = targetMethod.DeclaringType!.FullName!,
            MethodName = targetMethod.Name
        };

        var returnTaskType = targetMethod.ReturnType;

        if (!typeof(Task).IsAssignableFrom(returnTaskType))
        {
            throw new Exception($"Method {request.InterfaceName}.{request.MethodName} returns nothing task-based and cannot be forwarded.");
        }

        var hasVoidReturnType = returnTaskType == typeof(Task);

        var returnType = hasVoidReturnType
            ? typeof(string) // we could use anything nullable here
            : returnTaskType.GenericTypeArguments[0];

        var tcs = CustomTaskCompletionSource.Create(returnType);

        var callTask = Connection.Call(request);

        callTask.ContinueWith(t =>
        {
            if (t.Exception != null)
            {
                tcs.TrySetException(t.Exception);
            }
            else
            {
                var response = callTask.Result;

                if (response.Exception is not null)
                {
                    tcs.TrySetException(response.Exception);
                }
                else if (hasVoidReturnType)
                {
                    tcs.TrySetResult(null!);
                }
                else
                {
                    try
                    {
                        var result = JsonConvert.DeserializeObject(response.Result!, returnType, Settings);

                        tcs.TrySetResult(result!);
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                }
            }
        });

        return tcs.GetTask();
    }
}

public class DispatchProxyFactory
{
    public static readonly JsonSerializerSettings settings = new() { TypeNameHandling = TypeNameHandling.Auto };

    public static TService Create<TService>(IRemotingConnection connection) where TService : class
    {
        InterfaceValidator<TService>.Validate();

        var service = DispatchProxy.Create<TService, QuickServiceDispatchProxy>();

        var proxy = service as QuickServiceDispatchProxy;

        if (proxy is null) throw new Exception($"Can't cast proxy to it's expected implementation type");

        proxy.Settings = settings;
        proxy.Connection = connection;

        return service;
    }
}
