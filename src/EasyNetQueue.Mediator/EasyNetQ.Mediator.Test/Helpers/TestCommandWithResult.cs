using MediatR;

namespace EasyNetQ.Mediator.Test.Helpers;

public record TestCommand(int Id) : IRequest;

public class TestCommandHandler : IRequestHandler<TestCommand>
{
    public Task Handle(TestCommand request, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}