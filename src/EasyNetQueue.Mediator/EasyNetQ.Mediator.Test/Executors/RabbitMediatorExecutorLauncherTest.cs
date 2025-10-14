using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Mediator.Executors;
using EasyNetQ.Mediator.Factories;
using EasyNetQ.Mediator.Mapping;
using EasyNetQ.Mediator.Registrations;
using EasyNetQ.Mediator.Sender.Implementations;
using EasyNetQ.Mediator.Test.Helpers;
using EasyNetQ.Mediator.Test.Integrations;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace EasyNetQ.Mediator.Test.Executors;

public class RabbitMediatorExecutorLauncherTest : IClassFixture<IntegrationTestFixture>
{
    IServiceProvider _serviceProvider;
    private IntegrationTestFixture _fixture;
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
        serviceCollection.AddSingleton(_dependency);
        _serviceProvider = serviceCollection.BuildServiceProvider();
    }

    [Fact]
    public async Task RabbitMediatorExecutorLauncher()
    {
        //Arrange
        var builder = new ReceiverRegistrationBuilder();

        var mapped = new TaskCompletionSource<TestMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        _mapper.Map<TestMessage, TestCommand>(Arg.Any<TestMessage>()).Returns(ci =>
        {
            mapped.TrySetResult(ci.Arg<TestMessage>());
            return new TestCommand(1);
        });
        
        builder.Register()
            .OnCommand<TestCommand>()
            .OnMessage<TestMessage>()
            .WithOptions(x => {});
        
        RabbitMediatorExecutorLauncher launcher = new RabbitMediatorExecutorLauncher(builder, _serviceProvider);

        using var listenerCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var task = launcher.Run(listenerCts.Token);

        var options = new QueueOptions();
        
        var factory = new QueueFactory<TestMessage>(_fixture.Bus, options);
        
        var messageSender = new MessageSender<TestMessage>(factory);
        
        await messageSender.SendAsync(new TestMessage()
        {
            Id = 1,
        });

        var completed = await Task.WhenAny(mapped.Task, Task.Delay(TimeSpan.FromSeconds(30), listenerCts.Token));
        if (completed != mapped.Task)
        {
            throw new TimeoutException("Mapper not invoked");
        }

        await listenerCts.CancelAsync();
        await task;
        
        _dependency.Received(1).DoStuff();
    }
}
