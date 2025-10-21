using System.ComponentModel;
using EasyNetQ.Mediator.Executors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EasyNetQ.Mediator.AspNetCore;

public class EasyNetQMediatorRunner(IServiceProvider serviceProvider, ILogger<EasyNetQMediatorRunner> logger) : IHostedService
{
    private readonly RabbitMediatorExecutorLauncher _launcher = serviceProvider.GetRequiredService<RabbitMediatorExecutorLauncher>();
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting RabbitMQ Mediator Executor");
        return _launcher.Run(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping RabbitMQ mediator executor.");
        return Task.CompletedTask;
    }
}