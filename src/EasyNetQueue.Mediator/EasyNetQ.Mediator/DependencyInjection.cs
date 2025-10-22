using EasyNetQ.Mediator.Consumer.Implementations;
using EasyNetQ.Mediator.Consumer.Interfaces;
using EasyNetQ.Mediator.Consumer.Options;
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
        services.AddSingleton(typeof(IQueueFactory<>), typeof(QueueFactory<>));
        services.AddSingleton<QueueOptions>();

        services.AddSingleton(typeof(ISubscriberFactory<>), typeof(SubscriberFactory<>));
        services.AddSingleton(typeof(IMessageSubscriber<>), typeof(MessageSubscriber<>));
        services.AddSingleton(typeof(SubscriberExecutor<,>));
        services.AddSingleton<SubscriberOptions>();

        services.AddSingleton(typeof(IRpcFactory<,>), typeof(RpcFactory<,>));
        services.AddSingleton(typeof(IMessageResponder<,>), typeof(MessageResponder<,>));
        services.AddSingleton(typeof(RpcExecutor<,,,>));
        services.AddSingleton<RpcOptions>();
        services.AddSingleton<IRabbitMediatorExecutorLauncher, RabbitMediatorExecutorLauncher>();
    }
}
