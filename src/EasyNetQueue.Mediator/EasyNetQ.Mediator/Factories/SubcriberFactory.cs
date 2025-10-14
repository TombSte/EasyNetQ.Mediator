using System.Reflection;
using EasyNetQ.Mediator.Consumer.Options;
using EasyNetQ.Mediator.Message;

namespace EasyNetQ.Mediator.Factories;

public interface ISubscriberFactory<T> : IFactory where T : BaseMessage
{
    public SubscriberOptions Options { get; } 
    public IBus Bus { get; }
    public IAdvancedBus AdvancedBus { get; }
}

public class SubscriberFactory<T> : ISubscriberFactory<T>
    where T : BaseMessage
{ 
    public SubscriberOptions Options { get; }

    public IBus Bus { get; }
    public IAdvancedBus AdvancedBus => Bus.Advanced;

    public SubscriberFactory(IBus bus, SubscriberOptions options)
    {
        Bus = bus;
        Options = options;
        options.QueueName ??= Helper.DefaultExchangeName<T>();
        options.SubQueueName ??= Helper.DefaultSubQueueName<T>() + "-" + Assembly.GetEntryAssembly()?.GetName().Name;
    }
}