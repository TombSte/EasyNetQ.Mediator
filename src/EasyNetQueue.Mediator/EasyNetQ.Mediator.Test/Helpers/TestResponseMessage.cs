using EasyNetQ.Mediator.Message;

namespace EasyNetQ.Mediator.Test.Helpers;

public record TestResponseMessage : BaseMessage
{
    public int ResultId { get; init; }
}
