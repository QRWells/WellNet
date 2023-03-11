using System.Net.Sockets;
using QRWells.WellNet.Core.Buffer;
using Serilog;

namespace QRWells.WellNet.Core.Tcp;

public class TcpConnection : IDisposable
{
    private readonly ILogger _logger = Log.ForContext<TcpConnection>();
    private readonly ByteBuffer _readBuffer;
    private readonly SocketAsyncEventArgs _readEventArgs;
    private readonly object _sendLock = new();
    private readonly TcpServer _server;
    private readonly Socket _socket;
    private readonly ByteBuffer _writeBuffer;
    private readonly SocketAsyncEventArgs _writeEventArgs;

    internal TcpConnection(TcpServer server, Socket socket)
    {
        _server = server;
        _socket = socket;

        _writeEventArgs = new SocketAsyncEventArgs();
        _writeEventArgs.Completed += OnIoCompleted;

        _readEventArgs = new SocketAsyncEventArgs();
        _readEventArgs.Completed += OnIoCompleted;

        _readBuffer = _server.GetBuffer();
        _writeBuffer = _server.GetBuffer();

        EstablishConnection();
    }

    public long BytesReceived { get; private set; }
    public long BytesSent { get; private set; }

    public Guid Id { get; } = Guid.NewGuid();
    public bool IsConnected { get; private set; }

    public void Dispose()
    {
        _socket.Dispose();
        _readBuffer.Release();
        _writeBuffer.Release();
    }

    private void EstablishConnection()
    {
        TryReceive();
        IsConnected = true;
        _server.InternalConnectionEstablished(this);
    }

    private void OnIoCompleted(object? sender, SocketAsyncEventArgs e)
    {
        // determine which type of operation just completed and call the associated handler
        switch (e.LastOperation)
        {
            case SocketAsyncOperation.Receive:
                ProcessReceive(e);
                break;
            case SocketAsyncOperation.Send:
                ProcessSend(e);
                break;
            default:
                throw new ArgumentException("The last operation completed on the socket was not a receive or send");
        }
    }

    private void TryReceive()
    {
        if (!IsConnected)
            return;

        var process = true;

        while (process)
        {
            process = false;

            _readEventArgs.SetBuffer(_readBuffer.Memory);
            if (!_socket.ReceiveAsync(_readEventArgs))
                process = ProcessReceive(_readEventArgs);
        }
    }

    private bool ProcessReceive(SocketAsyncEventArgs e)
    {
        if (!IsConnected) return false;

        long size = e.BytesTransferred;

        // Received some data from the client
        if (size > 0)
        {
            // Update statistic
            BytesReceived += size;

            // Call the buffer received handler
            _server.DataReceived(this, _readBuffer.Memory[..(int)size]);

            // If the receive buffer is full increase its size
            if (_readBuffer.Capacity == size) _readBuffer.Reserve(2 * size);
        }

        // check if the remote host closed the connection
        if (e.SocketError == SocketError.Success)
        {
            if (e.BytesTransferred > 0) return true;
            Disconnect(e);
        }
        else
        {
            _server.InternalConnectionError(this);
            Disconnect(e);
        }

        return false;
    }

    // This method is invoked when an asynchronous send operation completes.
    // The method issues another receive on the socket to read any additional
    // data sent from the client
    //
    // <param name="e"></param>
    private void ProcessSend(SocketAsyncEventArgs e)
    {
        if (e.SocketError == SocketError.Success)
        {
            // read the next block of data send from the client
            if (!_socket.ReceiveAsync(e)) ProcessReceive(e);
        }
        else
        {
            Disconnect(e);
        }
    }

    private void Disconnect(SocketAsyncEventArgs e)
    {
        _readEventArgs.Completed -= OnIoCompleted;
        _writeEventArgs.Completed -= OnIoCompleted;
        try
        {
            _socket.Shutdown(SocketShutdown.Send);
        }
        catch (Exception)
        {
            _logger.Error("Socket shutdown failed");
        }

        _socket.Close();
        _socket.Dispose();
        _readEventArgs.Dispose();
        _writeEventArgs.Dispose();

        _readBuffer.Dispose();
        _writeBuffer.Dispose();

        IsConnected = false;

        _server.InternalDisconnect(this);
    }
}