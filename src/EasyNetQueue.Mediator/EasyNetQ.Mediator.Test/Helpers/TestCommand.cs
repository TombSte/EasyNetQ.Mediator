using MediatR;

namespace EasyNetQ.Mediator.Test.Helpers;

public record TestCommandWithResult(int Id) : IRequest<TestCommandResult>;

public record TestCommandResult(int ResultId);
public class TestCommandWithResultHandler : IRequestHandler<TestCommandWithResult, TestCommandResult>
{
    public Task<TestCommandResult> Handle(TestCommandWithResult request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new TestCommandResult(10));
    }
}