using System.Net.Sockets;
using System.Numerics;

namespace QRWells.WellNet.Core.Buffer;

public sealed class BufferPool
{
    private readonly ByteBuffer[] _buffers;
    private readonly uint _bufferSize;

    private readonly bool[] _used;
    private Memory<byte> _underlyingMemory;

    public BufferPool(uint totalSize, uint bufferSize)
    {
        totalSize = BitOperations.RoundUpToPowerOf2(totalSize);
        bufferSize = BitOperations.RoundUpToPowerOf2(bufferSize);
        _underlyingMemory = new byte[totalSize];
        _bufferSize = bufferSize;

        var count = totalSize / bufferSize;
        _used = new bool[count];
        _buffers = new ByteBuffer[count];

        for (var i = 0; i < count; i++)
        {
            var buffer = new ByteBuffer(this, i,
                _underlyingMemory.Slice(i * (int)bufferSize, (int)bufferSize));
            _buffers[i] = buffer;
        }
    }

    public ByteBuffer Get()
    {
        for (var i = 0; i < _used.Length; i++)
        {
            if (_used[i]) continue;
            _used[i] = true;
            return _buffers[i];
        }

        return null; // todo: expand pool
    }

    public void Return(ByteBuffer item, bool clear = true)
    {
        // Not a pooled buffer
        if (item.Pool == null) return;
        // Not from this pool, delegate to the correct pool
        if (item.Pool != this) item.Release(clear);

        if (clear) item.Clear();
        _used[item.Index] = false;
    }

    public void SetBuffer(SocketAsyncEventArgs readWriteEventArg)
    {
        var buffer = Get();
        readWriteEventArg.SetBuffer(buffer.Buffer);
    }
}