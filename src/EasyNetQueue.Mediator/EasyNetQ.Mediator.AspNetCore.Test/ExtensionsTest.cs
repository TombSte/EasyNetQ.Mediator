using System.Runtime.ExceptionServices;
using EasyNetQ.Mediator.Registrations;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Mediator.AspNetCore.Test;

public class ExtensionsTest
{
    private IServiceCollection serviceCollection;
    private IServiceProvider serviceProvider;
    
    private class R1 : ReceiverRegistrationBuilder;
    private class R2 : ReceiverRegistrationBuilder;
    private class S1 : SubscriberRegistrationBuilder;
    private class S2 : SubscriberRegistrationBuilder;
    private class RP1 : RpcRegistrationBuilder;
    private class RP2 : RpcRegistrationBuilder;
    
    public ExtensionsTest()
    {
        serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQMediator(x =>
        {
            x.AddReceiverRegistrationBuilder<R1>();
            x.AddReceiverRegistrationBuilder<R2>();
            x.AddSubscriberRegistrationBuilder<S1>();
            x.AddSubscriberRegistrationBuilder<S2>();
            x.AddRpcRegistrationBuilder<RP1>();
            x.AddRpcRegistrationBuilder<RP2>();
        });
        
        serviceProvider = serviceCollection.BuildServiceProvider();
    }

    [Fact]
    public void ReceiverRegistrationBuilder_ShouldBeResolved()
    {
        var builders = serviceProvider.GetServices<ReceiverRegistrationBuilder>().ToArray();
        
        builders.Should().NotBeNull();
        builders.Should().HaveCount(2);
        builders.Should().ContainItemsAssignableTo<R1>();
        builders.Should().ContainItemsAssignableTo<R2>();
    }
    
    [Fact]
    public void SubscriberRegistrationBuilder_ShouldBeResolved()
    {
        var builders = serviceProvider.GetServices<SubscriberRegistrationBuilder>().ToArray();
        
        builders.Should().NotBeNull();
        builders.Should().HaveCount(2);
        builders.Should().ContainItemsAssignableTo<S1>();
        builders.Should().ContainItemsAssignableTo<S2>();
    }
    
    [Fact]
    public void RpcRegistrationBuilder_ShouldBeResolved()
    {
        var builders = serviceProvider.GetServices<RpcRegistrationBuilder>().ToArray();
        
        builders.Should().NotBeNull();
        builders.Should().HaveCount(2);
        builders.Should().ContainItemsAssignableTo<RP1>();
        builders.Should().ContainItemsAssignableTo<RP2>();
    }
}