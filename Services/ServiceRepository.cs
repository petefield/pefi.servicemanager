using MongoDB.Driver;
using pefi.Rabbit;
using pefi.servicemanager.Contracts.Messages;
using System.Text.Json;
using Service = pefi.servicemanager.Models.Service;

namespace pefi.servicemanager.Services;

public class ServiceRepository(IMessageBroker messageBroker, IDataStore database) : IServiceRepository
{
    private readonly string databaseName = "ServiceDb";
    private readonly string serviceCollectionName = "services";

    public async Task<IEnumerable<Service>> GetServices()
    {   
        var services = await database.Get<Service>(databaseName, serviceCollectionName);
        return services;
    }
    
    public async Task<Service> Add(string name, string? hostName, string? containerPortNumber, string? hostPortNumber, string? dockerImageUrl)
    {
        var service = new Service(name, hostName, containerPortNumber, hostPortNumber, dockerImageUrl);

        await database.Add(databaseName, serviceCollectionName, service);

        using var topic = await messageBroker.CreateTopic("Events");
        var message = JsonSerializer.Serialize(new ServiceCreatedMessage(service.ServiceName));
        await topic.Publish("events.service.created", message);

        return service;
    }

    public async Task<Service?> GetService(string name)
    {
        var services = await database.Get<Service>(databaseName, serviceCollectionName, s => s.ServiceName == name);
        return services.SingleOrDefault();
    }

    public async Task Delete(string serviceName)
    {
        var service = await GetService(serviceName);
        if (service != null)
        {
            await database.Delete<Service>(databaseName, serviceCollectionName, s => s.ServiceName == serviceName);
            using var topic = await messageBroker.CreateTopic("Events");
            var message = JsonSerializer.Serialize(new ServiceDeletedMessage(service));
            await topic.Publish("events.service.deleted", message);
        }
    }
}

