using EasyNetQ.Mediator.Message;

namespace EasyNetQ.Mediator.Test.Helpers;

public record TestMessage : BaseMessage
{
    public int Id { get; set; }
}