using MongoDB.Driver;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;
using pefi.Rabbit;

namespace pefi.servicemanager.Persistance
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPeFiPersistance(this IServiceCollection services, string connectionString)
        {
            services.AddSingleton<IDataStore, MongoDatastore>();


            services.AddSingleton<IMongoClient>(_ =>
            {
                var clientSettings = MongoClientSettings.FromConnectionString(connectionString);
                clientSettings.ClusterConfigurator = cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber());
                return new MongoClient(clientSettings);
            });

            return services;

        }



        public static IServiceCollection AddPeFiMessaging(this IServiceCollection services, string address, string username, string password)
            => services.AddSingleton<IMessageBroker>(sp => new MessageBroker(address, username, password));


    }

}
