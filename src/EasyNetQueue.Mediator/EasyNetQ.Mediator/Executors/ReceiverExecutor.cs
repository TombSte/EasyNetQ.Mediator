using System;
using System.Threading;
using EasyNetQ.Mediator.Consumer.Interfaces;
using EasyNetQ.Mediator.Mapping;
using EasyNetQ.Mediator.Message;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Mediator.Executors;

public class ReceiverExecutor<TMessage, TCommand>(
    IMessageReceiver<TMessage> receiver,
    ISender sender,
    IMessageMapper mapper,
    IServiceScopeFactory scopeFactory) where TMessage : BaseMessage
{
    public Task Execute(CancellationToken cancellationToken)
    {
        return receiver.ReceiveAsync(async message =>
        {
            using var scope = scopeFactory.CreateScope();
            var command = mapper.Map<TMessage, TCommand>(message);

            if (command is null) throw new ArgumentNullException(nameof(command), "Cannot map message");

            await sender.Send(command, cancellationToken: CancellationToken.None).ConfigureAwait(false);
        }, cancellationToken);
    }
}
