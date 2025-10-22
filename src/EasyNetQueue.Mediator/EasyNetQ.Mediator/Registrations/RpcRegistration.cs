using EasyNetQ.Mediator.Consumer.Options;
using EasyNetQ.Mediator.Message;
using MediatR;

namespace EasyNetQ.Mediator.Registrations;

public abstract class RpcRegistrationBuilder : BaseRegistrationBuilder<RpcRegistration>
{
    protected RpcRegistration Register()
    {
        var registration = new RpcRegistration();
        Registrations.Add(registration);
        return registration;
    }
}

public class RpcRegistration() : BaseRegistration<RpcOptions>(new RpcOptions())
{
    public Type? ResponseMessageType { get; private set; }
    public Type? CommandResultType { get; private set; }

    public new RpcRegistration OnMessage<TMessage>() where TMessage : BaseMessage
    {
        base.OnMessage<TMessage>();
        Options.RequestMessageType = typeof(TMessage);
        if (CommandType is not null)
        {
            Options.CommandType ??= CommandType;
        }
        if (CommandResultType is not null)
        {
            Options.CommandResultType ??= CommandResultType;
        }
        return this;
    }

    public RpcRegistration OnCommand<TCommand, TResult>() where TCommand : IRequest<TResult>
    {
        CommandType = typeof(TCommand);
        CommandResultType = typeof(TResult);
        Options.CommandType = typeof(TCommand);
        Options.CommandResultType = CommandResultType;
        if (MessageType is not null)
        {
            Options.RequestMessageType ??= MessageType;
        }
        return this;
    }

    public RpcRegistration OnResponseMessage<TResponse>() where TResponse : BaseMessage
    {
        ResponseMessageType = typeof(TResponse);
        Options.ResponseMessageType = typeof(TResponse);
        if (CommandResultType is not null)
        {
            Options.CommandResultType ??= CommandResultType;
        }
        return this;
    }

    public new RpcRegistration WithOptions(Action<RpcOptions> action)
    {
        base.WithOptions(action);
        Options.RequestMessageType ??= MessageType;
        Options.ResponseMessageType ??= ResponseMessageType;
        Options.CommandType ??= CommandType;
        Options.CommandResultType ??= CommandResultType;
        return this;
    }
}
