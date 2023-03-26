namespace QRWells.WellNet.Core.Http;

public enum HttpMethod
{
    Get,
    Post,
    Put,
    Delete,
    Head,
    Options,
    Trace,
    Connect,
    Patch
}

public static class HttpMethodExtensions
{
    public static string ToHttpString(this HttpMethod method)
    {
        return method switch
        {
            HttpMethod.Get => "GET",
            HttpMethod.Post => "POST",
            HttpMethod.Put => "PUT",
            HttpMethod.Delete => "DELETE",
            HttpMethod.Head => "HEAD",
            HttpMethod.Options => "OPTIONS",
            HttpMethod.Trace => "TRACE",
            HttpMethod.Connect => "CONNECT",
            HttpMethod.Patch => "PATCH",
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };
    }

    public static HttpMethod ParseHttpMethod(this ReadOnlySpan<char> method)
    {
        return method switch
        {
            "GET" => HttpMethod.Get,
            "POST" => HttpMethod.Post,
            "PUT" => HttpMethod.Put,
            "DELETE" => HttpMethod.Delete,
            "HEAD" => HttpMethod.Head,
            "OPTIONS" => HttpMethod.Options,
            "TRACE" => HttpMethod.Trace,
            "CONNECT" => HttpMethod.Connect,
            "PATCH" => HttpMethod.Patch,
            _ => throw new ArgumentOutOfRangeException(nameof(method), "Invalid HTTP method.")
        };
    }
}