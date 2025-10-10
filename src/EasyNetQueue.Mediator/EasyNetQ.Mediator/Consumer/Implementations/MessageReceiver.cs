using EasyNetQ.Mediator.Consumer.Interfaces;
using EasyNetQ.Mediator.Extensions;
using EasyNetQ.Mediator.Factories;
using EasyNetQ.Mediator.Message;
using Microsoft.Extensions.Logging;

namespace EasyNetQ.Mediator.Consumer.Implementations;


public class MessageReceiver<T>(IQueueFactory<T> factory, ILogger<MessageReceiver<T>> logger) : IMessageReceiver<T> where T : BaseMessage
{
    public async Task ReceiveAsync(IMessageReceiver<T>.OnReceive onReceive)
    {
        var queue = await factory.AdvancedBus.QueueDeclareAsync(factory.Options);

        factory.AdvancedBus.Consume<T>(queue, (msg, info) =>
        {
            onReceive(msg.Body);
            return Task.CompletedTask;
        });
    }
}