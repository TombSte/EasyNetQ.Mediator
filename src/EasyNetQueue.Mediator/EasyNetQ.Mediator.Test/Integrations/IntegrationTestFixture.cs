namespace EasyNetQ.Mediator.Test.Integrations;

public class IntegrationTestFixture: IDisposable
{
    public IBus Bus { get; }
    
    public IntegrationTestFixture()
    {
        Bus = RabbitHutch.CreateBus("host=localhost;username=guest;password=guest");
    }
    
    public void Dispose()
    {
        Bus.Dispose();
    }
}