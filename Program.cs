using dnsimple;
using dnsimple.Services;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;

using OpenTelemetry.Trace;
using pefi.observability;
using pefi.servicemanager.Contracts;
using pefi.servicemanager.Docker;
using pefi.servicemanager.Models;
using pefi.servicemanager.Persistance;
using pefi.servicemanager.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddPefiObservability("http://192.168.1.86:4317", t=> t
    .AddSource("MongoDB.Driver.Core.Extensions.DiagnosticSources")
    .AddRabbitMQInstrumentation());

builder.Logging.AddPefiLogging();


builder.Services.AddPeFiPersistance(options =>
{
    options.ConnectionString = builder.Configuration.GetSection("Persistance").GetValue<string>("connectionstring") ?? "";
});

builder.Services.AddPeFiMessaging(options => {
    options.Username = builder.Configuration.GetSection("Messaging").GetValue<string>("username") ?? "";
    options.Password = builder.Configuration.GetSection("Messaging").GetValue<string>("password") ?? "";
    options.Address = builder.Configuration.GetSection("Messaging").GetValue<string>("address") ?? "";
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
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
app.UseRouting();
app.UseCors();

app.MapGet("/services", async (IServiceRepository serviceRepository) =>
{
    var result = await serviceRepository.GetServices();

    return result is null
        ? Results.Ok(Enumerable.Empty<GetServiceResponse>())
        : Results.Ok(result.Select(service => GetServiceResponse.From(service)));
})
  .RequireCors("allow-all")
  .WithName("Get All Services")
  .Produces<IEnumerable<GetServiceResponse>>(200)
  .WithOpenApi();

app.MapGet("/services/{serviceName}", async (string serviceName, IServiceRepository serviceRepository) =>
{
    var result = await serviceRepository.GetService(serviceName);

    return (result is null)
        ? Results.NotFound()
        : Results.Ok(GetServiceResponse.From(result));

})  
  .RequireCors("allow-all")
  .WithName("Get Service By Name")
  .Produces<GetServiceResponse>(200)
  .WithOpenApi();

app.MapPost("/services", async (ILogger<Program> logger, IDockerManager dckrMgr,IServiceRepository serviceRepository, CreateServiceRequest s) =>
{
    var currentContainer = await dckrMgr.GetContainer(s.ServiceName);

    if (currentContainer != null)
    {
        logger.LogInformation("Found existing container '{containerNAme}'", s.ServiceName);
        return Results.Conflict();
    }

    logger.LogInformation("Pulling image: {image_url}", s.DockerImageUrl);
    await dckrMgr.CreateImage(s.DockerImageUrl);

    logger.LogInformation("Creating container '{packageName}' from image '{image_url}'", s.ServiceName, s.DockerImageUrl);
    var newContainer = await dckrMgr.CreateContainer(s.DockerImageUrl, s.ServiceName, s.ContainerPortNumber, s.HostPortNumber, s.NetworkName, s.EnvironmentVariables);

    if (newContainer == null)
    {
        logger.LogError("Failed to create container from image: {image_url}", s.DockerImageUrl);
        throw new Exception("Failed to create container");
    }

    logger.LogInformation("Starting container {container_name}", s.ServiceName);

    try
    {
        await dckrMgr.StartContainer(newContainer.ID);
    }
    catch (Exception e)
    {
        logger.LogError(e,"Starting container failed {message}", e.Message);

    }
    var result = await serviceRepository.Add(s.ServiceName, s.HostName, s.ContainerPortNumber, s.HostPortNumber, s.DockerImageUrl, s.NetworkName, s.EnvironmentVariables);

    return Results.Created(string.Empty, CreateServiceResponse.From(result));

})
  .RequireCors("allow-all")
  .WithName("Create Service")
  .Produces<IEnumerable<CreateServiceResponse>>(201)
  .WithOpenApi();


app.MapPost("services/{serviceName}/update", async (string serviceName, ILogger<Program> logger, IDockerManager dckrMgr, IServiceRepository serviceRepository) =>
{

    var service = await serviceRepository.GetService(serviceName);

    if (service is null)
    {
        logger.LogError("Service not found: {service_name}", service.ServiceName);
        return Results.NotFound();
    }

    logger.LogInformation("Service {service_name} is being updated.", service.ServiceName);


    if (service.DockerImageUrl is null)
    {
        logger.LogError("Service {service_name} does not have a docker image path specified.", service.ServiceName);
        return Results.UnprocessableEntity();
    }


    logger.LogInformation("Pulling image: {image_url}", service.DockerImageUrl);
    await dckrMgr.CreateImage(service.DockerImageUrl);

    
    var container = await dckrMgr.GetContainer(service.ServiceName);

    if (container != null)
    {

        if (container.State == "running")
        {
            logger.LogInformation("Stopping Container: {container_name}", container.Names.First());
            await dckrMgr.StopContainer(container.ID); // Ensure the container is stopped before removing it
        }
        
        logger.LogInformation("Removing Container: {container_name}", service.ServiceName);
        await dckrMgr.RemoveContainer(container.ID);
    }

    logger.LogInformation("Creating container '{packageName}' from image '{image_url}'", service.ServiceName, service.DockerImageUrl);
    var newContainer = await dckrMgr.CreateContainer(service.DockerImageUrl, service.ServiceName, service.ContainerPortNumber, service.HostPortNumber, service.NetworkName);

    if (newContainer == null)
    {
        logger.LogError("Failed to create container from image: {image_url}", service.DockerImageUrl);
        throw new Exception("Failed to create container");
    }

    logger.LogInformation("Starting container {container_name}", service.ServiceName);
    await dckrMgr.StartContainer(newContainer.ID);
    return Results.NoContent();

}).RequireCors("allow-all")
  .WithName("Update Service")
  .Produces(204)
  .WithOpenApi();

app.MapPost("services/{serviceName}/restart", async (string serviceName, ILogger<Program> logger, IDockerManager dckrMgr, IServiceRepository serviceRepository) =>
{

    var service = await serviceRepository.GetService(serviceName);

    if (service is null)
    {
        logger.LogError("Service not found: {service_name}", service.ServiceName);
        return Results.NotFound();
    }

    logger.LogInformation("Service {service_name} is being restarted.", service.ServiceName);



    var container = await dckrMgr.GetContainer(service.ServiceName);

    if (container != null)
    {
        if (container.State == "running")
        {
            logger.LogInformation("Stopping Container: {container_name}", container.Names.First());
            await dckrMgr.StopContainer(container.ID); // Ensure the container is stopped before removing it
        }
            await dckrMgr.StartContainer(container.ID);

    }
    return Results.NoContent();

}).RequireCors("allow-all")
  .WithName("Restart Service")
  .Produces(204)
  .WithOpenApi();

app.MapDelete("/services/{serviceName}", async (ILogger<Program> logger, IDockerManager dckrMgr, IServiceRepository serviceRepository, string serviceName) =>
{
    try
    {
        await dckrMgr.StopContainer(serviceName);
        await dckrMgr.RemoveContainer(serviceName);
    }
    catch (Exception ex) 
    {
        logger.LogError(ex, "Unable to stop service while deleting {serviceName}", serviceName);
    }

    await serviceRepository.Delete(serviceName);
    return Results.NoContent();

})
  .RequireCors("allow-all")
  .WithName("Delete Service")
  .Produces(204)
  .WithOpenApi();

app.Run();
