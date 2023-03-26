using System.IO.Pipelines;
using System.Net.Sockets;
using QRWells.WellNet.Core.Pipeline;
using Serilog;

namespace QRWells.WellNet.Core.Tcp;

public sealed class TcpConnection : IDisposable
{
    private readonly PipeWriter _inboundWriter; // write to pipeline
    private readonly ILogger _logger = Log.ForContext<TcpConnection>();
    private readonly PipeReader _outboundReader; // read from pipeline

    private readonly Pipeline.Pipeline _pipeline;

    private readonly CancellationTokenSource _receiveCancel = new();
    private readonly CancellationTokenSource _sendCancel = new();
    private readonly TcpServer _server;
    private readonly Socket _socket;

    private bool _disposed;
    private bool _receiving;
    private bool _sending;

    internal TcpConnection(TcpServer server, Socket socket, Action<PipelineBuilder> pipelineBuilder)
    {
        _server = server;
        _socket = socket;
        var inboundPipe = new Pipe();
        var outboundPipe = new Pipe();
        _inboundWriter = inboundPipe.Writer;
        _outboundReader = outboundPipe.Reader;
        var builder = new PipelineBuilder(inboundPipe.Reader, outboundPipe.Writer, this);
        pipelineBuilder(builder);
        _pipeline = builder.Build();
    }

    public bool Connected { get; private set; } = true;
    public bool Closed { get; private set; }

    public Guid Id { get; } = Guid.NewGuid();

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        Close();

        _receiveCancel.Dispose();
        _sendCancel.Dispose();
    }

    public event Action<TcpConnection>? ConnectionClosed;

    internal void Start()
    {
        _logger.Information("Connection {Id} started", Id);
        Task.Run(ReceiveAsync, _receiveCancel.Token);
        Task.Run(SendAsync, _sendCancel.Token);
        _pipeline.Start();
    }

    private async Task ReceiveAsync()
    {
        _receiving = true;
        while (_receiving)
        {
            var mem = _inboundWriter.GetMemory(8192); // Get memory from the PipeWriter
            var result = await _socket.ReceiveAsync(mem);

            if (result == 0)
            {
                _logger.Information("Connection {Id} closed", Id);
                break;
            }

            _logger.Information("Connection {Id} received {Bytes} bytes", Id, result);

            _inboundWriter.Advance(result); // Inform the PipeWriter how much was read from the Socket

            var res = await _inboundWriter.FlushAsync(); // Inform the PipeReader that there's data to be read
            if (res.IsCompleted) break;
        }

        await _inboundWriter.CompleteAsync();
    }

    private async Task SendAsync()
    {
        _sending = true;
        while (_sending)
        {
            var result = await _outboundReader.ReadAsync();
            var buffer = result.Buffer;

            if (result.IsCanceled || result.IsCompleted) break;

            if (buffer.IsEmpty) continue;

            var closed = false;

            foreach (var segment in buffer)
            {
                var sent = await _socket.SendAsync(segment, SocketFlags.None);

                if (sent == 0)
                {
                    _logger.Information("Connection {Id} closed", Id);
                    closed = true;
                    break;
                }

                _logger.Information("Connection {Id} sent {Bytes} bytes", Id, sent);

                _outboundReader.AdvanceTo(buffer.GetPosition(sent));
            }

            if (closed) break;
        }

        await _outboundReader.CompleteAsync();
    }

    public void Disconnect()
    {
        if (!Connected) return;
        _receiving = false;
        _sending = false;
        _receiveCancel.Cancel();
        _socket.Disconnect(false);
        Connected = false;

        ConnectionClosed?.Invoke(this);

        _logger.Information("Connection {Id} disconnected", Id);
    }

    public void Close()
    {
        if (Closed) return;

        Disconnect();

        Closed = true;
        _pipeline.Close();
        _socket.Close();
        _server.ConnectionClosed(this);

        _logger.Information("Connection {Id} closed", Id);
    }
}