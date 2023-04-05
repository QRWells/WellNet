namespace QRWells.WellNet.Core.Buffer;

public interface IBufferPool
{
    public static IBufferPool Default => FixedBufferPool.Default;
    public IByteBuffer Rent(int sizeHint = 0);
    public void Return(IPoolingBuffer buffer, bool clear = true);
}