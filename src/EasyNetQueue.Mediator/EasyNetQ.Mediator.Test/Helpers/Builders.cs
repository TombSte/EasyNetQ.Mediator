using EasyNetQ.Mediator.Registrations;

namespace EasyNetQ.Mediator.Test.Helpers;

public class MockedReceiverRegistrationBuilder : ReceiverRegistrationBuilder
{
    public MockedReceiverRegistrationBuilder(string? queueName = null, bool register = true)
    {
        if (!register)
        {
            return;
        }

        Register()
            .OnCommand<TestCommand>()
            .OnMessage<TestMessage>()
            .WithOptions(options =>
            {
                if (queueName is not null)
                {
                    options.QueueName = queueName;
                }

                options.AutoDelete = true;
                options.Durable = false;
            });
    }
}

public class MockedSubscriberRegistrationBuilder : SubscriberRegistrationBuilder
{
    public MockedSubscriberRegistrationBuilder(string? exchangeName = null, bool register = true)
    {
        if (!register)
        {
            return;
        }

        Subscribe()
            .OnCommand<TestCommand>()
            .OnMessage<TestMessage>()
            .WithOptions(options =>
            {
                if (exchangeName is not null)
                {
                    options.QueueName = exchangeName;
                    options.SubQueueName = $"{exchangeName}-sub";
                }

                options.AutoDelete = true;
                options.Durable = false;
            });
    }
}

public class MockedRpcRegistrationBuilder : RpcRegistrationBuilder
{
    public MockedRpcRegistrationBuilder(string? rpcQueue = null, bool register = true)
    {
        if (!register)
        {
            return;
        }

        Register()
            .OnCommand<TestCommandWithResult, TestCommandResult>()
            .OnMessage<TestMessage>()
            .OnResponseMessage<TestResponseMessage>()
            .WithOptions(options =>
            {
                if (rpcQueue is not null)
                {
                    options.QueueName = rpcQueue;
                }

                options.AutoDelete = true;
                options.Durable = false;
            });
    }
}
