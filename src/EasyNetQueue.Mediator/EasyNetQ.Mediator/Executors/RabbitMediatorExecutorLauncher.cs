using System.Collections.Generic;
using System.Reflection;
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

public class RabbitMediatorExecutorLauncher(ReceiverRegistrationBuilder builder, IServiceProvider serviceProvider)
{
    private const string ExecuteMethodName = "Execute";

    public Task Run()
    {
        if (builder.Registrations.Count == 0)
        {
            return Task.CompletedTask;
        }

        var tasks = new List<Task>(builder.Registrations.Count);

        foreach (var receiverRegistration in builder.Registrations)
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
                var executor = Activator.CreateInstance(executorType, messageReceiver, sender, mapper, scopedProvider)
                               ?? throw new InvalidOperationException($"Unable to create receiver executor for message {messageType.Name} and command {commandType.Name}.");

                var executeMethod = executorType.GetMethod(ExecuteMethodName, BindingFlags.Instance | BindingFlags.Public)
                                    ?? throw new InvalidOperationException($"Execute method not found on executor for message {messageType.Name} and command {commandType.Name}.");

                var executeTask = executeMethod.Invoke(executor, null) as Task
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

        return Task.WhenAll(tasks);
    }

    private static object GetNullLogger(Type messageReceiverType)
    {
        var nullLoggerType = typeof(NullLogger<>).MakeGenericType(messageReceiverType);
        var instanceProperty = nullLoggerType.GetProperty(nameof(NullLogger<object>.Instance), BindingFlags.Public | BindingFlags.Static);
        return instanceProperty?.GetValue(null) ?? throw new InvalidOperationException($"Unable to create null logger for {messageReceiverType.Name}.");
    }
}
