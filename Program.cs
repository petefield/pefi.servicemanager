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
using pefi.servicemanager.Services;
using pefi.servicemanager.Persistance;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddPefiObservability("http://192.168.1.86:4317", t=> t
    .AddSource("MongoDB.Driver.Core.Extensions.DiagnosticSources")
    .AddRabbitMQInstrumentation());

builder.Logging.AddPefiLogging();
builder.Services.AddPeFiPersistance("mongodb://192.168.1.86:27017");
builder.Services.AddPeFiMessaging("192.168.1.86", "username", "password");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<WebhookEventProcessor, ProcessRegistryPackageWebhookProcessor>();
builder.Services.AddSingleton<IDockerManager, DockerManager>();
builder.Services.AddSingleton<IServiceRepository, ServiceRepository>();
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

app.UseSwagger();
app.UseSwaggerUI();
var a = app.UseRouting();
app.UseCors();
a.UseEndpoints(endpoints => endpoints.MapGitHubWebhooks("service-manager/newpackage"));


app.MapGet("/services", async (IServiceRepository serviceRepository) =>
{
    var result = await serviceRepository.GetServices();

    return result is null
        ? Results.Ok(Enumerable.Empty<GetServiceResponse>())
        : Results.Ok(result.Select(service => GetServiceResponse.From(service)));
}).RequireCors("allow-all")
  .WithName("Get All Services")
  .Produces<IEnumerable<GetServiceResponse>>(200)
  .WithOpenApi();

app.MapGet("/services/{serviceName}", async (string serviceName, IServiceRepository serviceRepository) =>
{
    var result = await serviceRepository.GetService(serviceName);

    return (result is null)
        ? Results.NotFound()
        : Results.Ok(GetServiceResponse.From(result));

}).RequireCors("allow-all")
  .WithName("Get Service By Name")
  .Produces<GetServiceResponse>(200)
  .WithOpenApi();

app.MapPost("/services", async (IServiceRepository serviceRepository, CreateServiceRequest s) =>
{
    var result = await serviceRepository.Add(s.ServiceName, s.HostName, s.ContainerPortNumber, s.HostPortNumber, s.DockerImageUrl);
    return Results.Created(string.Empty, CreateServiceResponse.From(result));

}).RequireCors("allow-all")
  .WithName("Create Service")
  .Produces<IEnumerable<CreateServiceResponse>>(201)
  .WithOpenApi();

app.MapDelete("/services/{serviceName}", async (IServiceRepository serviceRepository, string serviceName) =>
{
    await serviceRepository.Delete(serviceName);
    return Results.NoContent();

}).RequireCors("allow-all")
  .WithName("Delete Service")
  .Produces(204)
  .WithOpenApi();

app.Run();
