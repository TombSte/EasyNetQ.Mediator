using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ;
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
    ReceiverRegistrationBuilder receiverBuilder, 
    IServiceProvider serviceProvider,
    SubscriberRegistrationBuilder subscriberBuilder)
{
    private const string ExecuteMethodName = "Execute";

    public Task Run(CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>();

        if (receiverBuilder.Registrations.Count > 0)
        {
            tasks.AddRange(RunReceivers(cancellationToken));
        }

        if (subscriberBuilder.Registrations.Count > 0)
        {
            tasks.AddRange(RunSubscribers(cancellationToken));
        }

        if (tasks.Count == 0)
        {
            return Task.CompletedTask;
        }

        return Task.WhenAll(tasks);
    }

    private List<Task> RunReceivers(CancellationToken cancellationToken)
    {
        var tasks = new List<Task>(receiverBuilder.Registrations.Count);

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
                                        types: new[] { typeof(CancellationToken) },
                                        modifiers: null)
                                    ?? throw new InvalidOperationException($"Execute method not found on executor for message {messageType.Name} and command {commandType.Name}.");

                var executeTask = executeMethod.Invoke(executor, new object[] { cancellationToken }) as Task
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
        var tasks = new List<Task>(subscriberBuilder.Registrations.Count);

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

                var executorType = typeof(SubcriberExecutor<,>).MakeGenericType(messageType, commandType);
                var executor = Activator.CreateInstance(executorType, messageSubscriber, sender, mapper)
                               ?? throw new InvalidOperationException($"Unable to create subscriber executor for message {messageType.Name} and command {commandType.Name}.");

                var executeMethod = executorType.GetMethod(
                                        ExecuteMethodName,
                                        BindingFlags.Instance | BindingFlags.Public,
                                        binder: null,
                                        types: new[] { typeof(CancellationToken) },
                                        modifiers: null)
                                    ?? throw new InvalidOperationException($"Execute method not found on subscriber executor for message {messageType.Name} and command {commandType.Name}.");

                var executeTask = executeMethod.Invoke(executor, new object[] { cancellationToken }) as Task
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

    private static object GetNullLogger(Type messageReceiverType)
    {
        var nullLoggerType = typeof(NullLogger<>).MakeGenericType(messageReceiverType);
        var instanceProperty = nullLoggerType.GetProperty(nameof(NullLogger<object>.Instance), BindingFlags.Public | BindingFlags.Static);
        return instanceProperty?.GetValue(null) ?? throw new InvalidOperationException($"Unable to create null logger for {messageReceiverType.Name}.");
    }
}
