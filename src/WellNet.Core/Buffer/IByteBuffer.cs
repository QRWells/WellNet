namespace QRWells.WellNet.Core.Buffer;

public interface IByteBuffer : IDisposable
{
    /// <summary>
    ///     Endian of the buffer
    /// </summary>
    public Endian Endian { get; set; }

    /// <summary>
    ///     Total capacity
    /// </summary>
    public int Capacity { get; }

    /// <summary>
    ///     Readable bytes
    /// </summary>
    public int Size { get; }

    public int ReadIndex { get; }
    public int WriteIndex { get; }

    /// <summary>
    ///     Whole memory
    /// </summary>
    public Memory<byte> Memory { get; }

    public Memory<byte> ReadableMemory { get; }
    public Memory<byte> WritableMemory { get; }

    #region Operation

    /// <summary>
    ///     Increase the read index
    /// </summary>
    /// <param name="count">Count to increase</param>
    public void Advance(int count);

    /// <summary>
    ///     Decrease the read index
    /// </summary>
    /// <param name="count">Count to decrease</param>
    public void Rewind(int count);

    /// <summary>
    ///     Clear the buffer. (Set read and write index to 0)
    /// </summary>
    public void Clear();

    #endregion

    #region Write

    public void WriteByte(byte value);
    public void WriteShort(short value);
    public void WriteInt(int value);
    public void WriteLong(long value);
    public void WriteFloat(float value);
    public void WriteDouble(double value);
    public void Write(ReadOnlySpan<byte> span);

    #endregion

    #region Read

    public bool TryReadByte(out byte value);
    public bool TryReadShort(out short value);
    public bool TryReadInt(out int value);
    public bool TryReadLong(out long value);
    public bool TryReadFloat(out float value);
    public bool TryReadDouble(out double value);
    public int Read(Span<byte> span);

    #endregion
}

public enum Endian : byte
{
    Little,
    Big
}