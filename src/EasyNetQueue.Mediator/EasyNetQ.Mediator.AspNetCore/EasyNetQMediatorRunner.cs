using EasyNetQ.Mediator.Executors;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EasyNetQ.Mediator.AspNetCore;

public class EasyNetQMediatorRunner(IRabbitMediatorExecutorLauncher launcher, ILogger<EasyNetQMediatorRunner> logger) : IHostedService
{
    private readonly IRabbitMediatorExecutorLauncher _launcher = launcher;
    private Task? _runTask;
    private CancellationTokenSource? _linkedCts;
    private readonly ILogger<EasyNetQMediatorRunner> _logger = logger;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting RabbitMQ mediator executor.");
        _linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _runTask = _launcher.Run(_linkedCts.Token);
        return _runTask.IsCompleted ? _runTask : Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping RabbitMQ mediator executor.");
        if (_runTask is null)
        {
            return Task.CompletedTask;
        }

        _linkedCts?.Cancel();
        return Task.WhenAny(_runTask, Task.Delay(Timeout.Infinite, cancellationToken));
    }
}
