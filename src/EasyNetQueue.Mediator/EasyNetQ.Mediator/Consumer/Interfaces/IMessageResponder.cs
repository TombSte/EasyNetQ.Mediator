using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Mediator.Message;

namespace EasyNetQ.Mediator.Consumer.Interfaces;

public interface IMessageResponder<TRequest, TResponse>
    where TRequest : BaseMessage
    where TResponse : BaseMessage
{
    public delegate Task<TResponse> OnRespond(TRequest message);

    Task RespondAsync(OnRespond onRespond, CancellationToken cancellationToken = default);
}
