using System.Numerics;

namespace QRWells.WellNet.Core.Buffer;

public sealed class FlexBufferPool : IBufferPool
{
    private readonly uint _chunkNum;
    private readonly uint _chunkSize;
    private readonly uint[] _longest;
    private readonly uint _poolSize;

    private readonly Memory<byte> _rawBuffer;

    public FlexBufferPool(uint poolSize, uint chunkSize)
    {
        poolSize = Math.Max(poolSize, 4 * 1024);
        poolSize = BitOperations.RoundUpToPowerOf2(poolSize);
        _poolSize = poolSize;

        chunkSize = Math.Max(chunkSize, 64);
        chunkSize = BitOperations.RoundUpToPowerOf2(chunkSize);
        _chunkSize = chunkSize;

        _rawBuffer = new byte[poolSize];
        _chunkNum = poolSize / chunkSize;

        _longest = new uint[2 * _chunkNum - 1];
        var nodeSize = _chunkNum * 2;
        for (var i = 0; i < _longest.Length; i++)
        {
            if (BitOperations.IsPow2(i + 1)) nodeSize /= 2;

            _longest[i] = nodeSize;
        }
    }

    public static FlexBufferPool Default { get; } = new(1024 * 1024, 1024);

    public IByteBuffer Rent(int sizeHint = 0)
    {
        sizeHint = Math.Max(sizeHint, 64);
        var chunks = (uint)(sizeHint / _chunkSize);
        // chunks need to be allocated
        chunks = sizeHint % _chunkSize == 0
            ? BitOperations.RoundUpToPowerOf2(chunks)
            : BitOperations.RoundUpToPowerOf2(chunks + 1);
        if (_longest[0] < chunks)
        {
            return new ByteBuffer(sizeHint);
        }

        uint nodeSize;
        uint index = 0;

        for (nodeSize = _chunkNum; nodeSize != chunks; nodeSize /= 2)
        {
            var left = _longest[LeftLeaf(index)];
            var right = _longest[RightLeaf(index)];
            if (left <= right)
                index = left >= chunks ? LeftLeaf(index) : RightLeaf(index);
            else
                index = right >= chunks ? RightLeaf(index) : LeftLeaf(index);
        }

        _longest[index] = 0;
        var offset = (index + 1) * nodeSize - _chunkNum;

        while (index != 0)
        {
            index = Parent(index);
            _longest[index] = Math.Max(_longest[LeftLeaf(index)],
                _longest[RightLeaf(index)]);
        }

        var buffer = _rawBuffer.Slice((int)(offset * _chunkSize), (int)(chunks * _chunkSize));
        return new FlexByteBuffer(this, offset, buffer);
    }

    public void Return(IPoolingBuffer buffer, bool clear = true)
    {
        if (buffer is not FlexByteBuffer flexByteBuffer) return;
        if (clear) flexByteBuffer.Clear();
        var offset = flexByteBuffer.Offset;
        var nodeSize = 1u;
        var index = offset + _chunkNum - 1;
        for (; _longest[index] != 0; index = Parent(index))
        {
            nodeSize *= 2;
            if (index == 0)
                return;
        }

        _longest[index] = nodeSize;

        // merge contiguous free chunks
        while (index != 0)
        {
            index = Parent(index);
            nodeSize *= 2;

            var leftLongest = _longest[LeftLeaf(index)];
            var rightLongest = _longest[RightLeaf(index)];

            if (leftLongest + rightLongest == nodeSize)
                _longest[index] = nodeSize;
            else
                _longest[index] = Math.Max(leftLongest, rightLongest);
        }
    }

    private static uint LeftLeaf(uint index)
    {
        return index * 2 + 1;
    }

    private static uint RightLeaf(uint index)
    {
        return index * 2 + 2;
    }

    private static uint Parent(uint index)
    {
        return (index + 1) / 2 - 1;
    }
}