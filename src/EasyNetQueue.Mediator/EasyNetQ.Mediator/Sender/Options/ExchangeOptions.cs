using EasyNetQ.Mediator.Factories;

namespace EasyNetQ.Mediator.Sender.Options;

public class ExchangeOptions : QueueOptions
{
    public string RoutingKey { get; set; }
    public new bool Exclusive { get; set; } = false;
}