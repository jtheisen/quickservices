using Castle.DynamicProxy;
using Newtonsoft.Json;

namespace QuickRemoting;

internal class Interceptor : IInterceptor
{
    private readonly IRemotingConnection connection;
    private readonly JsonSerializerSettings settings;

    public Interceptor(IRemotingConnection connection, JsonSerializerSettings settings)
    {
        this.connection = connection;
        this.settings = settings;
    }

    public void Intercept(IInvocation invocation)
    {
        var request = new RequestEnvelope
        {
            Arguments = JsonConvert.SerializeObject(invocation.Arguments, Formatting.Indented, settings),
            InterfaceName = invocation.Method.DeclaringType!.FullName!,
            MethodName = invocation.Method.Name
        };

        var returnTaskType = invocation.Method.ReturnType;

        if (!typeof(Task).IsAssignableFrom(returnTaskType))
        {
            throw new Exception($"Method {request.InterfaceName}.{request.MethodName} returns nothing task-based and cannot be forwarded.");
        }

        var hasVoidReturnType = returnTaskType == typeof(Task);

        var returnType = hasVoidReturnType
            ? typeof(string) // we could use anything nullable here
            : returnTaskType.GenericTypeArguments[0];

        var tcs = CustomTaskCompletionSource.Create(returnType);

        invocation.ReturnValue = tcs.GetTask();

        var callTask = connection.Call(request);

        callTask.ContinueWith(t =>
        {
            if (t.Exception != null)
            {
                tcs.TrySetException(t.Exception);
            }
            else
            {
                var response = callTask.Result;

                if (response.IsError)
                {
                    tcs.TrySetException(new Exception(response.Result)); // IMPROVEME
                }
                else if (hasVoidReturnType)
                {
                    tcs.TrySetResult(null);
                }
                else
                {
                    try
                    {
                        var result = JsonConvert.DeserializeObject(response.Result, returnType, settings);

                        tcs.TrySetResult(result);
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                }
            }
        });
    }
}

public class RemotingProxyFactory
{
    private static readonly ProxyGenerator generator = new();

    public static readonly JsonSerializerSettings settings = new() { TypeNameHandling = TypeNameHandling.Auto };

    public static TService Create<TService>(IRemotingConnection connection) where TService : class
    {
        InterfaceValidator<TService>.Validate();

        return generator.CreateInterfaceProxyWithoutTarget<TService>(new Interceptor(connection, settings));
    }
}
