using Octokit.Webhooks;
using Octokit.Webhooks.AspNetCore;
using pefi.servicemanager;
using pefi.servicemanager.Docker;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddConsole();

builder.Services.AddSingleton<WebhookEventProcessor, ProcessRegistryPackageWebhookProcessor>();
builder.Services.AddSingleton<IDockerManager, DockerManager>();
builder.Services.AddSingleton<IServiceRepository, ServiceRepository>();

var app = builder.Build();


app.UseRouting()
    .UseEndpoints(endpoints => endpoints.MapGitHubWebhooks("service-manager/newpackage"));

app.MapGet("/services", async (IServiceRepository serviceRepository) =>
{
    return await serviceRepository.GetServices();
});

app.Run();
