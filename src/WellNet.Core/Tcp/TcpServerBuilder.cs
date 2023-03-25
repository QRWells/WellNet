using System.Net;
using QRWells.WellNet.Core.Pipeline;

namespace QRWells.WellNet.Core.Tcp;

public sealed class TcpServerBuilder
{
    private EndPoint _endPoint;
    private Action<PipelineBuilder> _pipelineBuilder;

    public TcpServerBuilder(EndPoint endPoint)
    {
        _endPoint = endPoint;
    }

    public TcpServerBuilder()
    {
    }

    public TcpServerBuilder WithEndPoint(EndPoint endPoint)
    {
        _endPoint = endPoint;
        return this;
    }

    public TcpServerBuilder WithPort(int port)
    {
        _endPoint = new IPEndPoint(IPAddress.Any, port);
        return this;
    }

    public TcpServer Build()
    {
        return new TcpServer(_endPoint, _pipelineBuilder);
    }

    public TcpServerBuilder ConfigurePipeline(Action<PipelineBuilder> configure)
    {
        _pipelineBuilder = configure;
        return this;
    }
}