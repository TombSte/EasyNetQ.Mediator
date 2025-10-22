using EasyNetQ.Mediator.Consumer.Options;

namespace EasyNetQ.Mediator.Registrations;

public abstract class SubscriberRegistrationBuilder : BaseRegistrationBuilder<SubscriberRegistration>
{
    protected SubscriberRegistration Subscribe()
    {
        var registration = new SubscriberRegistration();
        Registrations.Add(registration);
        return registration;
    }
}

public class SubscriberRegistration() : BaseRegistration<SubscriberOptions>(new SubscriberOptions());