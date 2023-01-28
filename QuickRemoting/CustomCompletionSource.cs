namespace QuickRemoting;

internal interface ICustomTaskCompletionSource
{
    Task GetTask();

    bool TrySetCanceled();
    bool TrySetCanceled(CancellationToken cancellationToken);
    bool TrySetException(Exception exception);
    bool TrySetException(IEnumerable<Exception> exceptions);
    bool TrySetResult(object result);

}

internal static class CustomTaskCompletionSource
{
    public static ICustomTaskCompletionSource Create(Type type)
    {
        var ctcsType = typeof(CustomTaskCompletionSource<>).MakeGenericType(type);

        return (ICustomTaskCompletionSource?)Activator.CreateInstance(ctcsType)
            ?? throw new Exception($"Activator gave no instance");
    }
}

internal class CustomTaskCompletionSource<TResult> : TaskCompletionSource<TResult>, ICustomTaskCompletionSource
{
    Task ICustomTaskCompletionSource.GetTask() => Task;
    bool ICustomTaskCompletionSource.TrySetCanceled() => TrySetCanceled();
    bool ICustomTaskCompletionSource.TrySetCanceled(CancellationToken cancellationToken) => TrySetCanceled();
    bool ICustomTaskCompletionSource.TrySetException(Exception exception) => TrySetException(exception);
    bool ICustomTaskCompletionSource.TrySetException(IEnumerable<Exception> exceptions) => TrySetException(exceptions);
    bool ICustomTaskCompletionSource.TrySetResult(object result) => TrySetResult((TResult)result);
}
