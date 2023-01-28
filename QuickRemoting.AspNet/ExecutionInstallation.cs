using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System.Reflection;
using System.Text;

namespace QuickRemoting;

public static class QuickRemotingExtensions
{
    static T ApplyIfNotNull<T>(this T source, Func<T, T>? func)
        => func is not null ? func(source) : source;

    public static IApplicationBuilder UseQuickRemotingExecution(this IApplicationBuilder app, Assembly assembly, Func<IApplicationBuilder, IApplicationBuilder>? pre = null)
    {
        IRemotingConnection makeConnection(HttpContext context, string serviceName)
            => new ExecutingRemotingConnection(context.RequestServices, assembly);

        return app.MapWhen(c => c.Request.Path == "/" + Constants.DefaultPath, app2 => app2
            .ApplyIfNotNull(pre)
            // FIXME
            //.UseWvIdentityForwarding()
            //.UseCultureForwarding()
            .Run(ctx => HandleRequest(ctx, makeConnection)));
    }

    private static async Task HandleRequest(HttpContext context, Func<HttpContext, string, IRemotingConnection> makeConnection)
    {
        var ms = new MemoryStream();
        await context.Request.Body.CopyToAsync(ms);
        var requestBody = Encoding.UTF8.GetString(ms.ToArray());

        var envelope = new RequestEnvelope
        {
            Arguments = requestBody,
            InterfaceName = context.Request.Headers[Constants.InterfaceHeaderName],
            MethodName = context.Request.Headers[Constants.MethodHeaderName]
        };

        var connection = makeConnection(context, envelope.InterfaceName);

        if (string.IsNullOrWhiteSpace(envelope.InterfaceName))
        {
            await Error(context, "No interface specified"); return;
        }
        else if (string.IsNullOrWhiteSpace(envelope.MethodName))
        {
            await Error(context, "No method specified"); return;
        }

        try
        {
            var response = await connection.Call(envelope);

            context.Response.StatusCode = response.Exception is not null ? 418 : 200;

            var result = response.Result ?? response.Exception?.Message ?? "";

            var responseBytes = Encoding.UTF8.GetBytes(result);
            ms = new MemoryStream(responseBytes);

            context.Response.ContentLength = responseBytes.Length;
            await ms.CopyToAsync(context.Response.Body);
        }
        catch (Exception ex)
        {
            // FIXME
            //logger?.LogError(ex, $"Quickremoting couldn't begin to execute {envelope.InterfaceName}.{envelope.MethodName}");

            await Error(context, ex.Message); return;
        }
    }

    private static async Task Error(HttpContext context, string error)
    {
        context.Response.StatusCode = 500;
        context.Response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = error;
        var message = new MemoryStream(Encoding.UTF8.GetBytes(error));
        await message.CopyToAsync(context.Response.Body);
    }
}
