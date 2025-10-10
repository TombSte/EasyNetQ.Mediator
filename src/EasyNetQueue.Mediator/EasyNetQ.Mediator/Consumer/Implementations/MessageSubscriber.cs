using EasyNetQ.Mediator.Consumer.Interfaces;
using EasyNetQ.Mediator.Extensions;
using EasyNetQ.Mediator.Factories;
using EasyNetQ.Mediator.Message;

namespace EasyNetQ.Mediator.Consumer.Implementations;

public class MessageSubscriber<T>(ISubscriberFactory<T> factory) : IMessageSubscriber<T>  where T : BaseMessage
{
    public async Task ConsumeAsync(IMessageSubscriber<T>.OnConsume onConsume)
    {
        var exchange = await factory.AdvancedBus.ExchangeDeclareAsync(factory.Options);
        
        var queue = await factory.AdvancedBus.SubscriberQueueDeclareAsync(factory.Options);

        await factory.AdvancedBus.BindAsync(exchange, queue, string.Empty);
        
        factory.AdvancedBus.Consume<T>(queue, async (msg, info) =>
        {
            await onConsume(msg.Body);
        });
    }
}