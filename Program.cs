using Octokit.Webhooks;
using Octokit.Webhooks.AspNetCore;
using pefi.servicemanager;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<WebhookEventProcessor, ProcessRegistryPackageWebhookProcessor>();
builder.Services.AddSingleton<IDockerManager, DockerManager>();

builder.Logging.AddConsole();

var app = builder.Build();

app.UseRouting()
    .UseEndpoints(endpoints => endpoints.MapGitHubWebhooks("service-manager/newpackage"));

app.Run();
