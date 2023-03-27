using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Text;
using QRWells.WellNet.Core.Pipeline;
using QRWells.WellNet.Core.Pipeline.Processor.Codec;
using QRWells.WellNet.Core.Utils;
using static System.Buffer;

namespace QRWells.WellNet.Core.Http;

public sealed class HttpMessageDecoder : ByteMessageDecoder<HttpObject>
{
    private static readonly byte[] Crlf = "\r\n"u8.ToArray();
    private static readonly byte[] CrlfCrlf = "\r\n\r\n"u8.ToArray();
    private static readonly Decoder Utf8Decoder = Encoding.UTF8.GetDecoder();
    private FirstLine _firstLine;
    private MultiDictionary _headers = new();
    private bool _isRequest;
    private State _state;

    protected override bool TryDecode(DecoderContext ctx, ref SequenceReader<byte> reader,
        [NotNullWhen(true)] out HttpObject? message)
    {
        switch (_state)
        {
            case State.FirstLine:
                goto firstLine;
            case State.Header:
                goto header;
            case State.Body:
                goto body;
            default:
                throw new ArgumentOutOfRangeException();
        }

        #region First Line

        firstLine:

        if (reader.Remaining < 4)
        {
            message = null;
            return false;
        }

        if (reader.TryPeek(out var h))
            switch (h)
            {
                case (byte)'G': // GET
                case (byte)'P': // POST PUT PATCH
                case (byte)'D': // DELETE
                case (byte)'O': // OPTIONS
                case (byte)'C': // CONNECT
                case (byte)'T': // TRACE
                    _isRequest = true;
                    break;
                case (byte)'H':
                {
                    if (reader.TryPeek(1, out var t))
                    {
                        if (t == (byte)'T')
                            _isRequest = false;
                        else
                            throw new InvalidOperationException();
                    }
                    else
                    {
                        // todo: Set exception
                        message = null;
                        return false;
                    }

                    break;
                }
                default:
                    throw new InvalidOperationException();
            }

        if (!TryParseFirstLine(ref reader, _isRequest, out _firstLine))
        {
            message = null;
            return false;
        }

        _state = State.Header;

        #endregion

        #region Headers

        header:

        if (!TryParseHeader(ref reader, out _headers))
        {
            message = null;
            return false;
        }

        _state = State.Body;

        #endregion

        #region Body

        body:

        byte[] body;
        if (_headers.TryGetValue("Transfer-Encoding", out var values))
        {
            if (values[0] == "chunked")
            {
                var chunks = new List<byte[]>();
                var chunkSize = 0;
                while (true)
                {
                    if (!TryParseChunkSize(ref reader, out chunkSize))
                    {
                        message = null;
                        return false;
                    }

                    if (chunkSize == 0)
                        break;

                    if (reader.Remaining < chunkSize + 2)
                    {
                        message = null;
                        return false;
                    }

                    var chunk = new byte[chunkSize];
                    reader.TryCopyTo(chunk);
                    chunks.Add(chunk);
                    reader.Advance(2);
                }

                body = new byte[chunks.Sum(x => x.Length)];
                var offset = 0;
                foreach (var chunk in chunks)
                {
                    BlockCopy(chunk, 0, body, offset, chunk.Length);
                    offset += chunk.Length;
                }

                reader.Advance(2);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        else if (_headers.TryGetValue("Content-Length", out var value))
        {
            var contentLength = int.Parse(value[0]);
            if (reader.Remaining < contentLength)
            {
                message = null;
                return false;
            }

            body = new byte[contentLength];
            reader.TryCopyTo(body);
        }
        else
        {
            // read until the end
            body = new byte[reader.Remaining];
            reader.TryCopyTo(body);
        }

        _state = State.FirstLine;

        #endregion

        if (_isRequest)
            message = new HttpRequest
            {
                Method = _firstLine.Method,
                Path = _firstLine.Path!,
                Version = _firstLine.Version,
                Headers = _headers,
                Body = body
            };
        else
            message = new HttpResponse
            {
                Version = _firstLine.Version,
                StatusCode = (HttpStatusCode)_firstLine.StatusCode,
                Headers = _headers,
                Body = body
            };

        return true;
    }

    private static bool TryParseChunkSize(ref SequenceReader<byte> reader, out int chunkSize)
    {
        if (!reader.TryReadTo(out ReadOnlySpan<byte> chunkSizeSpan, Crlf))
        {
            chunkSize = 0;
            return false;
        }

        chunkSize = int.Parse(chunkSizeSpan.ToString(), NumberStyles.HexNumber);
        return true;
    }

    private static bool TryParseFirstLine(ref SequenceReader<byte> reader, bool request,
        [NotNullWhen(true)] out FirstLine? message)
    {
        if (!reader.TryReadTo(out ReadOnlySpan<byte> firstLineSpan, Crlf))
        {
            message = null;
            return false;
        }

        var len = Utf8Decoder.GetCharCount(firstLineSpan, true); // the flush flag is unused actually
        var firstLine = new char[len];
        Utf8Decoder.GetChars(firstLineSpan, firstLine, true);
        ReadOnlySpan<char> span = firstLine.AsSpan();

        var spaceIdx = span.IndexOf(' ');
        message = new FirstLine();
        if (request)
        {
            message.Method = span[..spaceIdx].ParseHttpMethod();
            span = span[(spaceIdx + 1)..];
            spaceIdx = span.IndexOf(' ');
            message.Path = span[..spaceIdx].ToString();
            span = span[(spaceIdx + 1)..];
            message.Version = span.ParseHttpVersion();
        }
        else
        {
            message.Version = span[..spaceIdx].ParseHttpVersion();
            span = span[(spaceIdx + 1)..];
            spaceIdx = span.IndexOf(' ');
            message.StatusCode = int.Parse(span[..spaceIdx]);
            span = span[(spaceIdx + 1)..];
            message.Status = span.ToString();
        }

        return true;
    }

    private static bool TryParseHeader(ref SequenceReader<byte> reader, out MultiDictionary headers)
    {
        headers = new MultiDictionary();
        if (!reader.TryReadTo(out ReadOnlySpan<byte> headerSpan, CrlfCrlf)) return false;

        var len = Utf8Decoder.GetCharCount(headerSpan, true); // the flush flag is unused actually
        var headerChars = new char[len];
        Utf8Decoder.GetChars(headerSpan, headerChars, true);
        ReadOnlySpan<char> span = headerChars.AsSpan();

        while (span.Length > 0)
        {
            var crlfIdx = span.IndexOf("\r\n");
            if (crlfIdx == -1) crlfIdx = span.Length;
            var header = span[..crlfIdx];
            var colonIdx = header.IndexOf(':');
            var key = header[..colonIdx].ToString();
            var value = header[(colonIdx + 1)..].ToString();
            headers.Add(key, value);
            if (crlfIdx == span.Length) break;
            span = span[(crlfIdx + 2)..];
        }

        return true;
    }
}

internal enum State
{
    FirstLine,
    Header,
    Body
}

internal class FirstLine
{
    public HttpMethod Method { get; set; }
    public string? Path { get; set; }
    public HttpVersion Version { get; set; }
    public int StatusCode { get; set; }
    public string? Status { get; set; }
}