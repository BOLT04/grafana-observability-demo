namespace GrafanaOtelDemoApp.Application
{
    public interface IEventBusGateway
    {
        public void CreateGenerateCountersTimer();
        public Task PublishEvent();
        public Task ConsumeEvent();
    }
}
