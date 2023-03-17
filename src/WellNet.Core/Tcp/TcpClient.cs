using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using QRWells.WellNet.Core.Buffer;

namespace QRWells.WellNet.Core.Tcp;

public sealed class TcpClient : IDisposable
{
    private readonly BufferPool _bufferPool = new(1024 * 1024, 8192);
    private readonly ByteBuffer _receiveBuffer = new(16 * 1024);
    private readonly CancellationTokenSource _receiveCancel = new();
    private readonly CancellationTokenSource _sendCancel = new();
    private readonly AutoResetEvent _sendEvent = new(false);
    private readonly ConcurrentQueue<SendState> _sendQueue = new();
    private readonly TcpServer? _server;
    private readonly Socket _socket;
    private EndPoint _endPoint;
    private bool _receiving;
    private bool _sending;

    public TcpClient()
    {
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    }

    public TcpClient(string host, int port) : this(new IPEndPoint(IPAddress.Parse(host), port))
    {
    }

    public TcpClient(EndPoint endPoint)
    {
        _socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _endPoint = endPoint;
    }

    internal TcpClient(TcpServer server, Socket socket)
    {
        _server = server;
        _socket = socket;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public bool CreatedByServer => _server != null;
    public bool Connected => _socket.Connected;

    public void Dispose()
    {
        _receiveCancel.Dispose();
        _sendCancel.Dispose();
        _sendEvent.Dispose();
        _socket.Dispose();
    }

    public event Action<TcpClient, Memory<byte>>? OnDataReceived;

    public async Task ConnectAsync()
    {
        if (Connected) return;
        await _socket.ConnectAsync(_endPoint);
        _ = Task.Run(ReceiveLoopAsync, _receiveCancel.Token);
        _ = Task.Run(SendLoopAsync, _sendCancel.Token);
    }

    public async Task ConnectAsync(string host, int port)
    {
        if (Connected) return;
        await ConnectAsync(new IPEndPoint(IPAddress.Parse(host), port));
    }

    public async Task ConnectAsync(EndPoint endPoint)
    {
        if (Connected) return;
        _endPoint = endPoint;
        await _socket.ConnectAsync(_endPoint);
        _ = Task.Run(ReceiveLoopAsync, _receiveCancel.Token);
        _ = Task.Run(SendLoopAsync, _sendCancel.Token);
    }

    public async Task SendAsync(ReadOnlyMemory<byte> data)
    {
        if (!Connected) return;

        var buffer = _bufferPool.Rent();
        data.CopyTo(buffer.Memory);
        var state = new SendState(buffer, new TaskCompletionSource());
        _sendQueue.Enqueue(state);
        _sendEvent.Set();
        await state.Args.Task;
    }

    private async Task ReceiveLoopAsync()
    {
        _receiving = true;
        while (_receiving)
        {
            var result = await _socket.ReceiveAsync(_receiveBuffer.Memory);

            if (result != 0)
            {
                OnDataReceived?.Invoke(this, _receiveBuffer.Memory[..result]);
                continue;
            }

            _receiving = false;
            await DisconnectAsync();
            break;
        }
    }

    private async Task SendLoopAsync()
    {
        _sending = true;
        while (_sending)
        {
            _sendEvent.WaitOne();

            while (_sendQueue.TryDequeue(out var state))
            {
                await _socket.SendAsync(state.Buffer.Memory);
                state.Args.SetResult();
            }
        }
    }

    public async Task DisconnectAsync()
    {
        if (!Connected) return;
        await _socket.DisconnectAsync(false);
    }
}