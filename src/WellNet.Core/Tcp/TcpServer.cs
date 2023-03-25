using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using QRWells.WellNet.Core.Pipeline;
using Serilog;

namespace QRWells.WellNet.Core.Tcp;

public sealed class TcpServer : IDisposable
{
    private readonly ConcurrentDictionary<Guid, TcpConnection> _connections = new();
    private readonly Guid _id = Guid.NewGuid();
    private readonly CancellationTokenSource _listenCancel = new();
    private readonly Socket _listener;
    private readonly ILogger _logger = Log.ForContext<TcpServer>();
    private readonly Action<PipelineBuilder> _pipelineBuilder;

    private bool _listening;

    internal TcpServer(EndPoint endPoint, Action<PipelineBuilder> pipelineBuilder)
    {
        _pipelineBuilder = pipelineBuilder;
        _listener = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _listener.Bind(endPoint);
        _listener.Listen(100);

        _logger.Information("Server {Id} listening on {EndPoint}", _id, endPoint);
    }

    public void Dispose()
    {
        _listening = false;
        _listenCancel.Dispose();
        _listener.Dispose();

        foreach (var connection in _connections.Values) connection.Dispose();
    }

    public event Action<TcpConnection>? OnConnectionClosed;

    public void Start()
    {
        Task.Run(AcceptConnectionsAsync, _listenCancel.Token);
    }

    private async Task AcceptConnectionsAsync()
    {
        _listening = true;
        while (_listening)
        {
            var socket = await _listener.AcceptAsync().ConfigureAwait(false);
            var connection = CreateConnection(socket);
            _logger.Information("Accepted connection {Id} from {EndPoint}", connection.Id, socket.RemoteEndPoint);
        }
    }

    private TcpConnection CreateConnection(Socket socket)
    {
        var connection = new TcpConnection(this, socket, _pipelineBuilder);
        connection.ConnectionClosed += OnConnectionClosed;
        _connections.TryAdd(connection.Id, connection);
        connection.Start();

        _logger.Information("Accepted connection {Id} from {EndPoint}", connection.Id, socket.RemoteEndPoint);

        return connection;
    }

    public void StopListening()
    {
        _listening = false;
        _listenCancel.Cancel();

        _logger.Information("Server {Id} stopped listening", _id);
    }

    public void Stop()
    {
        StopListening();
        foreach (var connection in _connections.Values) connection.Close();

        _logger.Information("Server {Id} stopped", _id);
    }

    internal void ConnectionClosed(TcpConnection connection)
    {
        _connections.TryRemove(connection.Id, out _);
    }
}