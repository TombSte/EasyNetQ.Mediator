using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ;
using EasyNetQ.Mediator.Factories;
using EasyNetQ.Mediator.Message;
using EasyNetQ.Mediator.Sender.Interfaces;

namespace EasyNetQ.Mediator.Sender.Implementations;

public class RpcMessageSender<TRequest, TResponse>(
    IRpcFactory<TRequest, TResponse> factory)
    : IRpcMessageSender<TRequest, TResponse>
    where TRequest : BaseMessage
    where TResponse : BaseMessage
{
    public IRpcMessageSender<TRequest, TResponse> Configure(Action<IRpcFactory<TRequest, TResponse>> configure)
    {
        configure(factory);
        return this;
    }

    public Task<TResponse> RequestAsync(TRequest message, CancellationToken cancellationToken = default)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(factory.Options.RequestTimeout);

        return factory.Bus.Rpc.RequestAsync<TRequest, TResponse>(
            message,
            configuration =>
            {
                configuration.WithQueueName(factory.Options.QueueName);
            },
            timeoutCts.Token);
    }
}
