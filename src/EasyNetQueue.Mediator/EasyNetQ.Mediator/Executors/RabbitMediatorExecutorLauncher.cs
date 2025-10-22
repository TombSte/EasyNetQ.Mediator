using System.Reflection;
using EasyNetQ.Mediator.Consumer.Implementations;
using EasyNetQ.Mediator.Factories;
using EasyNetQ.Mediator.Mapping;
using EasyNetQ.Mediator.Registrations;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EasyNetQ.Mediator.Executors;

public class RabbitMediatorExecutorLauncher(
    IServiceProvider serviceProvider,
    IEnumerable<ReceiverRegistrationBuilder> receiverBuilders, 
    IEnumerable<SubscriberRegistrationBuilder> subscriberBuilders,
    IEnumerable<RpcRegistrationBuilder> rpcBuilders) : IRabbitMediatorExecutorLauncher
{
    private const string ExecuteMethodName = "Execute";

    public Task Run(CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>();

        if (receiverBuilders.Any() && receiverBuilders.SelectMany(x => x.Registrations).Any())
        {
            tasks.AddRange(RunReceivers(cancellationToken));
        }

        if (subscriberBuilders.Any() && subscriberBuilders.SelectMany(x => x.Registrations).Any())
        {
            tasks.AddRange(RunSubscribers(cancellationToken));
        }

        if (rpcBuilders.Any() && rpcBuilders.SelectMany(x => x.Registrations).Any())
        {
            tasks.AddRange(RunResponders(cancellationToken));
        }

        if (tasks.Count == 0)
        {
            return Task.CompletedTask;
        }

        return Task.WhenAll(tasks);
    }

    private List<Task> RunReceivers(CancellationToken cancellationToken)
    {
        var tasks = new List<Task>(receiverBuilders.SelectMany(x => x.Registrations).Count());

        foreach (var receiverBuilder in receiverBuilders)
        foreach (var receiverRegistration in receiverBuilder.Registrations)
        {
            var messageType = receiverRegistration.MessageType 
                              ?? throw new InvalidOperationException("Receiver registration missing MessageType.");
            var commandType = receiverRegistration.CommandType 
                              ?? throw new InvalidOperationException("Receiver registration missing CommandType.");

            var scope = serviceProvider.CreateScope();
            var scopedProvider = scope.ServiceProvider;

            try
            {
                var bus = scopedProvider.GetRequiredService<IBus>();

                var queueFactoryType = typeof(QueueFactory<>).MakeGenericType(messageType);
                var queueFactory = Activator.CreateInstance(queueFactoryType, bus, receiverRegistration.Options)
                                   ?? throw new InvalidOperationException($"Unable to create queue factory for message type {messageType.Name}.");

                var messageReceiverType = typeof(MessageReceiver<>).MakeGenericType(messageType);
                var loggerType = typeof(ILogger<>).MakeGenericType(messageReceiverType);
                var logger = scopedProvider.GetService(loggerType) ?? GetNullLogger(messageReceiverType);

                var messageReceiver = Activator.CreateInstance(messageReceiverType, queueFactory, logger)
                                      ?? throw new InvalidOperationException($"Unable to create message receiver for message type {messageType.Name}.");

                var sender = scopedProvider.GetRequiredService<ISender>();
                var mapper = scopedProvider.GetRequiredService<IMessageMapper>();

                var executorType = typeof(ReceiverExecutor<,>).MakeGenericType(messageType, commandType);
                var executor = Activator.CreateInstance(executorType, messageReceiver, sender, mapper)
                               ?? throw new InvalidOperationException($"Unable to create receiver executor for message {messageType.Name} and command {commandType.Name}.");

                var executeMethod = executorType.GetMethod(
                                        ExecuteMethodName,
                                        BindingFlags.Instance | BindingFlags.Public,
                                        binder: null,
                                        types: [typeof(CancellationToken)],
                                        modifiers: null)
                                    ?? throw new InvalidOperationException($"Execute method not found on executor for message {messageType.Name} and command {commandType.Name}.");

                var executeTask = executeMethod.Invoke(executor, [cancellationToken]) as Task
                                  ?? throw new InvalidOperationException("Receiver executor Execute must return a Task.");

                var trackedTask = executeTask
                    .ContinueWith(t =>
                    {
                        scope.Dispose();
                        return t;
                    }, TaskScheduler.Default)
                    .Unwrap();

                tasks.Add(trackedTask);
            }
            catch
            {
                scope.Dispose();
                throw;
            }
        }

        return tasks;
    }

    private List<Task> RunSubscribers(CancellationToken cancellationToken)
    {
        var tasks = new List<Task>(subscriberBuilders.SelectMany(x => x.Registrations).Count());

        foreach (var subscriberBuilder in subscriberBuilders)
        foreach (var subscriberRegistration in subscriberBuilder.Registrations)
        {
            var messageType = subscriberRegistration.MessageType
                              ?? throw new InvalidOperationException("Subscriber registration missing MessageType.");
            var commandType = subscriberRegistration.CommandType
                              ?? throw new InvalidOperationException("Subscriber registration missing CommandType.");

            var scope = serviceProvider.CreateScope();
            var scopedProvider = scope.ServiceProvider;

            try
            {
                var bus = scopedProvider.GetRequiredService<IBus>();

                var subscriberFactoryType = typeof(SubscriberFactory<>).MakeGenericType(messageType);
                var subscriberFactory = Activator.CreateInstance(
                                            subscriberFactoryType,
                                            bus,
                                            subscriberRegistration.Options)
                                       ?? throw new InvalidOperationException($"Unable to create subscriber factory for message type {messageType.Name}.");

                var messageSubscriberType = typeof(MessageSubscriber<>).MakeGenericType(messageType);
                var messageSubscriber = Activator.CreateInstance(messageSubscriberType, subscriberFactory)
                                        ?? throw new InvalidOperationException($"Unable to create message subscriber for message type {messageType.Name}.");

                var sender = scopedProvider.GetRequiredService<ISender>();
                var mapper = scopedProvider.GetRequiredService<IMessageMapper>();

                var executorType = typeof(SubscriberExecutor<,>).MakeGenericType(messageType, commandType);
                var executor = Activator.CreateInstance(executorType, messageSubscriber, sender, mapper)
                               ?? throw new InvalidOperationException($"Unable to create subscriber executor for message {messageType.Name} and command {commandType.Name}.");

                var executeMethod = executorType.GetMethod(
                                        ExecuteMethodName,
                                        BindingFlags.Instance | BindingFlags.Public,
                                        binder: null,
                                        types: [typeof(CancellationToken)],
                                        modifiers: null)
                                    ?? throw new InvalidOperationException($"Execute method not found on subscriber executor for message {messageType.Name} and command {commandType.Name}.");

                var executeTask = executeMethod.Invoke(executor, [cancellationToken]) as Task
                                  ?? throw new InvalidOperationException("Subscriber executor Execute must return a Task.");

                var trackedTask = executeTask
                    .ContinueWith(t =>
                    {
                        scope.Dispose();
                        return t;
                    }, TaskScheduler.Default)
                    .Unwrap();

                tasks.Add(trackedTask);
            }
            catch
            {
                scope.Dispose();
                throw;
            }
        }

        return tasks;
    }

    private List<Task> RunResponders(CancellationToken cancellationToken)
    {
        var tasks = new List<Task>(rpcBuilders.SelectMany(x => x.Registrations).Count());

        foreach (var rpcBuilder in rpcBuilders)
        foreach (var rpcRegistration in rpcBuilder.Registrations)
        {
            var requestMessageType = rpcRegistration.MessageType
                                      ?? throw new InvalidOperationException("RPC registration missing MessageType.");
            var responseMessageType = rpcRegistration.ResponseMessageType
                                      ?? throw new InvalidOperationException("RPC registration missing ResponseMessageType.");
            var commandType = rpcRegistration.CommandType
                               ?? throw new InvalidOperationException("RPC registration missing CommandType.");
            var commandResultType = rpcRegistration.CommandResultType
                                    ?? throw new InvalidOperationException("RPC registration missing CommandResultType.");

            rpcRegistration.Options.RequestMessageType ??= requestMessageType;
            rpcRegistration.Options.ResponseMessageType ??= responseMessageType;
            rpcRegistration.Options.CommandType ??= commandType;
            rpcRegistration.Options.CommandResultType ??= commandResultType;

            var scope = serviceProvider.CreateScope();
            var scopedProvider = scope.ServiceProvider;

            try
            {
                var bus = scopedProvider.GetRequiredService<IBus>();

                var rpcFactoryType = typeof(RpcFactory<,>).MakeGenericType(requestMessageType, responseMessageType);
                var rpcFactory = Activator.CreateInstance(rpcFactoryType, bus, rpcRegistration.Options)
                                 ?? throw new InvalidOperationException($"Unable to create RPC factory for message type {requestMessageType.Name}.");

                var responderType = typeof(MessageResponder<,>).MakeGenericType(requestMessageType, responseMessageType);
                var responder = Activator.CreateInstance(responderType, rpcFactory)
                               ?? throw new InvalidOperationException($"Unable to create message responder for message type {requestMessageType.Name}.");

                var sender = scopedProvider.GetRequiredService<ISender>();
                var mapper = scopedProvider.GetRequiredService<IMessageMapper>();

                var executorType = typeof(RpcExecutor<,,,>).MakeGenericType(requestMessageType, responseMessageType, commandType, commandResultType);
                var executor = Activator.CreateInstance(executorType, responder, sender, mapper)
                               ?? throw new InvalidOperationException($"Unable to create RPC executor for message {requestMessageType.Name} and command {commandType.Name}.");

                var executeMethod = executorType.GetMethod(
                                        ExecuteMethodName,
                                        BindingFlags.Instance | BindingFlags.Public,
                                        binder: null,
                                        types: [typeof(CancellationToken)],
                                        modifiers: null)
                                    ?? throw new InvalidOperationException($"Execute method not found on RPC executor for message {requestMessageType.Name} and command {commandType.Name}.");

                var executeTask = executeMethod.Invoke(executor, [cancellationToken]) as Task
                                  ?? throw new InvalidOperationException("RPC executor Execute must return a Task.");

                var trackedTask = executeTask
                    .ContinueWith(t =>
                    {
                        scope.Dispose();
                        return t;
                    }, TaskScheduler.Default)
                    .Unwrap();

                tasks.Add(trackedTask);
            }
            catch
            {
                scope.Dispose();
                throw;
            }
        }

        return tasks;
    }

    private static object GetNullLogger(Type messageReceiverType)
    {
        var nullLoggerType = typeof(NullLogger<>).MakeGenericType(messageReceiverType);
        var instanceProperty = nullLoggerType.GetProperty(nameof(NullLogger<object>.Instance), BindingFlags.Public | BindingFlags.Static);
        return instanceProperty?.GetValue(null) ?? throw new InvalidOperationException($"Unable to create null logger for {messageReceiverType.Name}.");
    }
}
