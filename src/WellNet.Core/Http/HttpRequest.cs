namespace QRWells.WellNet.Core.Http;

public sealed class HttpRequest : HttpObject
{
    public HttpMethod Method { get; set; }
    public string Path { get; set; }
    public HttpVersion Version { get; set; }
}