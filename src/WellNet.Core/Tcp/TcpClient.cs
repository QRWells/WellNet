using System.Net;
using System.Net.Sockets;
using QRWells.WellNet.Core.Buffer;

namespace QRWells.WellNet.Core.Tcp;

public sealed class TcpClient : IDisposable
{
    /// <summary>
    ///     The minimum amount of data that needs to be in the receive buffer before it is passed to the pipeline
    /// </summary>
    public int ReceiveThreshold { get; set; } = 1024;

    public Guid Id { get; } = Guid.NewGuid();

    public bool Connected => _socket.Connected;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
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

    public void Send(ReadOnlyMemory<byte> data)
    {
        if (!Connected) return;
        lock (_sendBuffer)
        {
            _sendBuffer.Write(data.Span);
            _sendEvent.Set();
        }
    }

    private async Task ReceiveLoopAsync()
    {
        _receiving = true;
        var bytesReceived = 0;
        while (_receiving)
        {
            var result = await _socket.ReceiveAsync(_receiveBuffer.WritableMemory);

            if (result != 0)
            {
                _receiveBuffer.BytesWritten(result);
                bytesReceived += result;
                if (bytesReceived < ReceiveThreshold) continue; // Wait for more data
                if (OnDataReceived == null)
                {
                    _receiveBuffer.Clear(); // Clear the buffer if there is no handler
                    continue;
                }

                OnDataReceived.Invoke(this, _receiveBuffer.ReadableMemory);
                bytesReceived = 0;
                _receiveBuffer.Compact();
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

            while (_sendBuffer.Size > 0)
            {
                var result = await _socket.SendAsync(_sendBuffer.ReadableMemory);
                _sendBuffer.Advance(result);
                _sendBuffer.Compact();
            }
        }
    }

    public async Task DisconnectAsync()
    {
        if (!Connected) return;
        await _socket.DisconnectAsync(false);
    }

    #region signals

    private readonly CancellationTokenSource _receiveCancel = new();
    private readonly CancellationTokenSource _sendCancel = new();
    private readonly AutoResetEvent _sendEvent = new(false);
    private bool _receiving;
    private bool _sending;

    #endregion

    #region socket info

    private readonly Socket _socket;
    private EndPoint _endPoint;
    private bool _disposed;

    #endregion

    #region buffers

    private readonly ByteBuffer _receiveBuffer = new(16 * 1024);
    private readonly ByteBuffer _sendBuffer = new(16 * 1024);

    #endregion

    #region Tasks

    #endregion

    #region Constructors

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

    #endregion
}