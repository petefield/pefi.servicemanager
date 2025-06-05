using dnsimple;
using MongoDB.Driver;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;
using Octokit.Webhooks;
using Octokit.Webhooks.AspNetCore;
using pefi.observability;
using pefi.Rabbit;
using pefi.servicemanager;
using pefi.servicemanager.Contracts;
using pefi.servicemanager.Docker;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddPefiObservability("http://192.168.0.5:4317", t=> t
    .AddSource("MongoDB.Driver.Core.Extensions.DiagnosticSources")
    .AddRabbitMQInstrumentation());

builder.Logging.AddPefiLogging();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<WebhookEventProcessor, ProcessRegistryPackageWebhookProcessor>();
builder.Services.AddSingleton<IDockerManager, DockerManager>();
builder.Services.AddSingleton<IServiceRepository, ServiceRepository>();
builder.Services.AddSingleton<IMessageBroker>( sp => new MessageBroker("192.168.0.5", "username", "password"));
builder.Services.AddSingleton<IDataStore,  MongoDatastore>();
builder.Services.AddSingleton<IMongoClient>(_ => {

    var clientSettings = MongoClientSettings.FromConnectionString("mongodb://192.168.0.5:27017");
    clientSettings.ClusterConfigurator = cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber());
    var mongoClient = new MongoClient(clientSettings);
    return mongoClient;
});
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "allow-all",
        policy =>
        {
            policy.AllowAnyHeader();
            policy.AllowAnyOrigin();
            policy.AllowAnyMethod();
        });
});

var app = builder.Build();

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();
app.UseRouting().UseEndpoints(endpoints => endpoints.MapGitHubWebhooks("service-manager/newpackage"));


app.MapGet("/services", async (IServiceRepository serviceRepository) =>
{
    var result = await serviceRepository.GetServices();
    return result.Select(x => new GetServiceResponse(x.ServiceName, x.HostName, x.ContainerPortNumber, x.HostPortNumber));
})
    .RequireCors("allow-all")
    .WithName("Get All Services")
    .WithOpenApi();

app.MapPost("/services", async (IServiceRepository serviceRepository, CreateServiceRequest s) =>
{
    var result = await serviceRepository.Add(s.ServiceName, s.HostName, s.ContainerPortNumber, s.HostPortNumber);
    return new CreateServiceResponse(s.ServiceName, s.HostName, s.ContainerPortNumber, s.HostPortNumber);
})
    .WithName("Create Service All Services")
    .WithOpenApi();

app.Run();
