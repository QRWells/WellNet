namespace QRWells.WellNet.Core.Buffer;

public interface IBufferPool
{
    public IByteBuffer Rent();
    public void Return(ByteBuffer item, bool clear = true);
}