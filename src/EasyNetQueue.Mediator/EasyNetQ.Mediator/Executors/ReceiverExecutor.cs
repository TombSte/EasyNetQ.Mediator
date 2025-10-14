using EasyNetQ.Mediator.Consumer.Implementations;
using EasyNetQ.Mediator.Consumer.Interfaces;
using EasyNetQ.Mediator.Mapping;
using EasyNetQ.Mediator.Message;
using EasyNetQ.Mediator.Registrations;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Mediator.Executors;

public class ReceiverExecutor<TMessage, TCommand>(
    IMessageReceiver<TMessage> receiver,
    ISender sender,
    IMessageMapper mapper,
    IServiceProvider serviceProvider) where TMessage : BaseMessage
{
    public Task Execute()
    {
        return receiver.ReceiveAsync(async message =>
        {
            using var scope = serviceProvider.CreateScope();
            var command = mapper.Map<TMessage, TCommand>(message);

            if (command is null) throw new ArgumentNullException(nameof(command), "Cannot map message");

            await sender.Send(command);
        });
        
    }
}