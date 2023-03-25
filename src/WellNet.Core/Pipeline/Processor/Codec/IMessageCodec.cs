namespace QRWells.WellNet.Core.Pipeline.Processor.Codec;

public interface IMessageEncoder<in TIn, in TOut> : IOutboundProcessor
{
}

public interface IMessageDecoder<in TIn, in TOut> : IInboundProcessor
{
}

/// <summary>
///     A two-way message codec. It will decode the inbound message and encode the outbound message.
/// </summary>
/// <typeparam name="TIn">Type of the input message</typeparam>
/// <typeparam name="TOut">Type of the output message</typeparam>
/// <remarks>
///     The positions of the two type parameters of the encoder and decoder are opposite.
/// </remarks>
public interface IMessageCodec<in TIn, in TOut> :
    IMessageEncoder<TOut, TIn>,
    IMessageDecoder<TIn, TOut>,
    IDuplexProcessor
{
}