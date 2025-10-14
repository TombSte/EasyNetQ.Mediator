using EasyNetQ.Mediator.Consumer.Interfaces;
using EasyNetQ.Mediator.Executors;
using EasyNetQ.Mediator.Mapping;
using EasyNetQ.Mediator.Test.Helpers;
using MediatR;
using NSubstitute;

namespace EasyNetQ.Mediator.Test.Executors;

public class ReceiverExecutorTest
{
    ReceiverExecutor<TestMessage, TestCommand> _executor;
    IMessageReceiver<TestMessage> _messageReceiver;
    ISender _sender;
    IMessageMapper _messageMapper;
    IServiceProvider _serviceProvider;
    public ReceiverExecutorTest()
    {
        _messageReceiver = Substitute.For<IMessageReceiver<TestMessage>>();
        _sender = Substitute.For<ISender>();
        _messageMapper = Substitute.For<IMessageMapper>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _executor = new ReceiverExecutor<TestMessage, TestCommand>(_messageReceiver, _sender, _messageMapper, _serviceProvider);
    }
    
    
}