using QRWells.WellNet.Core.Buffer;

namespace QRWells.WellNet.Core.Tcp;

internal class SendState : IDisposable
{
    public SendState(IByteBuffer buffer, TaskCompletionSource args)
    {
        Buffer = buffer;
        Args = args;
    }

    public IByteBuffer Buffer { get; }
    public TaskCompletionSource Args { get; }

    public void Dispose()
    {
        Buffer.Dispose();
    }
}