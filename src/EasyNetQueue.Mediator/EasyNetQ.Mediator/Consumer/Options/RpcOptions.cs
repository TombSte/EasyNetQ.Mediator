using System;
using EasyNetQ.Mediator.Factories;
namespace EasyNetQ.Mediator.Consumer.Options;

/// <summary>
/// Options controlling RPC responder and requester queue semantics.
/// </summary>
public class RpcOptions : QueueOptions
{
    /// <summary>
    /// Message type used for incoming RPC requests.
    /// </summary>
    public Type? RequestMessageType { get; internal set; }

    /// <summary>
    /// Message type used for outgoing RPC responses.
    /// </summary>
    public Type? ResponseMessageType { get; internal set; }

    /// <summary>
    /// MediatR command type invoked when an RPC request is received.
    /// </summary>
    public Type? CommandType { get; internal set; }

    /// <summary>
    /// Result type returned by the MediatR handler before mapping back to a response message.
    /// </summary>
    public Type? CommandResultType { get; internal set; }

    /// <summary>
    /// Optional prefetch count for responders to limit concurrent unacknowledged messages.
    /// </summary>
    public ushort? PrefetchCount { get; set; }

    /// <summary>
    /// Maximum time to wait for an RPC response before cancelling the request.
    /// Defaults to 30 seconds.
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
