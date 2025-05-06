using Octokit.Webhooks.Events;
using Octokit.Webhooks;
using Octokit.Webhooks.Events.Package;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace pefi.servicemanager;

public sealed class ProcessPackageWebhookProcessor(ILogger<ProcessPackageWebhookProcessor> logger) : WebhookEventProcessor
{
    protected async override Task ProcessPackageWebhookAsync(WebhookHeaders headers, PackageEvent ProcessPackageWebhookAsync, PackageAction action)
    {
        //stop existing docker container
        //remove existin docker container
        //pull new docker image
        //run new docker image

        logger.LogInformation("Received package webhook: {HookId}", headers.HookId);
        logger.LogInformation("Action: {Action}", action);
        logger.LogInformation("Package: {Package}", ProcessPackageWebhookAsync.Package.Name);
        
        DockerClient client = new DockerClientConfiguration()
             .CreateClient();

        IList<ContainerListResponse> containers = await client.Containers.ListContainersAsync(
            new ContainersListParameters()
            {
                Limit = 10,
            });
    }
}
