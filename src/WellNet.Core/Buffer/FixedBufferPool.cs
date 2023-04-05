using System.Numerics;

namespace QRWells.WellNet.Core.Buffer;

/// <summary>
///     A fixed-size buffer pool
/// </summary>
public sealed class FixedBufferPool : IBufferPool
{
    private readonly FixedByteBuffer[] _buffers;
    private readonly uint _bufferSize;
    private readonly bool[] _used;

    public FixedBufferPool(uint totalSize, uint bufferSize)
    {
        totalSize = BitOperations.RoundUpToPowerOf2(totalSize);
        bufferSize = BitOperations.RoundUpToPowerOf2(bufferSize);
        Memory<byte> underlyingBuffer = new byte[totalSize];
        _bufferSize = bufferSize;

        var count = totalSize / bufferSize;
        _used = new bool[count];
        _buffers = new FixedByteBuffer[count];

        for (var i = 0; i < count; i++)
        {
            var slice = underlyingBuffer.Slice((int)(i * bufferSize), (int)bufferSize);
            var buffer = new FixedByteBuffer(this, i, slice);
            _buffers[i] = buffer;
        }
    }

    public static FixedBufferPool Default { get; } = new(1024 * 1024, 1024);

    public IByteBuffer Rent(int sizeHint = 0)
    {
        for (var i = 0; i < _used.Length; i++)
        {
            if (_used[i]) continue;
            _used[i] = true;
            return _buffers[i];
        }

        // Allocate a new buffer, and discard it when it is returned
        return new ByteBuffer((int)_bufferSize);
    }

    public void Return(IPoolingBuffer item, bool clear = true)
    {
        if (item is not FixedByteBuffer fixedBuffer) return;

        // Not from this pool, delegate to the correct pool
        if (item.Pool != this)
        {
            item.Return(clear);
            return;
        }

        // Already returned
        if (!_used[fixedBuffer.Index]) return;

        // Not a pooled buffer
        if (fixedBuffer.Index == -1) return;

        if (clear) fixedBuffer.Clear();
        _used[fixedBuffer.Index] = false;
    }
}