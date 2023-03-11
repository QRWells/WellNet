using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using QRWells.WellNet.Core.Buffer;
using Serilog;

namespace QRWells.WellNet.Core.Tcp;

public class TcpServer : IDisposable
{
    private readonly SocketAsyncEventArgs _acceptSocketAsyncEventArgs = new();
    private readonly int _backlog = 100;
    private readonly BufferPool _bufferPool;
    private readonly ConcurrentDictionary<Guid, TcpConnection> _connections = new();
    private readonly ILogger _logger = Log.ForContext<TcpServer>();

    /// <summary>
    ///     the maximum number of connections to handle simultaneously
    /// </summary>
    private readonly int _maxConnections;

    private Socket _listenSocket; // the socket used to listen for incoming connection requests
    private int _receiveBufferSize; // buffer size to use for each socket I/O operation
    private int _totalBytesRead; // counter of the total # bytes received by the server

    public TcpServer() : this(8192)
    {
    }

    // Create an uninitialized server instance.
    // To start the server listening for connection requests
    // call the Init method followed by Start method
    //
    // <param name="numConnections">the maximum number of connections the sample is designed to handle simultaneously</param>
    // <param name="receiveBufferSize">buffer size to use for each socket I/O operation</param>
    internal TcpServer(int receiveBufferSize)
    {
        _receiveBufferSize = receiveBufferSize;

        _bufferPool = new BufferPool((uint)(receiveBufferSize * 16 * 2), (uint)receiveBufferSize);
    }

    public Action<TcpConnection, Memory<byte>> DataReceived { get; set; } = (_, _) => { };

    public void Dispose()
    {
        _listenSocket.Dispose();

        GC.SuppressFinalize(this);
    }

    public event EventHandler? Started;
    public event EventHandler? Stopped;

    public event EventHandler<TcpConnection>? Connecting;
    public event EventHandler<TcpConnection>? ConnectionEstablished;
    public event EventHandler<TcpConnection>? ConnectionClosed;
    public event EventHandler<TcpConnection>? ConnectionError;

    protected void Dispose(bool disposing)
    {
        if (disposing) _listenSocket.Dispose();
    }

    /// <summary>
    ///     Start listening for incoming connection requests
    /// </summary>
    /// <param name="localEndPoint">The endpoint which the server will listening for connection requests on</param>
    public void Start(IPEndPoint localEndPoint)
    {
        _listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _listenSocket.Bind(localEndPoint);
        _listenSocket.Listen(_backlog);

        _acceptSocketAsyncEventArgs.Completed += (_, e) =>
        {
            ProcessAccept(e);
            StartAccept(e);
        };
        StartAccept(_acceptSocketAsyncEventArgs);
    }

    public void Stop()
    {
        _listenSocket.Close();
    }

    private void StartAccept(SocketAsyncEventArgs acceptEventArg)
    {
        // socket must be cleared since the context object is being reused
        acceptEventArg.AcceptSocket = null;
        if (!_listenSocket.AcceptAsync(acceptEventArg)) ProcessAccept(acceptEventArg);
    }

    private void ProcessAccept(SocketAsyncEventArgs e)
    {
        if (e.SocketError == SocketError.Success)
        {
            var connection = new TcpConnection(this, e.AcceptSocket!);
            _connections.TryAdd(connection.Id, connection);
        }

        // accept the next connection request
        StartAccept(e);
    }

    internal ByteBuffer GetBuffer()
    {
        return _bufferPool.Rent();
    }

    internal void InternalConnectionError(TcpConnection connection)
    {
        ConnectionError?.Invoke(this, connection);
    }

    internal void InternalDisconnect(TcpConnection connection)
    {
        ConnectionClosed?.Invoke(this, connection);
        _connections.TryRemove(connection.Id, out _);
    }

    internal void InternalConnectionEstablished(TcpConnection connection)
    {
        ConnectionEstablished?.Invoke(this, connection);
    }
}