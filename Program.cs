using Octokit.Webhooks;
using Octokit.Webhooks.AspNetCore;
using pefi.servicemanager;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<WebhookEventProcessor, ProcessPackageWebhookProcessor>();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseRouting()
    .UseEndpoints(endpoints => endpoints.MapGitHubWebhooks("service-manager/newpackage"));

app.Run();
