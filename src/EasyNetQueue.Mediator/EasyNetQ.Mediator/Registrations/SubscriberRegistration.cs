using EasyNetQ.Mediator.Consumer.Options;

namespace EasyNetQ.Mediator.Registrations;

public class SubscriberRegistrationBuilder : BaseRegistrationBuilder<SubscriberRegistration>
{
    public SubscriberRegistration Subscribe()
    {
        var registration = new SubscriberRegistration();
        Registrations.Add(registration);
        return registration;
    }
}

public class SubscriberRegistration() : BaseRegistration<SubscriberOptions>(new SubscriberOptions());