namespace QRWells.WellNet.Core.Pipeline.Processor;

public interface IProcessor
{
}

/// <summary>
///     A one-way message processor that processes inbound messages.
/// </summary>
public interface IInboundProcessor : IProcessor
{
    public void ReadInbound(PipelineContext ctx, object message);
}

/// <summary>
///     A one-way message processor that processes outbound messages.
/// </summary>
public interface IOutboundProcessor : IProcessor
{
    public void WriteOutbound(PipelineContext ctx, object message);
}

/// <summary>
///     A two-way message processor that processes both inbound and outbound messages.
/// </summary>
public interface IDuplexProcessor : IInboundProcessor, IOutboundProcessor
{
}