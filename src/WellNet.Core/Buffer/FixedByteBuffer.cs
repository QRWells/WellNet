namespace QRWells.WellNet.Core.Buffer;

public sealed class FixedByteBuffer : ByteBuffer, IPoolingBuffer
{
    internal readonly int Index;

    internal FixedByteBuffer(FixedBufferPool pool, int index, Memory<byte> memory) : base(memory)
    {
        Pool = pool;
        Index = index;
    }

    public IBufferPool Pool { get; }

    public override void Dispose()
    {
        Pool.Return(this);
    }
}