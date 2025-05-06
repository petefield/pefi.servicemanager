using Octokit.Webhooks.Events;
using Octokit.Webhooks;
using Octokit.Webhooks.Events.Package;

namespace pefi.servicemanager;
public sealed class ProcessPackageWebhookProcessor(ILogger<ProcessPackageWebhookProcessor> logger) : WebhookEventProcessor
{
    protected override Task ProcessPackageWebhookAsync(WebhookHeaders headers, PackageEvent ProcessPackageWebhookAsync, PackageAction action)
    {
        //stop existing docker container
        //remove existin docker container
        //pull new docker image
        //run new docker image

        logger.LogInformation("Received package webhook: {HookId}", headers.HookId);
        logger.LogInformation("Action: {Action}", action);
        logger.LogInformation("Package: {Package}", ProcessPackageWebhookAsync.Package.Name);

        return Task.CompletedTask;
    }
}
