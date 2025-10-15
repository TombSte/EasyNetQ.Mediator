using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Mediator.Consumer.Interfaces;
using EasyNetQ.Mediator.Mapping;
using EasyNetQ.Mediator.Message;
using MediatR;

namespace EasyNetQ.Mediator.Executors;

public class RpcExecutor<TRequestMessage, TResponseMessage, TCommand, TCommandResult>(
    IMessageResponder<TRequestMessage, TResponseMessage> responder,
    ISender sender,
    IMessageMapper mapper)
    where TRequestMessage : BaseMessage
    where TResponseMessage : BaseMessage
    where TCommand : IRequest<TCommandResult>
{
    public Task Execute(CancellationToken cancellationToken)
    {
        return responder.RespondAsync(async message =>
        {
            var command = mapper.Map<TRequestMessage, TCommand>(message)
                          ?? throw new ArgumentNullException(nameof(message), "Cannot map request message");

            var result = await sender.Send(command, cancellationToken).ConfigureAwait(false);

            var responseMessage = mapper.Map<TCommandResult, TResponseMessage>(result)
                                  ?? throw new ArgumentNullException(nameof(result), "Cannot map command result");

            return responseMessage;
        }, cancellationToken);
    }
}
