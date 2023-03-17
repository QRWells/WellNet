using QRWells.WellNet.Core.Buffer;

namespace QRWells.WellNet.Core.Tcp;

internal class SendState
{
    public SendState(ByteBuffer buffer, TaskCompletionSource args)
    {
        Buffer = buffer;
        Args = args;
    }

    public ByteBuffer Buffer { get; }
    public TaskCompletionSource Args { get; }
}