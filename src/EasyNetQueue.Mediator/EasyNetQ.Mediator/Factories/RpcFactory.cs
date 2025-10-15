using EasyNetQ.Mediator.Consumer.Options;
using EasyNetQ.Mediator.Message;

namespace EasyNetQ.Mediator.Factories;

public interface IRpcFactory<TRequest, TResponse> : IFactory
    where TRequest : BaseMessage
    where TResponse : BaseMessage
{
    RpcOptions Options { get; }
    IBus Bus { get; }
}

public class RpcFactory<TRequest, TResponse> : IRpcFactory<TRequest, TResponse>
    where TRequest : BaseMessage
    where TResponse : BaseMessage
{
    public RpcOptions Options { get; }
    public IBus Bus { get; }

    public RpcFactory(IBus bus, RpcOptions options)
    {
        Bus = bus;
        Options = options;
        Options.QueueName ??= Helper.DefaultRpcQueueName<TRequest>();
        Options.RequestMessageType ??= typeof(TRequest);
        Options.ResponseMessageType ??= typeof(TResponse);
    }
}
