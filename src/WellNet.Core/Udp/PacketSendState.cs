using System.Net;
using System.Net.Sockets;
using QRWells.WellNet.Core.Buffer;

namespace QRWells.WellNet.Core.Udp;

internal sealed class PacketSendState : IDisposable
{
    public PacketSendState(ByteBuffer data, TaskCompletionSource args, SocketFlags flags, EndPoint endPoint)
    {
        Data = data;
        Args = args;
        Flags = flags;
        EndPoint = endPoint;
    }

    public ByteBuffer Data { get; }
    public TaskCompletionSource Args { get; }
    public SocketFlags Flags { get; }
    public EndPoint EndPoint { get; }

    public void Dispose()
    {
        Data.Dispose();
    }
}