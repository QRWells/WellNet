using System.Buffers.Binary;
using System.Diagnostics;

namespace QRWells.WellNet.Core.Buffer;

public class ByteBuffer : IByteBuffer
{
    public ByteBuffer(int capacity)
    {
        Memory = new byte[capacity];
        Capacity = capacity;
    }

    internal ByteBuffer(Memory<byte> memory)
    {
        Memory = memory;
        Capacity = memory.Length;
    }

    public Endian Endian { get; set; } = Endian.Little;
    public int Capacity { get; }

    public int ReadIndex { get; set; }
    public int WriteIndex { get; set; }
    public int Size => WriteIndex - ReadIndex;
    public Memory<byte> Memory { get; }
    public Memory<byte> ReadableMemory => Memory[ReadIndex..WriteIndex];
    public Memory<byte> WritableMemory => Memory[WriteIndex..];

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public void Clear()
    {
        ReadIndex = 0;
        WriteIndex = 0;
        Memory.Span.Clear();
    }

    public void Advance(int count)
    {
        Debug.Assert(count <= Size);
        ReadIndex += count;
    }

    public void Rewind(int count)
    {
        Debug.Assert(count <= ReadIndex);
        ReadIndex -= count;
    }

    internal void BytesWritten(int count)
    {
        WriteIndex += count;
    }

    public void Compact()
    {
        if (ReadIndex == 0) return;
        var span = Memory.Span;
        span.Slice(ReadIndex, WriteIndex - ReadIndex).CopyTo(span);
        WriteIndex -= ReadIndex;
        ReadIndex = 0;
    }

    private void Reserve(int size)
    {
        if (size < Capacity - WriteIndex) return;
        if (size < Capacity - ReadIndex) Compact();
    }

    #region Write

    public void WriteByte(byte value)
    {
        Reserve(1);
        Memory.Span[WriteIndex] = value;
        WriteIndex++;
    }

    public void WriteShort(short value)
    {
        Reserve(2);
        if (Endian == Endian.Little)
            BinaryPrimitives.WriteInt16LittleEndian(Memory.Span[WriteIndex..], value);
        else
            BinaryPrimitives.WriteInt16BigEndian(Memory.Span[WriteIndex..], value);
    }

    public void WriteInt(int value)
    {
        Reserve(4);
        if (Endian == Endian.Little)
            BinaryPrimitives.WriteInt32LittleEndian(Memory.Span[WriteIndex..], value);
        else
            BinaryPrimitives.WriteInt32BigEndian(Memory.Span[WriteIndex..], value);
    }

    public void WriteLong(long value)
    {
        Reserve(8);
        if (Endian == Endian.Little)
            BinaryPrimitives.WriteInt64LittleEndian(Memory.Span[WriteIndex..], value);
        else
            BinaryPrimitives.WriteInt64BigEndian(Memory.Span[WriteIndex..], value);
    }

    public void WriteFloat(float value)
    {
        Reserve(4);
        if (Endian == Endian.Little)
            BinaryPrimitives.WriteSingleLittleEndian(Memory.Span[WriteIndex..], value);
        else
            BinaryPrimitives.WriteSingleBigEndian(Memory.Span[WriteIndex..], value);
    }

    public void WriteDouble(double value)
    {
        Reserve(8);
        if (Endian == Endian.Little)
            BinaryPrimitives.WriteDoubleLittleEndian(Memory.Span[WriteIndex..], value);
        else
            BinaryPrimitives.WriteDoubleBigEndian(Memory.Span[WriteIndex..], value);
    }

    public void Write(ReadOnlySpan<byte> span)
    {
        Reserve(span.Length);
        span.CopyTo(Memory.Span[WriteIndex..]);
        WriteIndex += span.Length;
    }

    #endregion

    #region Read

    public bool TryReadByte(out byte value)
    {
        if (ReadIndex + 1 > WriteIndex)
        {
            value = default;
            return false;
        }

        value = Memory.Span[ReadIndex];
        ReadIndex++;
        return true;
    }

    public bool TryReadShort(out short value)
    {
        if (ReadIndex + 2 > WriteIndex)
        {
            value = default;
            return false;
        }

        value = Endian == Endian.Little
            ? BinaryPrimitives.ReadInt16LittleEndian(Memory.Span[ReadIndex..])
            : BinaryPrimitives.ReadInt16BigEndian(Memory.Span[ReadIndex..]);
        ReadIndex += 2;
        return true;
    }

    public bool TryReadInt(out int value)
    {
        if (ReadIndex + 4 > WriteIndex)
        {
            value = default;
            return false;
        }

        value = Endian == Endian.Little
            ? BinaryPrimitives.ReadInt32LittleEndian(Memory.Span[ReadIndex..])
            : BinaryPrimitives.ReadInt32BigEndian(Memory.Span[ReadIndex..]);
        ReadIndex += 4;
        return true;
    }

    public bool TryReadLong(out long value)
    {
        if (ReadIndex + 8 > WriteIndex)
        {
            value = default;
            return false;
        }

        value = Endian == Endian.Little
            ? BinaryPrimitives.ReadInt64LittleEndian(Memory.Span[ReadIndex..])
            : BinaryPrimitives.ReadInt64BigEndian(Memory.Span[ReadIndex..]);
        ReadIndex += 8;
        return true;
    }

    public bool TryReadFloat(out float value)
    {
        if (ReadIndex + 4 > WriteIndex)
        {
            value = default;
            return false;
        }

        value = Endian == Endian.Little
            ? BinaryPrimitives.ReadSingleLittleEndian(Memory.Span[ReadIndex..])
            : BinaryPrimitives.ReadSingleBigEndian(Memory.Span[ReadIndex..]);
        ReadIndex += 4;
        return true;
    }

    public bool TryReadDouble(out double value)
    {
        if (ReadIndex + 8 > WriteIndex)
        {
            value = default;
            return false;
        }

        value = Endian == Endian.Little
            ? BinaryPrimitives.ReadDoubleLittleEndian(Memory.Span[ReadIndex..])
            : BinaryPrimitives.ReadDoubleBigEndian(Memory.Span[ReadIndex..]);
        ReadIndex += 8;
        return true;
    }

    public int Read(Span<byte> span)
    {
        var count = Math.Min(span.Length, Size);
        Memory.Span[ReadIndex..(ReadIndex + count)].CopyTo(span);
        ReadIndex += count;
        return count;
    }

    #endregion
}