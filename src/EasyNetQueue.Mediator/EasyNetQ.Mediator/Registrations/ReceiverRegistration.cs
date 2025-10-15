using EasyNetQ.Mediator.Factories;
using EasyNetQ.Mediator.Message;
using MediatR;

namespace EasyNetQ.Mediator.Registrations;

public class ReceiverRegistrationBuilder : BaseRegistrationBuilder<ReceiverRegistration>
{
    public ReceiverRegistration Register()
    {
        var registration = new ReceiverRegistration();
        Registrations.Add(registration);
        return registration;
    }
}

public class ReceiverRegistration() : BaseRegistration<QueueOptions>(new QueueOptions());