using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ;
using EasyNetQ.Mediator.Consumer.Interfaces;
using EasyNetQ.Mediator.Factories;
using EasyNetQ.Mediator.Message;

namespace EasyNetQ.Mediator.Consumer.Implementations;

public class MessageResponder<TRequest, TResponse>(
    IRpcFactory<TRequest, TResponse> factory)
    : IMessageResponder<TRequest, TResponse>
    where TRequest : BaseMessage
    where TResponse : BaseMessage
{
    public async Task RespondAsync(
        IMessageResponder<TRequest, TResponse>.OnRespond onRespond,
        CancellationToken cancellationToken = default)
    {
        var responder = await factory.Bus.Rpc.RespondAsync<TRequest, TResponse>(
            (request, token) => onRespond(request),
            Configure,
            cancellationToken).ConfigureAwait(false);

        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // expected when cancellation requested
        }
        finally
        {
            responder.Dispose();
        }
    }

    private void Configure(IResponderConfiguration configuration)
    {
        configuration.WithQueueName(factory.Options.QueueName);
        if (factory.Options.PrefetchCount.HasValue)
        {
            configuration.WithPrefetchCount(factory.Options.PrefetchCount.Value);
        }
    }
}
