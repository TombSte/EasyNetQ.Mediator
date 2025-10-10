using EasyNetQ.Mediator.Message;
using EasyNetQ.Mediator.Sender.Options;

namespace EasyNetQ.Mediator.Factories;

public interface IExchangeFactory<T> where T : BaseMessage
{
    ExchangeFactory<T> Configure(Action<ExchangeOptions> setOptions);
    public ExchangeOptions Options { get; } 
    public IBus Bus { get; }
    public IAdvancedBus AdvancedBus { get; }
}

public class ExchangeFactory<T>(IBus bus) : IExchangeFactory<T>
    where T : BaseMessage
{
    public IBus Bus { get; } = bus;
    public IAdvancedBus AdvancedBus { get; } = bus.Advanced;

    public ExchangeOptions Options { get; private set; } = new()
    {
        QueueName = Helper.DefaultExchangeName<T>()
    };

    public ExchangeFactory<T> Configure(Action<ExchangeOptions> setOptions)
    {
        setOptions(Options);
        return this;
    }
}