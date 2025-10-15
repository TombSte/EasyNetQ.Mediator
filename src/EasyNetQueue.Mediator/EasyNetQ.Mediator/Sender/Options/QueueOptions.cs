namespace EasyNetQ.Mediator.Factories;

/// <summary>
/// Base queue configuration applied when declaring queues used by receivers or senders.
/// </summary>
public class QueueOptions
{
    /// <summary>
    /// RabbitMQ queue name. When null, a convention based name is generated.
    /// </summary>
    public string? QueueName { get; set; } 

    /// <summary>
    /// True to create an exclusive queue scoped to the connection.
    /// </summary>
    public bool Exclusive { get; set; } = false;

    /// <summary>
    /// True to delete the queue automatically when unused.
    /// </summary>
    public bool AutoDelete { get; set; } = false;

    /// <summary>
    /// True to persist queue metadata across broker restarts.
    /// </summary>
    public bool Durable { get; set; } = true;
}
