using System.Text;

namespace QRWells.WellNet.Core.Http;

public sealed class HttpRequest : HttpObject
{
    public HttpMethod Method { get; set; }
    public string Path { get; set; }
    public HttpVersion Version { get; set; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Method: {Method.ToHttpString()}");
        sb.AppendLine($"Path: {Path}");
        sb.AppendLine($"Version: {Version.ToHttpString()}");
        sb.Append(Headers);
        sb.AppendLine("Body:");
        sb.AppendLine(Encoding.UTF8.GetString(Body));

        return sb.ToString();
    }
}