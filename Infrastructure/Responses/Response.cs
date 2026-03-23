using System.Net;

namespace SomoniBank.Infrastructure.Responses;

public class Response<T>
{
    public int StatusCode { get; set; } = 0;
    public List<string> Description { get; set; } = [];
    public T? Data { get; set; }

    public Response(HttpStatusCode statusCode, string message, T data)
    {
        StatusCode = (int)statusCode;
        Description.Add(message);
        Data = data;
    }

    public Response(HttpStatusCode statusCode, List<string> message, T data)
    {
        StatusCode = (int)statusCode;
        Description = message;
        Data = data;
    }

    public Response(HttpStatusCode statusCode, string message)
    {
        StatusCode = (int)statusCode;
        Description.Add(message);
    }

    public Response(HttpStatusCode statusCode, List<string> message)
    {
        StatusCode = (int)statusCode;
        Description = message;
    }
}