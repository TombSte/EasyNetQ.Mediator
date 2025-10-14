using EasyNetQ.Mediator.Consumer.Interfaces;
using EasyNetQ.Mediator.Executors;
using EasyNetQ.Mediator.Mapping;
using EasyNetQ.Mediator.Test.Helpers;
using MediatR;
using NSubstitute;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Mediator.Test.Executors;

public class ReceiverExecutorTest
{
    ReceiverExecutor<TestMessage, TestCommand> _executor;
    IMessageReceiver<TestMessage> _messageReceiver;
    ISender _sender;
    IMessageMapper _messageMapper;
    IServiceScopeFactory _scopeFactory;
    IServiceScope _scope;
    public ReceiverExecutorTest()
    {
        _messageReceiver = Substitute.For<IMessageReceiver<TestMessage>>();
        _sender = Substitute.For<ISender>();
        _messageMapper = Substitute.For<IMessageMapper>();
        _scopeFactory = Substitute.For<IServiceScopeFactory>();
        _scope = Substitute.For<IServiceScope>();
        _scopeFactory.CreateScope().Returns(_scope);
        _executor = new ReceiverExecutor<TestMessage, TestCommand>(_messageReceiver, _sender, _messageMapper, _scopeFactory);
    }
    
    
}
