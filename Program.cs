using Octokit.Webhooks;
using Octokit.Webhooks.AspNetCore;
using pefi.servicemanager;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddConsole();

builder.Services.AddSingleton<WebhookEventProcessor, ProcessRegistryPackageWebhookProcessor>();
builder.Services.AddSingleton<IDockerManager, DockerManager>();
builder.Services.AddSingleton<IServiceRepository, ServiceRepository>();

var app = builder.Build();

var x= app.Services.GetService<IServiceRepository>();
await x.Add("pefi.home", "test", "8080", "5551");
await x.Add("pefi.dynamicdns", null, null, null);
app.UseRouting()
    .UseEndpoints(endpoints => endpoints.MapGitHubWebhooks("service-manager/newpackage"));



app.Run();
