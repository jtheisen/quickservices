using Microsoft.Extensions.DependencyInjection;
using QuickRemoting;

namespace TestSuite;

public interface IFooService
{
    Task SetValue(String value);

    Task<String> GetValue();

    Task ResetValue();

    Task ThrowException();
}

#pragma warning disable CS1998
public class FooServiceImplementation : IFooService
{
    String value = "";

    public async Task<String> GetValue() => value;

    public async Task ResetValue() => value = "";

    public async Task SetValue(string value) => this.value = value;

    public Task ThrowException() => throw new NotImplementedException();
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

        return DispatchProxyFactory.Create<Service>(new ExecutingRemotingConnection(services, typeof(IFooService).Assembly));
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

    [TestMethod]
    public async Task TestException()
    {
        var foo = GetService<IFooService, FooServiceImplementation>();

        try
        {
            await foo.ThrowException();

            Assert.Fail();
        }
        catch (Exception ex)
        {
            Assert.IsTrue(ex is NotImplementedException or QuickServiceServerException);
        }
    }
}
