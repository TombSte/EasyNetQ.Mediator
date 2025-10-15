using EasyNetQ.Mediator.Consumer.Implementations;
using EasyNetQ.Mediator.Consumer.Interfaces;
using EasyNetQ.Mediator.Executors;
using EasyNetQ.Mediator.Factories;
using EasyNetQ.Mediator.Registrations;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Mediator;

public static class DependencyInjection
{
    public static void AddEasyNetQMediator(this IServiceCollection services)
    {
        services.AddSingleton(typeof(IMessageReceiver<>), typeof(MessageReceiver<>));
        services.AddSingleton(typeof(ReceiverExecutor<,>));
        services.AddSingleton<ReceiverRegistrationBuilder>();
        services.AddSingleton(typeof(IQueueFactory<>), typeof(QueueFactory<>));
        services.AddSingleton<RpcRegistrationBuilder>();
    }
}
