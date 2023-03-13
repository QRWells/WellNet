using System.Net.Sockets;
using System.Numerics;

namespace QRWells.WellNet.Core.Buffer;

public sealed class BufferPool
{
    private readonly ByteBuffer[] _buffers;
    private readonly uint _bufferSize;
    private readonly Memory<byte> _underlyingBuffer;

    private readonly bool[] _used;

    public BufferPool(uint totalSize, uint bufferSize)
    {
        totalSize = BitOperations.RoundUpToPowerOf2(totalSize);
        bufferSize = BitOperations.RoundUpToPowerOf2(bufferSize);
        _underlyingBuffer = new byte[totalSize];
        _bufferSize = bufferSize;

        var count = totalSize / bufferSize;
        _used = new bool[count];
        _buffers = new ByteBuffer[count];

        for (var i = 0; i < count; i++)
        {
            var slice = _underlyingBuffer.Slice((int)(i * bufferSize), (int)bufferSize);
            var buffer = new ByteBuffer(this, i, slice);
            _buffers[i] = buffer;
        }
    }

    public ByteBuffer Rent()
    {
        for (var i = 0; i < _used.Length; i++)
        {
            if (_used[i]) continue;
            _used[i] = true;
            return _buffers[i];
        }

        // Allocate a new buffer, and discard it when it is returned
        return new ByteBuffer(this, -1, new byte[_bufferSize]);
    }

    public void Return(ByteBuffer item, bool clear = true)
    {
        // Not a pooled buffer
        if (item.Pool == null) return;
        // Not from this pool, delegate to the correct pool
        if (item.Pool != this)
        {
            item.Release(clear);
            return;
        }

        // Already returned
        if (!_used[item.Index]) return;

        // Not a pooled buffer
        if (item.Index == -1) return;

        if (clear) item.Clear();
        _used[item.Index] = false;
    }

    public void SetBuffer(SocketAsyncEventArgs readWriteEventArg)
    {
        var buffer = Rent();
        readWriteEventArg.SetBuffer(buffer.Memory);
    }
}