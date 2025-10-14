using EasyNetQ.Mediator.Factories;
using EasyNetQ.Mediator.Message;
using MediatR;

namespace EasyNetQ.Mediator.Registrations;

public class ReceiverRegistrationBuilder
{
    public readonly List<ReceiverRegistration> Registrations = [];

    public ReceiverRegistration Register()
    {
        var registration = new ReceiverRegistration();
        Registrations.Add(registration);
        return registration;
    }
}

public class ReceiverRegistration()
{
    public Type? MessageType { get; private set; }
    public Type? CommandType { get; private set; }
    public QueueOptions Options { get; } = new ();
    
    public ReceiverRegistration OnMessage<TMessage>() where TMessage : BaseMessage
    {
        MessageType = typeof(TMessage);
        return this;
    }

    public ReceiverRegistration OnCommand<TCommand>() where TCommand : IRequest
    {
        CommandType = typeof(TCommand);
        return this;
    }

    public ReceiverRegistration WithOptions(Action<QueueOptions> action)
    {
        action(Options);
        return this;
    }
}