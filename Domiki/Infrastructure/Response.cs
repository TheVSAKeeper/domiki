namespace Domiki.Web.Infrastructure;

public class Response<T> : Response
{
    public Response(T content)
    {
        Content = content;
    }

    public T Content { get; set; }
}

public class Response
{
    public Response()
    {
        Type = ResponseType.Success;
    }

    public ResponseType Type { get; set; }
}

public enum ResponseType
{
    Success = 1,
    ErrorMessage = 2,
}
