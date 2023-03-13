using System.Net;
using System.Net.Sockets;
using QRWells.WellNet.Core.Buffer;

namespace QRWells.WellNet.Core.Tcp;

public sealed class TcpClient : IDisposable
{
    private readonly CancellationTokenSource _receiveCancel = new();
    private readonly TcpServer? _server;
    private readonly Socket _socket;
    private EndPoint _endPoint;
    private ByteBuffer _receiveBuffer;
    private bool _receiving;

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
        _socket.Dispose();
    }

    public async Task ConnectAsync()
    {
        if (Connected) return;
        await _socket.ConnectAsync(_endPoint);
        Task.Run(ReceiveLoopAsync, _receiveCancel.Token);
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
        Task.Run(ReceiveLoopAsync, _receiveCancel.Token);
    }

    private async Task ReceiveLoopAsync()
    {
        _receiving = true;
        while (_receiving)
        {
            var result = await _socket.ReceiveAsync(_receiveBuffer.Memory);
            if (result != 0) continue;
            _receiving = false;
            await DisconnectAsync();
            break;
        }
    }

    public async Task DisconnectAsync()
    {
        if (!Connected) return;
        await _socket.DisconnectAsync(false);
    }
}