using EasyNetQ.Mediator.Message;

namespace EasyNetQ.Mediator.Consumer.Interfaces;

public interface IMessageSubscriber<T> where T : BaseMessage
{
    public delegate Task OnConsume(T message);
    public Task ConsumeAsync(OnConsume onConsume);
}