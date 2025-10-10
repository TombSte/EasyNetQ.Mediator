using EasyNetQ.Mediator.Consumer.Options;
using EasyNetQ.Mediator.Factories;
using EasyNetQ.Mediator.Sender.Options;
using EasyNetQ.Topology;

namespace EasyNetQ.Mediator.Extensions;

public static class AdvancedBusExtensions
{
    public static Task<Queue> QueueDeclareAsync(this IAdvancedBus advancedBus, QueueOptions options)
    {
        return advancedBus.QueueDeclareAsync(options.QueueName, 
            options.Durable, 
            options.Exclusive, 
            options.AutoDelete);
    } 
    
    public static Task<Exchange> ExchangeDeclareAsync(this IAdvancedBus advancedBus, ExchangeOptions options)
    {
        return advancedBus.ExchangeDeclareAsync(options.QueueName, 
            ExchangeType.Fanout, 
            durable: options.Durable, 
            autoDelete: options.AutoDelete);
    } 
    
    public static Task<Queue> SubscriberQueueDeclareAsync(this IAdvancedBus advancedBus, SubscriberOptions options)
    {
        return advancedBus.QueueDeclareAsync(options.SubQueueName, 
            options.Durable, 
            options.Exclusive, 
            options.AutoDelete);
    } 
}