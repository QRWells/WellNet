namespace QRWells.WellNet.Core.Http;

public enum HttpVersion
{
    Http10,
    Http11,
    Http2,
    Http3
}

public static class HttpVersionExtensions
{
    public static string ToHttpString(this HttpVersion version)
    {
        return version switch
        {
            HttpVersion.Http10 => "HTTP/1.0",
            HttpVersion.Http11 => "HTTP/1.1",
            HttpVersion.Http2 => "HTTP/2",
            HttpVersion.Http3 => "HTTP/3",
            _ => throw new ArgumentOutOfRangeException(nameof(version), version, null)
        };
    }

    public static HttpVersion ParseHttpVersion(this ReadOnlySpan<char> version)
    {
        return version switch
        {
            "HTTP/1.0" => HttpVersion.Http10,
            "HTTP/1.1" => HttpVersion.Http11,
            "HTTP/2" => HttpVersion.Http2,
            "HTTP/3" => HttpVersion.Http3,
            _ => throw new ArgumentOutOfRangeException(nameof(version), "Invalid HTTP version.")
        };
    }
}