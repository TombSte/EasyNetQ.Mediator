using EasyNetQ.Mediator.Mapping;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Mediator.AutoMapper;

public static class DependencyInjection
{
    public static void AddMessageAutoMapper(this IServiceCollection services)
    {
        services.AddSingleton<IMessageMapper, MessageAutoMapper>();
    }
}