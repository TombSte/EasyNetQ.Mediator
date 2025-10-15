using EasyNetQ.Mediator.Sender.Options;

namespace EasyNetQ.Mediator.Consumer.Options;

/// <summary>
/// Options governing queue and exchange configuration for subscribers.
/// </summary>
public class SubscriberOptions : ExchangeOptions
{
    /// <summary>
    /// Name of the queue bound to the exchange for this subscriber instance.
    /// When null, a name based on the message type and entry assembly is generated.
    /// </summary>
    public string? SubQueueName { get; set; }
}
