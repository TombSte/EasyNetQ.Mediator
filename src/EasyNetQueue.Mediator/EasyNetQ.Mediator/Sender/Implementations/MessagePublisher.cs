using EasyNetQ.Mediator.Extensions;
using EasyNetQ.Mediator.Factories;
using EasyNetQ.Mediator.Message;
using EasyNetQ.Mediator.Sender.Interfaces;
using EasyNetQ.Topology;

namespace EasyNetQ.Mediator.Sender.Implementations;

public class MessagePublisher<T>(ExchangeFactory<T> factory) : IMessagePublisher<T> where T : BaseMessage
{
    private IAdvancedBus AdvancedBus => factory.AdvancedBus;
    private string QueueName => factory.Options.QueueName;
    
    
    public IMessagePublisher<T> Configure(Action<IExchangeFactory<T>> configure)
    {
        configure(factory);
        return this;
    }

    public async Task PublishAsync(T message)
    {
        var exchange = await AdvancedBus
            .ExchangeDeclareAsync(factory.Options);

        var rabbitMessage = new Message<T>(message);

        // Pubblico sull'exchange (routingKey usata solo se non è fanout)
        await AdvancedBus.PublishAsync(exchange, routingKey: "", mandatory: true, rabbitMessage);
    }
}