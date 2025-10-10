using EasyNetQ.Mediator.Extensions;
using EasyNetQ.Mediator.Factories;
using EasyNetQ.Mediator.Message;
using EasyNetQ.Mediator.Sender.Interfaces;
using EasyNetQ.Topology;

namespace EasyNetQ.Mediator.Sender.Implementations;

public class MessageSender<T>(IQueueFactory<T> factory) : IMessageSender<T> where T : BaseMessage
{
    private IAdvancedBus AdvancedBus => factory.AdvancedBus;

    public IMessageSender<T> Configure(Action<IQueueFactory<T>> configure)
    {
        configure(factory);
        return this;
    }

    public async Task SendAsync(T message)
    {
        var exchange = Exchange.Default;
        var queue = await AdvancedBus.QueueDeclareAsync(factory.Options);

        var rabbitMessage = new Message<T>(message);
        await AdvancedBus.PublishAsync(exchange, queue.Name, mandatory: true, rabbitMessage);
    }
}