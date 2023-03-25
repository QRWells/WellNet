using System.Runtime.CompilerServices;

namespace QRWells.WellNet.Core.Buffer;

public sealed class ByteBuffer : IByteBuffer
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
    public int Size => _writePosition - _readPosition;
    public Memory<byte> Memory { get; private set; }
    public Memory<byte> WrittenMemory => Memory[_readPosition.._writePosition];

    public void Dispose()
    {
        Release();
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

    public void Write<T>(T value) where T : unmanaged
    {
        var size = Unsafe.SizeOf<T>();
        ReserveNew(size);
        Unsafe.WriteUnaligned(ref Memory.Span[_writePosition], value);
        _writePosition += size;
    }

    public void Write(ReadOnlySpan<byte> span)
    {
        ReserveNew(span.Length);
        span.CopyTo(Memory.Span[_writePosition..]);
        _writePosition += span.Length;
    }

    public void Read(Span<byte> span)
    {
        if (span.Length > Size) throw new ArgumentOutOfRangeException(nameof(span));
        Memory.Span[_readPosition.._writePosition][..span.Length].CopyTo(span);
        _readPosition += span.Length;
    }

    public void Advance(int count)
    {
        _writePosition += count;
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

    private void ReserveNew(int size)
    {
        if (size < Capacity - _writePosition) return;
        if (size < Capacity - _readPosition) Compact();
    }
}