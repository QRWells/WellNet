using System.Buffers;
using QRWells.WellNet.Core.Buffer;

namespace QRWells.WellNet.Core.Pipeline.Processor.Codec;

internal interface IByteMessageDecoder
{
    /// <summary>
    ///     Try to decode a message from the sequence
    /// </summary>
    /// <param name="sequence">The sequence to decode</param>
    /// <param name="message">The decoded message, null if not enough data</param>
    /// <param name="consumed">The number of bytes consumed, used only if a message was decoded</param>
    /// <returns>True if a valid message was decoded, false otherwise</returns>
    bool TryDecode(DecoderContext ctx, ReadOnlySequence<byte> sequence, out object? message, out long consumed);
}

internal interface IByteMessageEncoder
{
    /// <summary>
    ///     Try to encode a message to the writer
    /// </summary>
    /// <param name="message">The message to encode</param>
    /// <param name="writer">The writer to write to</param>
    void TryEncode(EncoderContext ctx, object message, IByteBuffer writer);
}

internal interface IByteMessageCodec : IByteMessageDecoder, IByteMessageEncoder
{
}