namespace EasyNetQ.Mediator;

public static class Helper
{
    public static string DefaultQueueName<T>() => typeof(T).Name.ToLower() + "-queue";
    public static string DefaultExchangeName<T>() => typeof(T).Name.ToLower() + "-exchange";
    public static string DefaultSubQueueName<T>() => typeof(T).Name.ToLower() + "-exchange-queue";
    public static string DefaultRpcQueueName<T>() => typeof(T).Name.ToLower() + "-rpc";
}
