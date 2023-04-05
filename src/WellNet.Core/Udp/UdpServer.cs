using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using QRWells.WellNet.Core.Buffer;
using Serilog;

namespace QRWells.WellNet.Core.Udp;

public sealed class UdpServer : IDisposable
{
    private readonly byte[] _buffer = new byte[8192];
    private readonly IBufferPool _bufferPool = IBufferPool.Default;
    private readonly ILogger _logger = Log.ForContext<UdpServer>();
    private readonly CancellationTokenSource _receiveCancel = new();
    private readonly CancellationTokenSource _sendCancel = new();
    private readonly AutoResetEvent _sendEvent = new(false);
    private readonly ConcurrentQueue<PacketSendState> _sendQueue = new();
    private readonly Socket _socket;
    private bool _receiving;
    private bool _sending;

    public UdpServer(int port)
    {
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _socket.Bind(new IPEndPoint(IPAddress.Any, port));
        _ = Task.Run(ReceiveLoopAsync, _receiveCancel.Token);
        _ = Task.Run(SendLoopAsync, _sendCancel.Token);
    }

    public void Dispose()
    {
        _receiveCancel.Dispose();
        _socket.Dispose();
    }

    public event Action<Memory<byte>>? OnDataReceived;

    public async Task SendAsync(EndPoint endPoint, Memory<byte> data, SocketFlags flags = SocketFlags.None)
    {
        var buffer = _bufferPool.Rent();
        data.CopyTo(buffer.Memory);
        var args = new TaskCompletionSource();
        _sendQueue.Enqueue(new PacketSendState(buffer, args, flags, endPoint));
        _sendEvent.Set();
        await args.Task;
    }

    private async Task ReceiveLoopAsync()
    {
        _receiving = true;
        while (_receiving)
        {
            var result = await _socket.ReceiveAsync(_buffer, SocketFlags.None, _receiveCancel.Token);
            OnDataReceived?.Invoke(_buffer.AsMemory(0, result));
        }

        _receiving = false;
    }

    private async Task SendLoopAsync()
    {
        _sending = true;
        while (_sending)
        {
            _sendEvent.WaitOne();
            while (_sendQueue.TryDequeue(out var state))
                try
                {
                    await _socket.SendToAsync(state.Data.Memory, state.Flags, state.EndPoint);
                    _logger.Debug("Sent packet to {EndPoint}", state.EndPoint);
                    state.Args.SetResult();
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Error sending packet");
                }
                finally
                {
                    state.Dispose();
                }
        }

        _sending = false;
    }
}