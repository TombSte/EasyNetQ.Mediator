using AutoMapper;
using EasyNetQ.Mediator.Mapping;

namespace EasyNetQ.Mediator.AutoMapper;

public class MessageAutoMapper(IMapper mapper) : IMessageMapper
{
    public TOutput Map<TInput, TOutput>(TInput input)
    {
        return mapper.Map<TInput, TOutput>(input);
    }
}