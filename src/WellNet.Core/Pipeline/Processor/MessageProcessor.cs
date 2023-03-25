using QRWells.WellNet.Core.Pipeline.Processor.Codec;

namespace QRWells.WellNet.Core.Pipeline.Processor;

public abstract class MessageProcessor<TIn, TOut> : IMessageCodec<TIn, TOut>
{
    public void ReadInbound(PipelineContext ctx, object message)
    {
        if (message is TIn inbound)
        {
            ProcessInbound(ctx, inbound);
        }
        else
        {
            ctx.NextInbound(message);
        }
    }

    public void WriteOutbound(PipelineContext ctx, object message)
    {
        if (message is TOut outbound)
        {
            ProcessOutbound(ctx, outbound);
        }
        else
        {
            ctx.NextOutbound(message);
        }
    }

    protected abstract void ProcessInbound(PipelineContext ctx, TIn message);
    protected abstract void ProcessOutbound(PipelineContext ctx, TOut message);
}