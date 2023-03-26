namespace QRWells.WellNet.Core.Http;

public sealed class HttpResponse : HttpObject
{
    public HttpVersion Version { get; set; }
    public int StatusCode { get; set; }
}