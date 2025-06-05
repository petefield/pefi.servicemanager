using System.Text.Json;
using MongoDB.Driver;
using pefi.Rabbit;

namespace pefi.servicemanager;

public class ServiceRepository(IMessageBroker messageBroker, IDataStore database) : IServiceRepository
{
    private readonly string databaseName = "ServiceDb";
    private readonly string serviceCollectionName = "services";

    public async Task<IEnumerable<ServiceDescription>> GetServices()
    {   
        var services = await database.Get<ServiceDescription>(databaseName, serviceCollectionName);
        return services;
    }
    
    public async Task<ServiceDescription> Add(string name, string? hostName, string? containerPortNumber, string? hostPortNumber)
    {
        var service = new ServiceDescription(name, hostName, containerPortNumber, hostPortNumber);

        await database.Add(databaseName, serviceCollectionName, service);

        using var topic = await messageBroker.CreateTopic("Events");
        var message = JsonSerializer.Serialize(service);
        await topic.Publish("events.service.created", message);

        return service;
    }

    public async Task<ServiceDescription?> GetService(string name)
    {
        var services = await database.Get<ServiceDescription>(databaseName, serviceCollectionName, s => s.ServiceName == name);
        return services.SingleOrDefault();
    }
}

