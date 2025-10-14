namespace EasyNetQ.Mediator.Mapping;

public interface IMessageMapper
{
    public TOutput Map<TInput, TOutput>(TInput input);
}