using EasyNetQ.Mediator.Factories;

namespace EasyNetQ.Mediator.Registrations;

public abstract class ReceiverRegistrationBuilder : BaseRegistrationBuilder<ReceiverRegistration>
{
    protected ReceiverRegistration Register()
    {
        var registration = new ReceiverRegistration();
        Registrations.Add(registration);
        return registration;
    }
}

public class ReceiverRegistration() : BaseRegistration<QueueOptions>(new QueueOptions());