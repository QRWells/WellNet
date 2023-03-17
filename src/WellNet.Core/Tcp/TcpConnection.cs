using System.Collections.Concurrent;
using System.Net.Sockets;
using QRWells.WellNet.Core.Buffer;
using Serilog;

namespace QRWells.WellNet.Core.Tcp;

public sealed class TcpConnection : IDisposable
{
    private readonly ILogger _logger = Log.ForContext<TcpConnection>();

    private readonly ByteBuffer _receiveBuffer;

    private readonly CancellationTokenSource _receiveCancel = new();
    private readonly CancellationTokenSource _sendCancel = new();
    private readonly AutoResetEvent _sendEvent = new(false);
    private readonly ConcurrentQueue<SendState> _sendQueue = new();
    private readonly TcpServer _server;
    private readonly Socket _socket;

    private bool _disposed;
    private bool _receiving;
    private bool _sending;

    internal TcpConnection(TcpServer server, Socket socket)
    {
        _server = server;
        _socket = socket;
        _receiveBuffer = _server.RentBuffer();
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
        _receiveBuffer.Dispose();
        _sendEvent.Dispose();
    }

    public event Action<TcpConnection, Memory<byte>>? DataReceived;
    public event Action<TcpConnection>? ConnectionClosed;

    internal void Start()
    {
        _logger.Information("Connection {Id} started", Id);
        Task.Run(ReceiveAsync, _receiveCancel.Token);
        Task.Run(SendAsync, _sendCancel.Token);
    }

    private async Task ReceiveAsync()
    {
        _receiving = true;
        while (_receiving)
        {
            var result = await _socket.ReceiveAsync(_receiveBuffer.Memory);
            if (result == 0)
            {
                _logger.Information("Connection {Id} closed", Id);
                break;
            }

            DataReceived?.Invoke(this, _receiveBuffer.Memory[..result]);

            _logger.Information("Connection {Id} received {Bytes} bytes", Id, result);
        }
    }

    private async Task SendAsync()
    {
        _sending = true;
        while (_sending)
        {
            _sendEvent.WaitOne();
            while (_sendQueue.TryDequeue(out var state))
            {
                var buffer = state.Buffer;
                var args = state.Args;
                try
                {
                    await _socket.SendAsync(buffer.Memory);
                    _logger.Information("Connection {Id} sent {Bytes} bytes", Id, buffer.Memory.Length);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Connection {Id} send error", Id);
                }
                finally
                {
                    _server.ReturnBuffer(buffer);
                    args.SetResult();
                }
            }
        }
    }

    public async Task SendAsync(Memory<byte> data)
    {
        if (!Connected) return;

        var buffer = _server.RentBuffer();
        data.CopyTo(buffer.Memory);
        var args = new TaskCompletionSource();
        _sendQueue.Enqueue(new SendState(buffer, args));
        _sendEvent.Set();
        await args.Task;
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
        _socket.Close();
        _server.ReturnBuffer(_receiveBuffer);
        _server.ConnectionClosed(this);

        _logger.Information("Connection {Id} closed", Id);
    }
}