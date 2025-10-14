using EasyNetQ.Mediator.Message;

namespace EasyNetQ.Mediator.Consumer.Interfaces;

public interface IMessageReceiver<T>  where T : BaseMessage
{
    public delegate Task OnReceive(T message);
    public Task ReceiveAsync(OnReceive onReceive);
}