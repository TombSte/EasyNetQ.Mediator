using EasyNetQ.Mediator.Factories;
using EasyNetQ.Mediator.Message;
using MediatR;

namespace EasyNetQ.Mediator.Registrations;

public abstract class BaseRegistrationBuilder<T>
{
    public readonly List<T> Registrations = [];
}

public abstract class BaseRegistration<T>
{
    protected BaseRegistration(T options)
    {
        Options = options;
    }

    public Type? MessageType { get; protected set; }
    public Type? CommandType { get; protected set; }
    
    public T Options { get; protected set; }

    public BaseRegistration<T> OnMessage<TMessage>() where TMessage : BaseMessage
    {
        MessageType = typeof(TMessage);
        return this;
    }

    public BaseRegistration<T> OnCommand<TCommand>() where TCommand : IRequest
    {
        CommandType = typeof(TCommand);
        return this;
    }

    public BaseRegistration<T> WithOptions(Action<T> action)
    {
        action(Options);
        return this;
    }
}