namespace QRWells.WellNet.Core.Buffer;

public interface IByteBuffer : IDisposable
{
    public int Capacity { get; }
    public int Size { get; }
    public Memory<byte> Memory { get; }
    public Memory<byte> WrittenMemory { get; }
    public void Clear();
    public void Release(bool clear = true);
    public void Write(ReadOnlySpan<byte> span);
    public void Write<T>(T value) where T : unmanaged;
    public void Read(Span<byte> span);
    public void Advance(int count);
}