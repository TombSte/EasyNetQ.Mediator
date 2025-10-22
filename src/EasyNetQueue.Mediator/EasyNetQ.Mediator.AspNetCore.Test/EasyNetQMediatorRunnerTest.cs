using System.Threading.Tasks.Sources;
using EasyNetQ.Mediator.AspNetCore;
using EasyNetQ.Mediator.Executors;
using Microsoft.Extensions.Logging.Abstractions;

namespace EasyNetQ.Mediator.AspNetCore.Test;

public class EasyNetQMediatorRunnerTest
{
    private sealed class FakeLauncher(Func<CancellationToken, Task> run) : IRabbitMediatorExecutorLauncher
    {
        private readonly Func<CancellationToken, Task> _run = run;
        public int Calls { get; private set; }

        public Task Run(CancellationToken cancellationToken = default)
        {
            Calls++;
            return _run(cancellationToken);
        }
    }

    [Fact]
    public async Task StartAsync_Completes_WhenLauncherFinishesImmediately()
    {
        var launcher = new FakeLauncher(_ => Task.CompletedTask);
        var runner = new EasyNetQMediatorRunner(launcher, NullLogger<EasyNetQMediatorRunner>.Instance);

        await runner.StartAsync(CancellationToken.None);
        Assert.Equal(1, launcher.Calls);

        await runner.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_Cancels_LongRunningLauncher()
    {
        var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var launcher = new FakeLauncher(token =>
        {
            token.Register(() => completionSource.TrySetResult());
            return completionSource.Task;
        });

        var runner = new EasyNetQMediatorRunner(launcher, NullLogger<EasyNetQMediatorRunner>.Instance);

        await runner.StartAsync(CancellationToken.None);
        Assert.Equal(1, launcher.Calls);

        using var stopCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await runner.StopAsync(stopCts.Token);

        Assert.True(completionSource.Task.IsCompleted);
    }
}
