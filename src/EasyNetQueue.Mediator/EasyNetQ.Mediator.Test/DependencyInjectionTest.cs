using System.Reflection;
using EasyNetQ.Mediator.Consumer.Interfaces;
using EasyNetQ.Mediator.Executors;
using EasyNetQ.Mediator.Factories;
using EasyNetQ.Mediator.Mapping;
using EasyNetQ.Mediator.Registrations;
using EasyNetQ.Mediator.Test.Helpers;
using EasyNetQ.Mediator.Test.Integrations;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace EasyNetQ.Mediator.Test;

public class DependencyInjectionTest : IClassFixture<IntegrationTestFixture>
{
    IServiceProvider _serviceProvider;
    public DependencyInjectionTest(IntegrationTestFixture fixture)
    {
        IServiceCollection serviceCollection = new ServiceCollection();
        
        serviceCollection.AddEasyNetQMediator();
        serviceCollection.AddImplicitDependencies();
        _serviceProvider = serviceCollection.BuildServiceProvider();
    }

    [Fact]
    public void ReceiverExecutor_IsResolved()
    {
        var executor = _serviceProvider.GetRequiredService<ReceiverExecutor<TestMessage, TestCommand>>();
        executor.Should().NotBeNull();
    }
    
    [Fact]
    public void IMessageReceiver_IsResolved()
    {
        var executor = _serviceProvider.GetRequiredService<IMessageReceiver<TestMessage>>();
        executor.Should().NotBeNull();
    }

    [Fact]
    public void ReceiverRegistrationBuilder_IsResolved()
    {
        var builder = _serviceProvider.GetRequiredService<ReceiverRegistrationBuilder>();
        builder.Should().NotBeNull();
    }
}