using System.Reflection;
using EasyNetQ.Mediator.Consumer.Options;
using EasyNetQ.Mediator.Message;

namespace EasyNetQ.Mediator.Factories;

public interface ISubscriberFactory<T> where T : BaseMessage
{
    ISubscriberFactory<T> Configure(Action<SubscriberOptions> setOptions);
    public SubscriberOptions Options { get; } 
    public IBus Bus { get; }
    public IAdvancedBus AdvancedBus { get; }
}

public class SubscriberFactory<T>(IBus bus) : ISubscriberFactory<T>
    where T : BaseMessage
{
    public ISubscriberFactory<T> Configure(Action<SubscriberOptions> setOptions)
    {
        setOptions(Options);
        return this;
    }

    public SubscriberOptions Options { get; } = new SubscriberOptions
    {
        QueueName = Helper.DefaultExchangeName<T>(),
        SubQueueName = Helper.DefaultSubQueueName<T>() + "-" + Assembly.GetEntryAssembly()?.GetName().Name
    };

    public IBus Bus { get; } = bus;
    public IAdvancedBus AdvancedBus { get; } = bus.Advanced;
}