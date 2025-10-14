using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Mediator.Consumer.Interfaces;
using EasyNetQ.Mediator.Extensions;
using EasyNetQ.Mediator.Factories;
using EasyNetQ.Mediator.Message;

namespace EasyNetQ.Mediator.Consumer.Implementations;

public class MessageSubscriber<T>(ISubscriberFactory<T> factory) : IMessageSubscriber<T> where T : BaseMessage
{
    public async Task ConsumeAsync(IMessageSubscriber<T>.OnConsume onConsume, CancellationToken cancellationToken = default)
    {
        var exchange = await factory.AdvancedBus.ExchangeDeclareAsync(factory.Options).ConfigureAwait(false);
        var queue = await factory.AdvancedBus.SubscriberQueueDeclareAsync(factory.Options).ConfigureAwait(false);

        await factory.AdvancedBus.BindAsync(exchange, queue, string.Empty).ConfigureAwait(false);

        var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var subscriber = factory.AdvancedBus.Consume<T>(queue, async (msg, info) =>
        {
            await onConsume(msg.Body).ConfigureAwait(false);
        });

        void Complete()
        {
            subscriber.Dispose();
            completionSource.TrySetResult();
        }

        if (cancellationToken.IsCancellationRequested)
        {
            Complete();
        }

        using var registration = cancellationToken.Register(Complete);
        await completionSource.Task.ConfigureAwait(false);
    }
}
