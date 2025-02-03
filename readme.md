# Quickservices

Quickservices are an extremely simple way of doing remote procedure calls between services in .NET. Is shines especially when

- Both client and service are written in .NET.
- Both are, even though deployed separately, built from the same sources (compile-time safety of the calls during build).
- Context flexibility is required (pass through of ambient context such as security principals or the current culture)

The entire code to make this work is about a screenful and can thereby just as well simply be copy-pasted into your project.

You then define a service with:

```c#
// lives in a common assembly shared between service and client
public interface IFooService
{
    Task SetValue(String value);

    Task<String> GetValue();

    Task ResetValue();

    Task ThrowException();
}
```

And implement the service:

```c#
// lives in the assembly of the service
public class FooServiceImplementation : IFooService
{
    String value = "";

    public async Task<String> GetValue() => value;

    public async Task ResetValue() => value = "";

    public async Task SetValue(string value) => this.value = value;

    public Task ThrowException() => throw new NotImplementedException();
}
```

The shared assembly ensures that the client can type-safely call the service:

```c#
await fooService.SetValue("secret");

Assert.AreEqual("secret", await fooService.GetValue());
```

The service executable offers the service like this:

```c#
// Program.cs of the service
// ...
services.AddTransient<IFooService, FooServiceImplementation>();
// ...
app.UseQuickRemotingExecution(typeof(IFooService).Assembly);
// ...
```

And the client can offer it for availability with DI like this:

```c#
services.AddQuickRemotingService<IWeatherForecastService>();
```

The repo contains a test suite and a test application.

I've used this extensively in a microservice architecture and the test app in this project sets this up for a Blazor Webassembly client against an ASP.NET server.
