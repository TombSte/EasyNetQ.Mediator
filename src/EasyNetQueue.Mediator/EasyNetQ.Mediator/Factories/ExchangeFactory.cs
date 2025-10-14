using EasyNetQ.Mediator.Message;
using EasyNetQ.Mediator.Sender.Options;

namespace EasyNetQ.Mediator.Factories;

public interface IExchangeFactory<T> : IFactory where T : BaseMessage
{
    public ExchangeOptions Options { get; } 
    public IBus Bus { get; }
    public IAdvancedBus AdvancedBus { get; }
}

public class ExchangeFactory<T> : IExchangeFactory<T>
    where T : BaseMessage
{
    public IBus Bus { get; }

    public IAdvancedBus AdvancedBus => Bus.Advanced;

    public ExchangeFactory(IBus bus, ExchangeOptions options)
    {
        Bus = bus;
        Options = options;
        options.QueueName ??= Helper.DefaultExchangeName<T>();
    }

    public ExchangeOptions Options { get; private set; }
}