using System;
using System.Threading;
using EasyNetQ.Mediator.Consumer.Implementations;
using EasyNetQ.Mediator.Consumer.Options;
using EasyNetQ.Mediator.Factories;
using EasyNetQ.Mediator.Sender.Implementations;
using EasyNetQ.Mediator.Test.Helpers;

namespace EasyNetQ.Mediator.Test.Integrations;

public class RpcIntegrationTest : IClassFixture<IntegrationTestFixture>
{
    IntegrationTestFixture _fixture;
    public RpcIntegrationTest(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task IntegrationRpc_ShouldSucceed()
    {
        var factory = new RpcFactory<TestMessage, TestResponseMessage>(_fixture.Bus, new RpcOptions());

        var sender = new RpcMessageSender<TestMessage, TestResponseMessage>(factory);
        var receiver = new MessageResponder<TestMessage, TestResponseMessage>(factory);

        using var responderCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var responderTask = receiver.RespondAsync(async message =>
        {
            await Task.Yield();
            return new TestResponseMessage { ResultId = message.Id + 1 };
        }, responderCts.Token);

        var response = await sender.RequestAsync(new TestMessage { Id = 1 }, responderCts.Token);

        Assert.Equal(2, response.ResultId);

        await responderCts.CancelAsync();
        await responderTask;

    }
}
