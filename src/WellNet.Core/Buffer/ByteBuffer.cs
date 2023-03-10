namespace QRWells.WellNet.Core.Buffer;

public class ByteBuffer
{
    internal readonly int Index;
    internal readonly BufferPool? Pool;
    private int _readPosition;
    private int _writePosition;

    internal ByteBuffer(BufferPool pool, int index, Memory<byte> buffer)
    {
        Buffer = buffer;
        Pool = pool;
        Index = index;
        Capacity = buffer.Length;
    }

    public ByteBuffer(int capacity)
    {
        Buffer = new byte[capacity];
        Capacity = capacity;
    }

    public int Capacity { get; }
    public Memory<byte> Buffer { get; }

    public void Release(bool clear = true)
    {
        Pool?.Return(this, clear);
    }

    public void Clear()
    {
        _readPosition = 0;
        _writePosition = 0;
        Buffer.Span.Clear();
    }

    public void Compact()
    {
        if (_readPosition == 0) return;
        var span = Buffer.Span;
        span.Slice(_readPosition, _writePosition - _readPosition).CopyTo(span);
        _writePosition -= _readPosition;
        _readPosition = 0;
    }
}