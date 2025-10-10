using EasyNetQ.Mediator.Factories;
using EasyNetQ.Mediator.Message;

namespace EasyNetQ.Mediator.Sender.Interfaces;

public interface IMessageSender<T>  where T : BaseMessage
{
    public IMessageSender<T> Configure(Action<IQueueFactory<T>> configure);
    public Task SendAsync(T message);
}