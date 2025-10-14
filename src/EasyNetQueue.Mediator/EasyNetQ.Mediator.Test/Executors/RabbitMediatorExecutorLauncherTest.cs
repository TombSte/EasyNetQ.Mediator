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
    public RabbitMediatorExecutorLauncherTest(IntegrationTestFixture fixture)
    {
        IServiceCollection serviceCollection = new ServiceCollection();
        _fixture = fixture;
        _mapper = Substitute.For<IMessageMapper>();
        
        serviceCollection.AddEasyNetQMediator();
        serviceCollection.AddImplicitDependencies(fixture, mapper: _mapper);
        _serviceProvider = serviceCollection.BuildServiceProvider();
    }

    [Fact]
    public async Task RabbitMediatorExecutorLauncher()
    {
        //Arrange
        var builder = new ReceiverRegistrationBuilder();

        _mapper.Map<TestMessage, TestCommand>(Arg.Any<TestMessage>()).Returns(
            new TestCommand(1));
        
        builder.Register()
            .OnCommand<TestCommand>()
            .OnMessage<TestMessage>()
            .WithOptions(x => {});
        
        RabbitMediatorExecutorLauncher launcher = new RabbitMediatorExecutorLauncher(builder, _serviceProvider);
        
        var task = launcher.Run();

        var options = new QueueOptions();
        
        var factory = new QueueFactory<TestMessage>(_fixture.Bus, options);
        
        var messageSender = new MessageSender<TestMessage>(factory);
        
        await messageSender.SendAsync(new TestMessage()
        {
            Id = 1,
        });
        
        task.Wait();
    }
}