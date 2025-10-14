using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Mediator.Consumer.Interfaces;
using EasyNetQ.Mediator.Extensions;
using EasyNetQ.Mediator.Factories;
using EasyNetQ.Mediator.Message;
using Microsoft.Extensions.Logging;

namespace EasyNetQ.Mediator.Consumer.Implementations;


public class MessageReceiver<T>(IQueueFactory<T> factory, ILogger<MessageReceiver<T>> logger) : IMessageReceiver<T>
    where T : BaseMessage
{
    public async Task ReceiveAsync(IMessageReceiver<T>.OnReceive onReceive, CancellationToken cancellationToken = default)
    {
        var queue = await factory.AdvancedBus.QueueDeclareAsync(factory.Options).ConfigureAwait(false);
        var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var consumer = factory.AdvancedBus.Consume<T>(queue, async (msg, info) =>
        {
            try
            {
                await onReceive(msg.Body).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while processing message {MessageType}", typeof(T).Name);
                throw;
            }
        });

        void Complete()
        {
            consumer.Dispose();
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
