using MongoDB.Driver;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;

namespace pefi.servicemanager
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPeFiPersistance(this IServiceCollection services, string connectionString)
        {
            services.AddSingleton<IDataStore, MongoDatastore>();

            services.AddSingleton<IMongoClient>(_ => {
                var clientSettings = MongoClientSettings.FromConnectionString(connectionString);
                clientSettings.ClusterConfigurator = cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber());
                return new MongoClient(clientSettings);
            });

            return services;

        }
    }
}
