using EasyNetQ.Mediator.Message;

namespace EasyNetQ.Mediator.Factories;

public interface IQueueFactory<T> : IFactory  where T : BaseMessage
{
    IBus Bus { get; }
    IAdvancedBus AdvancedBus { get; }
    QueueOptions Options { get; }
}

public class QueueFactory<T> : IQueueFactory<T>
    where T : BaseMessage
{
    public IBus Bus { get; }
    public IAdvancedBus AdvancedBus { get; }
    public QueueOptions Options { get; }
    
    public QueueFactory(IBus bus, QueueOptions options)
    {
        Bus = bus;
        AdvancedBus = bus.Advanced;
        Options = options;
        Options.QueueName ??= Helper.DefaultQueueName<T>();
    }
}