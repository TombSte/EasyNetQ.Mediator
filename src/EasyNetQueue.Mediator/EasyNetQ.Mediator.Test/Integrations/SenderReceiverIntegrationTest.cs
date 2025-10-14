using EasyNetQ.Mediator.Consumer.Implementations;
using EasyNetQ.Mediator.Factories;
using EasyNetQ.Mediator.Message;
using EasyNetQ.Mediator.Sender.Implementations;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace EasyNetQ.Mediator.Test.Integrations;

public record NewPerson(string FirstName, string LastName) : BaseMessage;

public class SenderReceiverIntegrationTest(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    [Fact]
    public void Should_be_able_to_send_a_message()
    {
        
        var queueName = "test-integration-send-new-person";
        var options = new QueueOptions()
        {
            AutoDelete = true,
            QueueName = queueName,
            Durable = false
        };
        
        var factory = new QueueFactory<NewPerson>(fixture.Bus, options);
        
        var messageSender = new MessageSender<NewPerson>(factory);
        
        var p = new NewPerson("John", "Doe");
        
        Action act = () => messageSender.SendAsync(p).GetAwaiter().GetResult();
        act.Should().NotThrow();
    }
    
    [Fact]
    public async Task ShouldSendAndReceiveAMessage()
    {
        var queueName = "test-integration-new-person";
        var options = new QueueOptions()
        {
            AutoDelete = true,
            QueueName = queueName,
            Durable = false
        };
        var factory = new QueueFactory<NewPerson>(fixture.Bus, options);

        
        
        var messageSender = new MessageSender<NewPerson>(factory);
        var messageReceiver = new MessageReceiver<NewPerson>(factory, Substitute.For<ILogger<MessageReceiver<NewPerson>>>());
        
        var p = new NewPerson("John" + Guid.NewGuid(), "Doe");

        await messageSender.SendAsync(p);

        await messageReceiver.ReceiveAsync(async x =>
        {
            x.Should().BeEquivalentTo(p);
        });
    }
}