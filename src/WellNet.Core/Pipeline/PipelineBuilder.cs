using System.IO.Pipelines;
using QRWells.WellNet.Core.Pipeline.Processor;
using QRWells.WellNet.Core.Pipeline.Processor.Codec;

namespace QRWells.WellNet.Core.Pipeline;

public sealed class PipelineBuilder
{
    private readonly Pipeline _pipeline;

    internal PipelineBuilder(PipeReader inbound, PipeWriter outbound)
    {
        _pipeline = new Pipeline(inbound, outbound);
    }

    public PipelineBuilder WithByteMessageDecoder<T>(ByteMessageDecoder<T> decoder)
    {
        _pipeline.ByteMessageDecoder = new DecoderContext(_pipeline, decoder);
        return this;
    }

    public PipelineBuilder AddByteMessageEncoder<T>(ByteMessageEncoder<T> encoder)
    {
        _pipeline.ByteMessageEncoders.Add(typeof(T), new EncoderContext(_pipeline, encoder));
        return this;
    }

    public PipelineBuilder AddLast(IProcessor processor)
    {
        _pipeline.AddLast(processor);
        return this;
    }

    public PipelineBuilder AddFirst(IProcessor processor)
    {
        _pipeline.AddFirst(processor);
        return this;
    }

    internal Pipeline Build()
    {
        return _pipeline;
    }
}