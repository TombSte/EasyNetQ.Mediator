using EasyNetQ.Mediator.Consumer.Interfaces;
using EasyNetQ.Mediator.Mapping;
using EasyNetQ.Mediator.Message;
using MediatR;

namespace EasyNetQ.Mediator.Executors;

public class SubscriberExecutor<TMessage, TCommand>(
    IMessageSubscriber<TMessage> receiver,
    ISender sender,
    IMessageMapper mapper) where TMessage : BaseMessage
{
    public Task Execute(CancellationToken cancellationToken)
    {
        return receiver.ConsumeAsync(async message =>
        {
            var command = mapper.Map<TMessage, TCommand>(message);

            if (command is null) throw new ArgumentNullException(nameof(command), "Cannot map message");

            await sender.Send(command, cancellationToken).ConfigureAwait(false);
        }, cancellationToken);
    }
}
