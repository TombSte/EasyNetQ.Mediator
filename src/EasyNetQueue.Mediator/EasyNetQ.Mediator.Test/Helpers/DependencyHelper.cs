using System.Reflection;
using EasyNetQ.Mediator.Factories;
using EasyNetQ.Mediator.Mapping;
using EasyNetQ.Mediator.Test.Integrations;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace EasyNetQ.Mediator.Test.Helpers;

public static class DependencyHelper
{
    public static void AddImplicitDependencies(this IServiceCollection services, IntegrationTestFixture? fixture = null, IMessageMapper? mapper = null)
    {
        var m = mapper ?? Substitute.For<IMessageMapper>(); 
        services.AddSingleton(m);
        var bus = fixture?.Bus ?? Substitute.For<IBus>();
        services.AddSingleton(bus);
        services.AddMediatR(opt =>
        {
            opt.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });
        services.AddSingleton(new QueueOptions());
        
        services.AddLogging();
    }
}