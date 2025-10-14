using EasyNetQ.Mediator.Registrations;
using EasyNetQ.Mediator.Test.Helpers;
using FluentAssertions;

namespace EasyNetQ.Mediator.Test.Registrations;

public class ReceiverRegistrationTest
{
    [Fact]
    public void Registration_Succss()
    {
        ReceiverRegistrationBuilder builder = new ReceiverRegistrationBuilder();
        builder
            .Register()
            .OnMessage<TestMessage>()
            .OnCommand<TestCommand>()
            .WithOptions(opt =>
            {
                opt.Exclusive = true;
            });

        builder.Registrations.Count.Should().Be(1);
        builder.Registrations[0].Should().BeOfType<ReceiverRegistration>();
        builder.Registrations[0].MessageType.Should().Be(typeof(TestMessage));
        builder.Registrations[0].CommandType.Should().Be(typeof(TestCommand));
        builder.Registrations[0].Options.Exclusive.Should().BeTrue();
        
    }
}