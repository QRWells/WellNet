using System.Buffers;
using System.Diagnostics;
using QRWells.WellNet.Core.Buffer;

namespace QRWells.WellNet.Core.Pipeline.Processor.Codec;

public abstract class ByteMessageDecoder<TMessage> : IByteMessageDecoder
{
    public bool TryDecode(DecoderContext ctx, ReadOnlySequence<byte> sequence, out object? message, out long consumed)
    {
        var reader = new SequenceReader<byte>(sequence);
        if (TryDecode(ctx, ref reader, out var typedMessage))
        {
            message = typedMessage;
            consumed = reader.Consumed;
            return true;
        }

        message = null;
        consumed = 0;
        return false;
    }

    protected abstract bool TryDecode(DecoderContext ctx, ref SequenceReader<byte> reader, out TMessage message);
}

public abstract class ByteMessageEncoder<TMessage> : IByteMessageEncoder
{
    public void TryEncode(EncoderContext ctx, object message, IByteBuffer writer)
    {
        Debug.Assert(message is TMessage, "message should be of type TMessage");
        Encode(ctx, (TMessage)message, writer);
    }

    protected abstract void Encode(EncoderContext ctx, TMessage message, IByteBuffer writer);
}