using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Mediator.AspNetCore;

public static class Extensions
{
    public static void UseEasyNetQMediator(this IServiceCollection services)
    {
        services.AddHostedService<EasyNetQMediatorRunner>();
    }
}