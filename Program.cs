using Octokit.Webhooks;
using Octokit.Webhooks.AspNetCore;
using pefi.Rabbit;
using pefi.servicemanager;
using pefi.servicemanager.Docker;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddConsole();

builder.Services.AddSingleton<WebhookEventProcessor, ProcessRegistryPackageWebhookProcessor>();
builder.Services.AddSingleton<IDockerManager, DockerManager>();
builder.Services.AddSingleton<IServiceRepository, ServiceRepository>();
builder.Services.AddSingleton<IMessageBroker>( sp => new MessageBroker("192.168.0.5", "username", "password"));
builder.Services.AddSingleton<IDataStore,  MongoDatastore>();
var app = builder.Build();


app.UseRouting()
    .UseEndpoints(endpoints => endpoints.MapGitHubWebhooks("service-manager/newpackage"));

app.MapGet("/services", async (IServiceRepository serviceRepository) =>
{
    return await serviceRepository.GetServices();
});

app.MapPost("/services",async (IServiceRepository serviceRepository, ServiceDescription s) =>
{
    return await serviceRepository.Add(s.ServiceName, s.HostName, s.ContainerPortNumber, s.HostPortNumber);
});

app.Run();
