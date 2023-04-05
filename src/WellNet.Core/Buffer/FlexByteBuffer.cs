namespace QRWells.WellNet.Core.Buffer;

public sealed class FlexByteBuffer : ByteBuffer, IPoolingBuffer
{
    internal readonly uint Offset;

    internal FlexByteBuffer(FlexBufferPool pool, uint offset, Memory<byte> memory) : base(memory)
    {
        Pool = pool;
        Offset = offset;
    }

    public IBufferPool Pool { get; }

    public override void Dispose()
    {
        Pool.Return(this);
    }
}