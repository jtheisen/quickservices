namespace QuickRemoting;

public class RequestEnvelope
{
    public String? InterfaceName { get; set; }

    public String? MethodName { get; set; }

    public String? Arguments { get; set; }
}

public class ResponseEnvelope
{
    public Boolean IsError { get; set; }

    public String? Result { get; set; }
}

public interface IRemotingConnection
{
    Task<ResponseEnvelope> Call(RequestEnvelope args);
}
