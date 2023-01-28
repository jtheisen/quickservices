using Microsoft.Extensions.DependencyInjection;
using QuickRemoting;

namespace TestSuite;

public interface IFooService
{
    Task SetValue(String value);

    Task<String> GetValue();

    Task ResetValue();
}

#pragma warning disable CS1998
public class FooServiceImplementation : IFooService
{
    String value = "";

    public async Task<String> GetValue() => value;

    public async Task ResetValue() => value = "";

    public async Task SetValue(string value) => this.value = value;
}
#pragma warning restore

[TestClass]
public class BasicTests
{
    Service GetService<Service, Implementation>()
        where Implementation : class, Service
        where Service : class
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<Service, Implementation>();
        var services = serviceCollection.BuildServiceProvider();

        return RemotingProxyFactory.Create<Service>(new ExecutingRemotingConnection(services, typeof(IFooService).Assembly));
    }

    [TestMethod]
    public async Task TestSimpleStuff()
    {
        var foo = GetService<IFooService, FooServiceImplementation>();

        Assert.AreEqual("", await foo.GetValue());

        await foo.SetValue("secret");

        Assert.AreEqual("secret", await foo.GetValue());

        await foo.ResetValue();

        Assert.AreEqual("", await foo.GetValue());
    }
}
