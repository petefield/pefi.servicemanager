using dnsimple;
using MongoDB.Driver;
using pefi.Rabbit;
using System.Xml.Linq;

namespace pefi.servicemanager
{
    public class ServiceRepository : IServiceRepository
    {
        public ServiceRepository(ILogger<ServiceRepository> logger)
        {
            Logger = logger;
        }

        public List<ServiceDescription> Services { 
            get {
                var connectionString = "mongodb://192.168.0.5:27017"; // Default Mongo URI
                var client = new MongoClient(connectionString);
                var database = client.GetDatabase("testdb");
                var collection = database.GetCollection<ServiceDescription>("services");


                var allPeople = collection.Find(_ => true).ToList();

                return allPeople;
            } 
        }
        public ILogger<ServiceRepository> Logger { get; }

        public async Task< ServiceDescription>  Add(string Name, string? hostName, string? containerPortNumber, string? hostPortNumber)
        {
            var connectionString = "mongodb://192.168.0.5:27017"; // Default Mongo URI
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase("testdb");
            var collection = database.GetCollection<ServiceDescription>("services");

            var service = new ServiceDescription(Name, hostName, containerPortNumber, hostPortNumber);


            collection.InsertOne(service);


            using var messageBroker = await pefi.Rabbit.MessageBroker.Create("192.168.0.5", "username", "password");
            using var topic = await messageBroker.CreateTopic("Events");


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
