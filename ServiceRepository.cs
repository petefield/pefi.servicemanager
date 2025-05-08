using dnsimple;
using pefi.Rabbit;

namespace pefi.servicemanager
{
    public class ServiceRepository : IServiceRepository
    {
        public ServiceRepository(ILogger<ServiceRepository> logger)
        {
            Logger = logger;
        }

        public List<ServiceDescription> Services { get; set; } = [];
        public ILogger<ServiceRepository> Logger { get; }

        public async Task< ServiceDescription>  Add(string Name, string? hostName, string? containerPortNumber, string? hostPortNumber)
        {
            using var messageBroker = await pefi.Rabbit.MessageBroker.Create("192.168.0.5", "username", "password");
            using var topic = await messageBroker.CreateTopic("Events");


            var service = new ServiceDescription(Name, hostName, containerPortNumber, hostPortNumber);
            Services.Add(service);
            await topic.Publish("events.service.created", service.ServiceName);
            Logger.LogWarning("message sent");

            return service;
        }

        public ServiceDescription? GetService(string name)
        {
            return Services.FirstOrDefault(s => s.ServiceName == name);
        }
    }
}
