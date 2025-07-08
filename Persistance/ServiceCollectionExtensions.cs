using Docker.DotNet.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;
using pefi.Rabbit;

namespace pefi.servicemanager.Persistance
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPeFiPersistance(this IServiceCollection services, Action<MongoDbSettings> configureOptions) 
        {
            services.Configure(configureOptions);

            services.AddSingleton<IDataStore, MongoDatastore>();

            services.AddSingleton<IMongoClient>(provider =>
            {
                var options = provider.GetRequiredService<IOptions<MongoDbSettings>>().Value;
                var connectionString = options.ConnectionString;

                var clientSettings = MongoClientSettings.FromConnectionString(connectionString);
                clientSettings.ClusterConfigurator = cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber());
                return new MongoClient(clientSettings);
            });

            return services;
        }

        public static IServiceCollection AddPeFiPersistance(this IServiceCollection services, string connectionString)
        {
            return AddPeFiPersistance(services, _ => new MongoDbSettings(connectionString));

        }

        public class MessagingConfig()
        {
            public string Address { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }

        }
        public static IServiceCollection AddPeFiMessaging(this IServiceCollection services, Action<MessagingConfig> configureOptions)
        {
            services.Configure(configureOptions);

            return services.AddSingleton<IMessageBroker>(sp => {

                var options = sp.GetRequiredService<IOptions<MessagingConfig>>().Value;
                return new MessageBroker(options.Address, options.Username, options.Password);

            } );

        }
        public static IServiceCollection AddPeFiMessaging(this IServiceCollection services, string address, string username, string password)
            => services.AddSingleton<IMessageBroker>(sp => new MessageBroker(address, username, password));


    }

}
