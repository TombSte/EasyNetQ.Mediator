using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ.Mediator.Executors;

public interface IRabbitMediatorExecutorLauncher
{
    Task Run(CancellationToken cancellationToken = default);
}
