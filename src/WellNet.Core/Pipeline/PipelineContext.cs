using System.Buffers;
using QRWells.WellNet.Core.Buffer;
using QRWells.WellNet.Core.Pipeline.Processor;
using QRWells.WellNet.Core.Pipeline.Processor.Codec;

namespace QRWells.WellNet.Core.Pipeline;

public abstract class PipelineContext
{
    public PipelineContext? Next { get; set; }
    public PipelineContext? Previous { get; set; }
    public abstract IProcessor Processor { get; }

    public abstract void NextInbound(object message);

    public abstract void NextOutbound(object message);
}

internal sealed class DefaultPipelineContext : PipelineContext
{
    public DefaultPipelineContext(IProcessor processor)
    {
        Processor = processor;
    }

    public override IProcessor Processor { get; }

    public override void NextInbound(object message)
    {
        if (Processor is IInboundProcessor inboundProcessor)
            inboundProcessor.ReadInbound(this, message);
        else
            Next?.NextInbound(message);
    }

    public override void NextOutbound(object message)
    {
        if (Processor is IOutboundProcessor outboundProcessor)
            outboundProcessor.WriteOutbound(this, message);
        else
            Next?.NextOutbound(message);
    }
}

public sealed class DecoderContext
{
    internal readonly IByteMessageDecoder _decoder;

    internal DecoderContext(Pipeline pipeline, IByteMessageDecoder decoder)
    {
        Pipeline = pipeline;
        _decoder = decoder;
    }

    public Pipeline Pipeline { get; }

    internal bool TryDecode(ReadOnlySequence<byte> buffer, out object? message, out long consumed)
    {
        return _decoder.TryDecode(this, buffer, out message, out consumed);
    }
}

public sealed class EncoderContext
{
    private readonly IByteMessageEncoder _encoder;

    internal EncoderContext(Pipeline pipeline, IByteMessageEncoder encoder)
    {
        Pipeline = pipeline;
        _encoder = encoder;
    }

    public Pipeline Pipeline { get; }

    internal void TryEncode(object message, IByteBuffer buffer)
    {
        _encoder.TryEncode(this, message, buffer);
    }
}