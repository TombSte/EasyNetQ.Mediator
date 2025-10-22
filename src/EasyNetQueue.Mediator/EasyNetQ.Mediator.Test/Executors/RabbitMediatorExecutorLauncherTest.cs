using EasyNetQ.Mediator.Consumer.Options;
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
        var builder = new MockedReceiverRegistrationBuilder(queueName);

        var mapped = new TaskCompletionSource<TestMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var dependencySignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        _mapper.Map<TestMessage, TestCommand>(Arg.Any<TestMessage>()).Returns(ci =>
        {
            var message = ci.Arg<TestMessage>();
            mapped.TrySetResult(message);
            return new TestCommand(message.Id);
        });

        _dependency.When(x => x.DoStuff()).Do(_ => dependencySignal.TrySetResult());

        var launcher = new RabbitMediatorExecutorLauncher(
            _serviceProvider,
            [builder],
            [new MockedSubscriberRegistrationBuilder(register: false)],
            [new MockedRpcRegistrationBuilder(register: false)]);

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
        var completed = await Task.WhenAny(processingTask, Task.Delay(TimeSpan.FromSeconds(30), listenerCts.Token));
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
        var receiverBuilder = new MockedReceiverRegistrationBuilder(register: false);
        var subscriberBuilder = new MockedSubscriberRegistrationBuilder(exchangeName);
        var rpcBuilder = new MockedRpcRegistrationBuilder(register: false);

        var mapped = new TaskCompletionSource<TestMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var dependencySignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        _mapper.Map<TestMessage, TestCommand>(Arg.Any<TestMessage>()).Returns(ci =>
        {
            var message = ci.Arg<TestMessage>();
            mapped.TrySetResult(message);
            return new TestCommand(message.Id);
        });

        _dependency.When(x => x.DoStuff()).Do(_ => dependencySignal.TrySetResult());

        var launcher = new RabbitMediatorExecutorLauncher(
            _serviceProvider,
            [receiverBuilder],
            [subscriberBuilder],
            [rpcBuilder]);

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
    public async Task Should_Process_Single_Rpc_Request()
    {
        var rpcQueue = $"launcher-rpc-single-{Guid.NewGuid():N}";
        var receiverBuilder = new MockedReceiverRegistrationBuilder(register: false);
        var subscriberBuilder = new MockedSubscriberRegistrationBuilder(register: false);
        var rpcBuilder = new MockedRpcRegistrationBuilder(rpcQueue);

        _mapper.Map<TestMessage, TestCommandWithResult>(Arg.Any<TestMessage>())
            .Returns(ci =>
            {
                var message = ci.Arg<TestMessage>();
                return new TestCommandWithResult(message.Id);
            });

        _mapper.Map<TestCommandResult, TestResponseMessage>(Arg.Any<TestCommandResult>())
            .Returns(ci =>
            {
                var result = ci.Arg<TestCommandResult>();
                return new TestResponseMessage { ResultId = result.ResultId };
            });

        var launcher = new RabbitMediatorExecutorLauncher(
            _serviceProvider,
            [receiverBuilder],
            [subscriberBuilder],
            [rpcBuilder]);

        using var listenerCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var runTask = launcher.Run(listenerCts.Token);

        var rpcOptions = new RpcOptions
        {
            QueueName = rpcQueue,
            AutoDelete = true,
            Durable = false
        };

        var factory = new RpcFactory<TestMessage, TestResponseMessage>(_fixture.Bus, rpcOptions);
        var sender = new RpcMessageSender<TestMessage, TestResponseMessage>(factory);

        var response = await sender.RequestAsync(new TestMessage { Id = 42 }, listenerCts.Token);

        Assert.NotNull(response);
        Assert.Equal(10, response.ResultId);

        await listenerCts.CancelAsync();
        await runTask;
    }

    [Fact]
    public async Task Run_Should_Remain_Pending_Until_Cancelled()
    {
        var queueName = $"launcher-pending-{Guid.NewGuid():N}";
        var builder = new MockedReceiverRegistrationBuilder(queueName);

        _mapper.Map<TestMessage, TestCommand>(Arg.Any<TestMessage>())
            .Returns(ci => new TestCommand(ci.Arg<TestMessage>().Id));

        var launcher = new RabbitMediatorExecutorLauncher(
            _serviceProvider,
            [builder],
            [new MockedSubscriberRegistrationBuilder(register: false)],
            [new MockedRpcRegistrationBuilder(register: false)]);

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
        var receiverBuilder = new MockedReceiverRegistrationBuilder(register: false);
        var subscriberBuilder = new MockedSubscriberRegistrationBuilder(exchangeName);
        var rpcBuilder = new MockedRpcRegistrationBuilder(register: false);

        _mapper.Map<TestMessage, TestCommand>(Arg.Any<TestMessage>())
            .Returns(ci => new TestCommand(ci.Arg<TestMessage>().Id));

        var launcher = new RabbitMediatorExecutorLauncher(
            _serviceProvider,
            [receiverBuilder],
            [subscriberBuilder],
            [rpcBuilder]);

        using var runCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var runTask = launcher.Run(runCts.Token);

        var completed = await Task.WhenAny(runTask, Task.Delay(TimeSpan.FromSeconds(1)));
        Assert.NotEqual(runTask, completed);

        runCts.Cancel();
        await runTask;
    }

    [Fact]
    public async Task Rpc_Run_Should_Remain_Pending_Until_Cancelled()
    {
        var rpcQueue = $"launcher-rpc-pending-{Guid.NewGuid():N}";
        var receiverBuilder = new MockedReceiverRegistrationBuilder(register: false);
        var subscriberBuilder = new MockedSubscriberRegistrationBuilder(register: false);
        var rpcBuilder = new MockedRpcRegistrationBuilder(rpcQueue);

        _mapper.Map<TestMessage, TestCommandWithResult>(Arg.Any<TestMessage>())
            .Returns(ci => new TestCommandWithResult(ci.Arg<TestMessage>().Id));

        _mapper.Map<TestCommandResult, TestResponseMessage>(Arg.Any<TestCommandResult>())
            .Returns(ci => new TestResponseMessage { ResultId = ci.Arg<TestCommandResult>().ResultId });

        var launcher = new RabbitMediatorExecutorLauncher(
            _serviceProvider,
            [receiverBuilder],
            [subscriberBuilder],
            [rpcBuilder]);

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
        var builder = new MockedReceiverRegistrationBuilder(queueName);

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

        var launcher = new RabbitMediatorExecutorLauncher(
            _serviceProvider,
            [builder],
            [new MockedSubscriberRegistrationBuilder(register: false)],
            [new MockedRpcRegistrationBuilder(register: false)]);

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
        var receiverBuilder = new MockedReceiverRegistrationBuilder(register: false);
        var subscriberBuilder = new MockedSubscriberRegistrationBuilder(exchangeName);

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

        var launcher = new RabbitMediatorExecutorLauncher(
            _serviceProvider,
            [receiverBuilder],
            [subscriberBuilder],
            [new MockedRpcRegistrationBuilder()]);

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
    public async Task Should_Process_Multiple_Rpc_Requests_With_Delays()
    {
        var rpcQueue = $"launcher-rpc-multi-{Guid.NewGuid():N}";
        var receiverBuilder = new MockedReceiverRegistrationBuilder(register: false);
        var subscriberBuilder = new MockedSubscriberRegistrationBuilder(register: false);
        var rpcBuilder = new MockedRpcRegistrationBuilder(rpcQueue);

        const int totalMessages = 4;

        _mapper.Map<TestMessage, TestCommandWithResult>(Arg.Any<TestMessage>()).Returns(ci =>
        {
            var message = ci.Arg<TestMessage>();
            return new TestCommandWithResult(message.Id);
        });

        _mapper.Map<TestCommandResult, TestResponseMessage>(Arg.Any<TestCommandResult>())
            .Returns(ci => new TestResponseMessage { ResultId = ci.Arg<TestCommandResult>().ResultId });

        var launcher = new RabbitMediatorExecutorLauncher(
            _serviceProvider,
            [receiverBuilder],
            [subscriberBuilder],
            [rpcBuilder]);

        using var listenerCts = new CancellationTokenSource(TimeSpan.FromSeconds(totalMessages * 5));
        var runTask = launcher.Run(listenerCts.Token);

        var rpcOptions = new RpcOptions
        {
            QueueName = rpcQueue,
            AutoDelete = true,
            Durable = false
        };

        var factory = new RpcFactory<TestMessage, TestResponseMessage>(_fixture.Bus, rpcOptions);
        var sender = new RpcMessageSender<TestMessage, TestResponseMessage>(factory);

        var responseTasks = Enumerable.Range(1, totalMessages)
            .Select(id => sender.RequestAsync(new TestMessage { Id = id }))
            .ToList();

        var allResponses = Task.WhenAll(responseTasks);

        var completed = await Task.WhenAny(
            allResponses,
            Task.Delay(TimeSpan.FromSeconds(totalMessages * 5)));

        if (completed != allResponses)
        {
            throw new TimeoutException("Not all RPC responses were processed");
        }

        foreach (var response in await allResponses)
        {
            Assert.Equal(10, response.ResultId);
        }

        await listenerCts.CancelAsync();
        await runTask;
    }

    [Fact]
    public async Task Run_With_No_Registrations_Completes_Immediately()
    {
        var launcher = new RabbitMediatorExecutorLauncher(
            _serviceProvider,
            [new MockedReceiverRegistrationBuilder(register: false)],
            [new MockedSubscriberRegistrationBuilder(register: false)],
            [new MockedRpcRegistrationBuilder(register: false)]);

        var runTask = launcher.Run();

        await runTask;

        Assert.True(runTask.IsCompletedSuccessfully);
    }
}
