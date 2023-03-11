using System.Buffers;

namespace QRWells.WellNet.Core.Buffer;

public class ByteBuffer : IMemoryOwner<byte>
{
    /// <summary>
    ///     The index of the buffer in the pool, invalid if not pooled
    /// </summary>
    internal readonly int Index;

    private int _readPosition;
    private int _writePosition;

    internal BufferPool? Pool;

    internal ByteBuffer(BufferPool pool, int index, Memory<byte> buffer)
    {
        Memory = buffer;
        Pool = pool;
        Index = index;
        Capacity = buffer.Length;
    }

    public ByteBuffer(int capacity)
    {
        Memory = new byte[capacity];
        Capacity = capacity;
    }

    public int Capacity { get; private set; }
    public Memory<byte> Memory { get; private set; }

    public void Dispose()
    {
        Release();

        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Returns the buffer to the pool it belongs to. (if it is pooled)
    /// </summary>
    /// <param name="clear">Whether to clear the buffer before returning it to the pool</param>
    public void Release(bool clear = true)
    {
        Pool?.Return(this, clear);
    }

    public void Clear()
    {
        _readPosition = 0;
        _writePosition = 0;
        Memory.Span.Clear();
    }

    public void Compact()
    {
        if (_readPosition == 0) return;
        var span = Memory.Span;
        span.Slice(_readPosition, _writePosition - _readPosition).CopyTo(span);
        _writePosition -= _readPosition;
        _readPosition = 0;
    }


    public void Reserve(long size)
    {
        if (size < Capacity) return;
        // TODO: Implement a better way to resize the buffer
        Release();
        Pool = null;
        Memory = new byte[size];
        Capacity = (int)size;
    }
}