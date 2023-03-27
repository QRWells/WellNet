using System.Net;
using System.Text;

namespace QRWells.WellNet.Core.Http;

public sealed class HttpResponse : HttpObject
{
    public HttpVersion Version { get; set; }
    public HttpStatusCode StatusCode { get; set; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Version: {Version.ToHttpString()}");
        sb.AppendLine($"StatusCode: {StatusCode}");
        sb.Append(Headers);
        sb.AppendLine("Body:");
        sb.AppendLine(Encoding.UTF8.GetString(Body));

        return sb.ToString();
    }
}