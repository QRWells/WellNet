using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Text;
using QRWells.WellNet.Core.Buffer;
using QRWells.WellNet.Core.Pipeline.Processor;

namespace QRWells.WellNet.Core.Pipeline;

public sealed class Pipeline
{
    private readonly IBufferPool _bufferPool = IBufferPool.Default;

    private readonly CancellationTokenSource _inboundCancel = new();

    private readonly PipeReader _inboundReader;
    private readonly CancellationTokenSource _outboundCancel = new();

    private readonly AutoResetEvent _outboundEvent = new(false);

    private readonly ConcurrentQueue<IByteBuffer> _outboundQueue = new();
    private readonly PipeWriter _outboundWriter;
    internal readonly Dictionary<Type, EncoderContext> ByteMessageEncoders = new();

    private PipelineContext? _head;
    private PipelineContext? _tail;
    internal DecoderContext ByteMessageDecoder;

    internal Pipeline(PipeReader inbound, PipeWriter outbound)
    {
        _inboundReader = inbound;
        _outboundWriter = outbound;
    }

    internal void Start()
    {
        Task.Run(ProcessInboundAsync, _inboundCancel.Token);
        Task.Run(ProcessOutboundAsync, _outboundCancel.Token);
    }

    public void Write(object message)
    {
        if (message is IByteBuffer byteBuffer)
        {
            _outboundQueue.Enqueue(byteBuffer);
            return;
        }

        var type = message.GetType();
        if (!ByteMessageEncoders.TryGetValue(type, out var encoder))
            throw new Exception("No encoder found for type " + type);

        var buffer = _bufferPool.Rent();
        encoder.TryEncode(message, buffer);
        _outboundQueue.Enqueue(buffer);
    }

    private void Flush()
    {
        _outboundEvent.Set();
    }

    public void WriteAndFlush(object message)
    {
        Write(message);
        Flush();
    }

    public IByteBuffer AllocateBuffer()
    {
        return _bufferPool.Rent();
    }

    private async Task ProcessInboundAsync()
    {
        while (true)
        {
            var result = await _inboundReader.ReadAsync();
            var buffer = result.Buffer;
            if (result.IsCanceled || result.IsCompleted) break;

            var decoded = ByteMessageDecoder.TryDecode(buffer, out var message, out var consumed);
            // maybe partial consumed, so we need to advance to the consumed position
            _inboundReader.AdvanceTo(buffer.GetPosition(consumed));

            if (!decoded) continue;

            if (_head == null) // there is no message processor
            {
                WriteAndFlush(message!); // write to outbound directly
                continue;
            }

            _head.NextInbound(message!);
        }

        await _inboundReader.CompleteAsync();
    }

    private async Task ProcessOutboundAsync()
    {
        var error = false;
        while (true)
        {
            if (_outboundQueue.IsEmpty) _outboundEvent.WaitOne();

            while (_outboundQueue.TryDequeue(out var buffer))
            {
                var res = await _outboundWriter.WriteAsync(buffer.ReadableMemory);
                if (res.IsCanceled || res.IsCompleted)
                {
                    error = true;
                    break;
                }

                buffer.Dispose();
            }

            if (error) break;
        }

        await _outboundWriter.CompleteAsync();
    }

    internal void AddFirst(IProcessor processor)
    {
        var context = new DefaultPipelineContext(processor);
        if (_head == null)
        {
            _head = context;
            _tail = context;
        }
        else
        {
            _head!.Previous = context;
            context.Next = _head;
            _head = context;
        }
    }

    internal void AddLast(IProcessor processor)
    {
        if (processor is not IInboundProcessor && processor is not IOutboundProcessor) return;
        var context = new DefaultPipelineContext(processor);
        if (_head == null)
        {
            _head = context;
            _tail = context;
        }
        else
        {
            _tail!.Next = context;
            context.Previous = _tail;
            _tail = context;
        }
    }

    public override string ToString()
    {
        var index = 0;
        var node = _head;
        var sb = new StringBuilder();
        while (node != null)
        {
            sb.Append(index);
            sb.Append(": ");
            sb.AppendLine(node.ToString());
            node = node.Next;
            index++;
        }

        return sb.ToString();
    }

    internal void Close()
    {
        _inboundCancel.Cancel();
        _outboundCancel.Cancel();
    }
}