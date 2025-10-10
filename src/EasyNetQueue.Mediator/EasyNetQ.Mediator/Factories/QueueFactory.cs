using EasyNetQ.Mediator.Message;

namespace EasyNetQ.Mediator.Factories;

public interface IQueueFactory<T> where T : BaseMessage
{
    IQueueFactory<T> Configure(Action<QueueOptions> setOptions);
    IBus Bus { get; }
    IAdvancedBus AdvancedBus { get; }
    QueueOptions Options { get; }
}

public class QueueFactory<T>(IBus bus) : IQueueFactory<T>
    where T : BaseMessage
{
    public IBus Bus { get; } = bus;
    public IAdvancedBus AdvancedBus { get; } = bus.Advanced;

    public QueueOptions Options { get; private set; } = new()
    {
        QueueName = Helper.DefaultQueueName<T>()
    };

    public IQueueFactory<T> Configure(Action<QueueOptions> setOptions)
    {
        setOptions(Options);
        return this;
    }
}