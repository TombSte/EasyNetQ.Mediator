using EasyNetQ.Mediator.Factories;
using EasyNetQ.Mediator.Message;

namespace EasyNetQ.Mediator.Sender.Interfaces;

public interface IMessagePublisher<T> where T : BaseMessage
{
    public Task PublishAsync(T message);
    public IMessagePublisher<T> Configure(Action<IExchangeFactory<T>> configure);
}