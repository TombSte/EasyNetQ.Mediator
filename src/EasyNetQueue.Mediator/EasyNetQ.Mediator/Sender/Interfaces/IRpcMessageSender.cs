using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Mediator.Factories;
using EasyNetQ.Mediator.Message;

namespace EasyNetQ.Mediator.Sender.Interfaces;

public interface IRpcMessageSender<TRequest, TResponse>
    where TRequest : BaseMessage
    where TResponse : BaseMessage
{
    IRpcMessageSender<TRequest, TResponse> Configure(Action<IRpcFactory<TRequest, TResponse>> configure);
    Task<TResponse> RequestAsync(TRequest message, CancellationToken cancellationToken = default);
}
