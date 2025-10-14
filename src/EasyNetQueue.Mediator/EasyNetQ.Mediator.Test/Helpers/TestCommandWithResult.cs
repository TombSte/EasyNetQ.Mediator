using MediatR;

namespace EasyNetQ.Mediator.Test.Helpers;

public record TestCommand(int Id) : IRequest;

public interface ITestDependency
{
    void DoStuff();
}

public class TestCommandHandler(ITestDependency dependency) : IRequestHandler<TestCommand>
{
    public Task Handle(TestCommand request, CancellationToken cancellationToken)
    {
        dependency.DoStuff();
        return Task.CompletedTask;
    }
}