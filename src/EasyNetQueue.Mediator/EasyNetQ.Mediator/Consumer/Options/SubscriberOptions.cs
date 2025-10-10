using EasyNetQ.Mediator.Factories;
using EasyNetQ.Mediator.Sender.Options;

namespace EasyNetQ.Mediator.Consumer.Options;

public class SubscriberOptions : ExchangeOptions
{
    public string SubQueueName { get; set; }
}