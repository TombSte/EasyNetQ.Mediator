using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Mediator.Consumer.Implementations;
using EasyNetQ.Mediator.Consumer.Options;
using EasyNetQ.Mediator.Factories;
using EasyNetQ.Mediator.Message;
using EasyNetQ.Mediator.Sender.Implementations;
using EasyNetQ.Mediator.Sender.Options;
using FluentAssertions;

namespace EasyNetQ.Mediator.Test.Integrations;

public record UpdatePerson(string FirstName, string LastName) : BaseMessage;

public class PublishSubscribeIntegrationTest(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    [Fact]
    public async Task ShouldSendAndReceiveAMessage()
    {
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var listenerCts = new CancellationTokenSource();

        var queueName = "test-integration-update-person-exchange";
        
        var exchangeOptions = new ExchangeOptions()
        {
            AutoDelete = true,
            QueueName = queueName,
            Durable = false,
        };
        
        var publisherFactory = new ExchangeFactory<UpdatePerson>(fixture.Bus, exchangeOptions);

        SubscriberOptions optionsSub1 = new SubscriberOptions()
        {
            AutoDelete = true,
            SubQueueName = queueName + "-sub1",
            QueueName = queueName,
            Durable = false,
        };
        
        var subscriberFactory = new SubscriberFactory<UpdatePerson>(fixture.Bus, optionsSub1);

        var sender = new MessagePublisher<UpdatePerson>(publisherFactory);
        var subscriber = new MessageSubscriber<UpdatePerson>(subscriberFactory);

        var received = new TaskCompletionSource<UpdatePerson>(TaskCreationOptions.RunContinuationsAsynchronously);

        // 1) Attiva il consumer PRIMA
        var consuming = subscriber.ConsumeAsync(x =>
        {
            received.TrySetResult(x);
            return Task.CompletedTask;
        }, listenerCts.Token);

        try
        {
            // 2) Poi pubblica
            var expected = new UpdatePerson("John" + Guid.NewGuid(), "Doe");
            await sender.PublishAsync(expected);

            // 3) Attendi il messaggio o timeout
            var actual = await Task.WhenAny(
                received.Task,
                Task.Delay(Timeout.Infinite, timeoutCts.Token)) == received.Task
                ? await received.Task
                : throw new TimeoutException("Messaggio non ricevuto entro 30s");

            actual.Should().BeEquivalentTo(expected);
        }
        finally
        {
            listenerCts.Cancel();
        }

        await consuming;
    }
    
    [Fact]
    public async Task ShouldSendAndReceiveAMessage_MultipleSubscribers()
    {
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        
        var queueName = "test-integration-update-person-exchange-multiple";

        var exchangeOptions = new ExchangeOptions()
        {
            AutoDelete = true,
            QueueName = queueName,
            Durable = false,
        };
        
        var publisherFactory = new ExchangeFactory<UpdatePerson>(fixture.Bus, exchangeOptions);

        SubscriberOptions optionsSub1 = new SubscriberOptions()
        {
            AutoDelete = true,
            SubQueueName = queueName + "-sub1",
            QueueName = queueName,
            Durable = false,
        };
        
        var subscriberFactory1 = new SubscriberFactory<UpdatePerson>(fixture.Bus, optionsSub1);
        
        SubscriberOptions optionsSub2 = new SubscriberOptions()
        {
            AutoDelete = true,
            SubQueueName = queueName + "-sub2",
            QueueName = queueName,
            Durable = false,
        };
        
        var subscriberFactory2 = new SubscriberFactory<UpdatePerson>(fixture.Bus, optionsSub2);

        var sender = new MessagePublisher<UpdatePerson>(publisherFactory);
        var subscriber1 = new MessageSubscriber<UpdatePerson>(subscriberFactory1);
        var subscriber2 = new MessageSubscriber<UpdatePerson>(subscriberFactory2);

        var expected = new UpdatePerson("John" + Guid.NewGuid(), "Doe");
        
        var ts1 = CheckSubscriber(subscriber1, timeoutCts.Token, expected);
        var ts2 = CheckSubscriber(subscriber2, timeoutCts.Token, expected);
        
        var tp =  sender.PublishAsync(expected);

        await Task.WhenAll(ts1, ts2, tp);
    }

    private static async Task CheckSubscriber(
        MessageSubscriber<UpdatePerson> subscriber,
        CancellationToken timeoutToken,
        UpdatePerson expected)
    {
        var received = new TaskCompletionSource<UpdatePerson>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var listenerCts = new CancellationTokenSource();

        var consuming = subscriber.ConsumeAsync(x =>
        {
            received.TrySetResult(x);
            return Task.CompletedTask;
        }, listenerCts.Token);

        try
        {
            var actual = await Task.WhenAny(
                received.Task,
                Task.Delay(Timeout.Infinite, timeoutToken)) == received.Task
                ? await received.Task
                : throw new TimeoutException("Messaggio non ricevuto entro 30s");

            actual.Should().BeEquivalentTo(expected);
        }
        finally
        {
            listenerCts.Cancel();
        }

        await consuming;
    }
}
