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
    [TestMethod]
    public void TestSimpleStuff()
    {

    }
}
