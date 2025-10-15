using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Mediator.Executors;
using EasyNetQ.Mediator.Factories;
using EasyNetQ.Mediator.Mapping;
using EasyNetQ.Mediator.Registrations;
using EasyNetQ.Mediator.Sender.Implementations;
using EasyNetQ.Mediator.Sender.Options;
using EasyNetQ.Mediator.Test.Helpers;
using EasyNetQ.Mediator.Test.Integrations;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace EasyNetQ.Mediator.Test.Executors;

public class RabbitMediatorExecutorLauncherTest : IClassFixture<IntegrationTestFixture>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IntegrationTestFixture _fixture;
    private readonly IMessageMapper _mapper;
    private readonly ITestDependency _dependency;

    public RabbitMediatorExecutorLauncherTest(IntegrationTestFixture fixture)
    {
        IServiceCollection serviceCollection = new ServiceCollection();
        _fixture = fixture;
        _mapper = Substitute.For<IMessageMapper>();
        _dependency = Substitute.For<ITestDependency>();

        serviceCollection.AddEasyNetQMediator();
        serviceCollection.AddImplicitDependencies(fixture, mapper: _mapper);
        serviceCollection.AddSingleton<ITestDependency>(_dependency);
        _serviceProvider = serviceCollection.BuildServiceProvider();
    }

    [Fact]
    public async Task Should_Process_Single_Message()
    {
        var queueName = $"launcher-single-{Guid.NewGuid():N}";
        var builder = new ReceiverRegistrationBuilder();

        var mapped = new TaskCompletionSource<TestMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var dependencySignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        _mapper.Map<TestMessage, TestCommand>(Arg.Any<TestMessage>()).Returns(ci =>
        {
            var message = ci.Arg<TestMessage>();
            mapped.TrySetResult(message);
            return new TestCommand(message.Id);
        });

        _dependency.When(x => x.DoStuff()).Do(_ => dependencySignal.TrySetResult());

        builder.Register()
            .OnCommand<TestCommand>()
            .OnMessage<TestMessage>()
            .WithOptions(options =>
            {
                options.QueueName = queueName;
                options.AutoDelete = true;
                options.Durable = false;
            });

        var launcher = new RabbitMediatorExecutorLauncher(
            builder,
            _serviceProvider,
            new SubscriberRegistrationBuilder());

        using var listenerCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var runTask = launcher.Run(listenerCts.Token);

        var queueOptions = new QueueOptions
        {
            QueueName = queueName,
            AutoDelete = true,
            Durable = false
        };

        var factory = new QueueFactory<TestMessage>(_fixture.Bus, queueOptions);
        var sender = new MessageSender<TestMessage>(factory);

        await sender.SendAsync(new TestMessage { Id = 1 });

        var processingTask = Task.WhenAll(mapped.Task, dependencySignal.Task);
        var completed = await Task.WhenAny(processingTask, Task.Delay(TimeSpan.FromSeconds(30)));
        if (completed != processingTask)
        {
            throw new TimeoutException("Message was not processed");
        }

        _dependency.Received(1).DoStuff();

        await listenerCts.CancelAsync();
        await runTask;
    }

    [Fact]
    public async Task Should_Process_Single_Subscription_Message()
    {
        var exchangeName = $"launcher-sub-single-{Guid.NewGuid():N}";
        var receiverBuilder = new ReceiverRegistrationBuilder();
        var subscriberBuilder = new SubscriberRegistrationBuilder();

        var mapped = new TaskCompletionSource<TestMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var dependencySignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        _mapper.Map<TestMessage, TestCommand>(Arg.Any<TestMessage>()).Returns(ci =>
        {
            var message = ci.Arg<TestMessage>();
            mapped.TrySetResult(message);
            return new TestCommand(message.Id);
        });

        _dependency.When(x => x.DoStuff()).Do(_ => dependencySignal.TrySetResult());

        subscriberBuilder.Subscribe()
            .OnCommand<TestCommand>()
            .OnMessage<TestMessage>()
            .WithOptions(options =>
            {
                options.QueueName = exchangeName;
                options.AutoDelete = true;
                options.Durable = false;
                options.SubQueueName = $"{exchangeName}-sub";
            });

        var launcher = new RabbitMediatorExecutorLauncher(
            receiverBuilder,
            _serviceProvider,
            subscriberBuilder);

        using var listenerCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var runTask = launcher.Run(listenerCts.Token);

        var exchangeOptions = new ExchangeOptions
        {
            QueueName = exchangeName,
            AutoDelete = true,
            Durable = false
        };

        var factory = new ExchangeFactory<TestMessage>(_fixture.Bus, exchangeOptions);
        var publisher = new MessagePublisher<TestMessage>(factory);

        await publisher.PublishAsync(new TestMessage { Id = 1 });

        var processingTask = Task.WhenAll(mapped.Task, dependencySignal.Task);
        var completed = await Task.WhenAny(processingTask, Task.Delay(TimeSpan.FromSeconds(30)));
        if (completed != processingTask)
        {
            throw new TimeoutException("Subscription message was not processed");
        }

        _dependency.Received(1).DoStuff();

        await listenerCts.CancelAsync();
        await runTask;
    }

    [Fact]
    public async Task Run_Should_Remain_Pending_Until_Cancelled()
    {
        var queueName = $"launcher-pending-{Guid.NewGuid():N}";
        var builder = new ReceiverRegistrationBuilder();

        _mapper.Map<TestMessage, TestCommand>(Arg.Any<TestMessage>())
            .Returns(ci => new TestCommand(ci.Arg<TestMessage>().Id));

        builder.Register()
            .OnCommand<TestCommand>()
            .OnMessage<TestMessage>()
            .WithOptions(options =>
            {
                options.QueueName = queueName;
                options.AutoDelete = true;
                options.Durable = false;
            });

        var launcher = new RabbitMediatorExecutorLauncher(
            builder,
            _serviceProvider,
            new SubscriberRegistrationBuilder());

        using var runCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var runTask = launcher.Run(runCts.Token);

        var completed = await Task.WhenAny(runTask, Task.Delay(TimeSpan.FromSeconds(1)));
        Assert.NotEqual(runTask, completed);

        runCts.Cancel();
        await runTask;
    }

    [Fact]
    public async Task Subscription_Run_Should_Remain_Pending_Until_Cancelled()
    {
        var exchangeName = $"launcher-sub-pending-{Guid.NewGuid():N}";
        var receiverBuilder = new ReceiverRegistrationBuilder();
        var subscriberBuilder = new SubscriberRegistrationBuilder();

        _mapper.Map<TestMessage, TestCommand>(Arg.Any<TestMessage>())
            .Returns(ci => new TestCommand(ci.Arg<TestMessage>().Id));

        subscriberBuilder.Subscribe()
            .OnCommand<TestCommand>()
            .OnMessage<TestMessage>()
            .WithOptions(options =>
            {
                options.QueueName = exchangeName;
                options.AutoDelete = true;
                options.Durable = false;
                options.SubQueueName = $"{exchangeName}-sub";
            });

        var launcher = new RabbitMediatorExecutorLauncher(
            receiverBuilder,
            _serviceProvider,
            subscriberBuilder);

        using var runCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var runTask = launcher.Run(runCts.Token);

        var completed = await Task.WhenAny(runTask, Task.Delay(TimeSpan.FromSeconds(1)));
        Assert.NotEqual(runTask, completed);

        runCts.Cancel();
        await runTask;
    }

    [Fact]
    public async Task Should_Process_Multiple_Messages_With_Delays()
    {
        var queueName = $"launcher-multi-{Guid.NewGuid():N}";
        var builder = new ReceiverRegistrationBuilder();

        const int totalMessages = 4;
        var dependencyCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var processed = 0;

        _mapper.Map<TestMessage, TestCommand>(Arg.Any<TestMessage>()).Returns(ci =>
        {
            var message = ci.Arg<TestMessage>();
            return new TestCommand(message.Id);
        });

        _dependency.When(x => x.DoStuff()).Do(_ =>
        {
            if (Interlocked.Increment(ref processed) == totalMessages)
            {
                dependencyCompletion.TrySetResult();
            }
        });

        builder.Register()
            .OnCommand<TestCommand>()
            .OnMessage<TestMessage>()
            .WithOptions(options =>
            {
                options.QueueName = queueName;
                options.AutoDelete = true;
                options.Durable = false;
            });

        var launcher = new RabbitMediatorExecutorLauncher(
            builder,
            _serviceProvider,
            new SubscriberRegistrationBuilder());

        using var listenerCts = new CancellationTokenSource(TimeSpan.FromSeconds(totalMessages * 5));
        var runTask = launcher.Run(listenerCts.Token);

        var queueOptions = new QueueOptions
        {
            QueueName = queueName,
            AutoDelete = true,
            Durable = false
        };

        var factory = new QueueFactory<TestMessage>(_fixture.Bus, queueOptions);
        var sender = new MessageSender<TestMessage>(factory);

        var messages = Enumerable.Range(1, totalMessages)
            .Select(id => new TestMessage { Id = id })
            .ToList();

        foreach (var message in messages)
        {
            await sender.SendAsync(message);
            if (!ReferenceEquals(message, messages[^1]))
            {
                await Task.Delay(TimeSpan.FromSeconds(2), CancellationToken.None);
            }
        }

        var finished = await Task.WhenAny(
            dependencyCompletion.Task,
            Task.Delay(TimeSpan.FromSeconds(totalMessages * 5)));

        if (finished != dependencyCompletion.Task)
        {
            throw new TimeoutException("Not all messages were processed");
        }

        _dependency.Received(totalMessages).DoStuff();

        await listenerCts.CancelAsync();
        await runTask;
    }

    [Fact]
    public async Task Should_Process_Multiple_Subscription_Messages_With_Delays()
    {
        var exchangeName = $"launcher-sub-multi-{Guid.NewGuid():N}";
        var receiverBuilder = new ReceiverRegistrationBuilder();
        var subscriberBuilder = new SubscriberRegistrationBuilder();

        const int totalMessages = 4;
        var dependencyCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var processed = 0;

        _mapper.Map<TestMessage, TestCommand>(Arg.Any<TestMessage>()).Returns(ci =>
        {
            var message = ci.Arg<TestMessage>();
            return new TestCommand(message.Id);
        });

        _dependency.When(x => x.DoStuff()).Do(_ =>
        {
            if (Interlocked.Increment(ref processed) == totalMessages)
            {
                dependencyCompletion.TrySetResult();
            }
        });

        subscriberBuilder.Subscribe()
            .OnCommand<TestCommand>()
            .OnMessage<TestMessage>()
            .WithOptions(options =>
            {
                options.QueueName = exchangeName;
                options.AutoDelete = true;
                options.Durable = false;
                options.SubQueueName = $"{exchangeName}-sub";
            });

        var launcher = new RabbitMediatorExecutorLauncher(
            receiverBuilder,
            _serviceProvider,
            subscriberBuilder);

        using var listenerCts = new CancellationTokenSource(TimeSpan.FromSeconds(totalMessages * 5));
        var runTask = launcher.Run(listenerCts.Token);

        var exchangeOptions = new ExchangeOptions
        {
            QueueName = exchangeName,
            AutoDelete = true,
            Durable = false
        };

        var factory = new ExchangeFactory<TestMessage>(_fixture.Bus, exchangeOptions);
        var publisher = new MessagePublisher<TestMessage>(factory);

        var messages = Enumerable.Range(1, totalMessages)
            .Select(id => new TestMessage { Id = id })
            .ToList();

        foreach (var message in messages)
        {
            await publisher.PublishAsync(message);
            if (!ReferenceEquals(message, messages[^1]))
            {
                await Task.Delay(TimeSpan.FromSeconds(2), CancellationToken.None);
            }
        }

        var finished = await Task.WhenAny(
            dependencyCompletion.Task,
            Task.Delay(TimeSpan.FromSeconds(totalMessages * 5)));

        if (finished != dependencyCompletion.Task)
        {
            throw new TimeoutException("Not all subscription messages were processed");
        }

        _dependency.Received(totalMessages).DoStuff();

        await listenerCts.CancelAsync();
        await runTask;
    }

    [Fact]
    public async Task Run_With_No_Registrations_Completes_Immediately()
    {
        var builder = new ReceiverRegistrationBuilder();
        var launcher = new RabbitMediatorExecutorLauncher(
            builder,
            _serviceProvider,
            new SubscriberRegistrationBuilder());

        var runTask = launcher.Run();

        await runTask;

        Assert.True(runTask.IsCompletedSuccessfully);
    }
}
