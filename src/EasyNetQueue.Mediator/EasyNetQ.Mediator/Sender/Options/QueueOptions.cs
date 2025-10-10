namespace EasyNetQ.Mediator.Factories;

public class QueueOptions
{
    public required string QueueName { get; set; }
    public bool Exclusive { get; set; } = false;
    public bool AutoDelete { get; set; } = false;
    public bool Durable { get; set; } = true;
}