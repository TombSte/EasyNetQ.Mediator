using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Mediator.Consumer.Implementations;
using EasyNetQ.Mediator.Factories;
using EasyNetQ.Mediator.Message;
using EasyNetQ.Mediator.Sender.Implementations;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace EasyNetQ.Mediator.Test.Integrations;

public record NewPerson(string FirstName, string LastName) : BaseMessage;

public class SenderReceiverIntegrationTest(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    [Fact]
    public void Should_be_able_to_send_a_message()
    {
        
        var queueName = "test-integration-send-new-person";
        var options = new QueueOptions()
        {
            AutoDelete = true,
            QueueName = queueName,
            Durable = false
        };
        
        var factory = new QueueFactory<NewPerson>(fixture.Bus, options);
        
        var messageSender = new MessageSender<NewPerson>(factory);
        
        var p = new NewPerson("John", "Doe");
        
        Action act = () => messageSender.SendAsync(p).GetAwaiter().GetResult();
        act.Should().NotThrow();
    }
    
    [Fact]
    public async Task ShouldSendAndReceiveAMessage()
    {
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var listenerCts = new CancellationTokenSource();

        var queueName = "test-integration-new-person";
        var options = new QueueOptions()
        {
            AutoDelete = true,
            QueueName = queueName,
            Durable = false
        };
        var factory = new QueueFactory<NewPerson>(fixture.Bus, options);

        
        
        var messageSender = new MessageSender<NewPerson>(factory);
        var messageReceiver = new MessageReceiver<NewPerson>(factory, Substitute.For<ILogger<MessageReceiver<NewPerson>>>());
        
        var p = new NewPerson("John" + Guid.NewGuid(), "Doe");

        var received = new TaskCompletionSource<NewPerson>(TaskCreationOptions.RunContinuationsAsynchronously);

        var consuming = messageReceiver.ReceiveAsync( x =>
        {
            received.TrySetResult(x);
            x.Should().BeEquivalentTo(p);
            return Task.CompletedTask;
        }, listenerCts.Token);

        await messageSender.SendAsync(p);

        try
        {
            var completedTask = await Task.WhenAny(
                received.Task,
                Task.Delay(Timeout.Infinite, timeoutCts.Token));

            if (completedTask != received.Task)
            {
                throw new TimeoutException("Messaggio non ricevuto entro 30s");
            }

            await received.Task;
        }
        finally
        {
            listenerCts.Cancel();
        }

        await consuming;
    }

    [Fact]
    public async Task ShouldProcessMultipleMessages_WithDelays()
    {
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        using var listenerCts = new CancellationTokenSource();

        var queueName = $"test-integration-new-person-multi-{Guid.NewGuid()}";
        var options = new QueueOptions
        {
            AutoDelete = true,
            QueueName = queueName,
            Durable = false
        };
        var factory = new QueueFactory<NewPerson>(fixture.Bus, options);

        var messageSender = new MessageSender<NewPerson>(factory);
        var messageReceiver = new MessageReceiver<NewPerson>(
            factory,
            Substitute.For<ILogger<MessageReceiver<NewPerson>>>());

        const int totalMessages = 4;
        var sentMessages = Enumerable.Range(1, totalMessages)
            .Select(i => new NewPerson($"John-{i}", $"Doe-{Guid.NewGuid()}"))
            .ToList();

        var receivedMessages = new List<NewPerson>();
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var syncRoot = new object();

        var consuming = messageReceiver.ReceiveAsync(message =>
        {
            lock (syncRoot)
            {
                receivedMessages.Add(message);
                if (receivedMessages.Count == totalMessages)
                {
                    completion.TrySetResult();
                }
            }

            return Task.CompletedTask;
        }, listenerCts.Token);

        foreach (var message in sentMessages)
        {
            await messageSender.SendAsync(message);
            if (!ReferenceEquals(message, sentMessages[^1]))
            {
                await Task.Delay(TimeSpan.FromSeconds(2), timeoutCts.Token);
            }
        }

        var completedTask = await Task.WhenAny(
            completion.Task,
            Task.Delay(Timeout.Infinite, timeoutCts.Token));

        if (completedTask != completion.Task)
        {
            throw new TimeoutException("Non tutti i messaggi sono stati elaborati in tempo");
        }

        listenerCts.Cancel();
        await consuming;

        receivedMessages.Should()
            .HaveCount(totalMessages)
            .And.BeEquivalentTo(sentMessages, opts => opts.WithStrictOrdering());
    }
}
