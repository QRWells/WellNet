namespace QRWells.WellNet.Core.Buffer;

public interface IPoolingBuffer
{
    public IBufferPool Pool { get; }

    public void Return(bool clear = true)
    {
        Pool.Return(this, clear);
    }
}