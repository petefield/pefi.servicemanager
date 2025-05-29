using Octokit.Webhooks;
using Octokit.Webhooks.AspNetCore;
using pefi.Rabbit;
using pefi.servicemanager;
using pefi.servicemanager.Contracts;
using pefi.servicemanager.Docker;
using pefi.observability;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddPefiObservability("http://192.168.0.5:4317");
builder.Logging.AddPefiLogging();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<WebhookEventProcessor, ProcessRegistryPackageWebhookProcessor>();
builder.Services.AddSingleton<IDockerManager, DockerManager>();
builder.Services.AddSingleton<IServiceRepository, ServiceRepository>();
builder.Services.AddSingleton<IMessageBroker>( sp => new MessageBroker("192.168.0.5", "username", "password"));
builder.Services.AddSingleton<IDataStore,  MongoDatastore>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting()
    .UseEndpoints(endpoints => endpoints.MapGitHubWebhooks("service-manager/newpackage"));

app.MapGet("/services", async (IServiceRepository serviceRepository) =>
{
    var result = await serviceRepository.GetServices();
    return result.Select(x => new GetServiceResponse(x.ServiceName, x.HostName, x.ContainerPortNumber, x.HostPortNumber));
})
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
