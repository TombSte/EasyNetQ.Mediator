using EasyNetQ.Mediator.Registrations;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Mediator.AspNetCore;

public static class Extensions
{
    public static void AddEasyNetQMediator(this IServiceCollection services, Action<IEasyNetQMediatorCollection> builder)
    {
        services.AddHostedService<EasyNetQMediatorRunner>();
        var collection = new EasyNetQMediatorCollection(services);
        builder(collection);
    }
}

public interface IEasyNetQMediatorCollection
{
    public void AddReceiverRegistrationBuilder<TBuilder>() 
        where TBuilder : ReceiverRegistrationBuilder;
    public void AddSubscriberRegistrationBuilder<TBuilder>() 
        where TBuilder : SubscriberRegistrationBuilder;
    public void AddRpcRegistrationBuilder<TBuilder>() 
        where TBuilder : RpcRegistrationBuilder;
}

public class EasyNetQMediatorCollection(IServiceCollection services) : IEasyNetQMediatorCollection
{
    public void AddReceiverRegistrationBuilder<TBuilder>() where TBuilder : ReceiverRegistrationBuilder
    {
        services.AddSingleton<ReceiverRegistrationBuilder, TBuilder>();
    }

    public void AddSubscriberRegistrationBuilder<TBuilder>() where TBuilder : SubscriberRegistrationBuilder
    {
        services.AddSingleton<SubscriberRegistrationBuilder, TBuilder>();
    }

    public void AddRpcRegistrationBuilder<TBuilder>() where TBuilder : RpcRegistrationBuilder
    {
        services.AddSingleton<RpcRegistrationBuilder, TBuilder>();
    }
}