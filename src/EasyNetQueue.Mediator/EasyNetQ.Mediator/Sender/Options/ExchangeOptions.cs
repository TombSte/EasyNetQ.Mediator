using EasyNetQ.Mediator.Factories;

namespace EasyNetQ.Mediator.Sender.Options;

/// <summary>
/// Options applied when declaring exchanges used for publish/subscribe scenarios.
/// </summary>
public class ExchangeOptions : QueueOptions
{
    /// <summary>
    /// Routing key used when publishing messages. Defaults to empty string for fanout.
    /// </summary>
    public string RoutingKey { get; set; }

    /// <summary>
    /// Shadows the base flag to match exchange semantics. Exchanges default to non-exclusive.
    /// </summary>
    public new bool Exclusive { get; set; } = false;
}
