using Octokit.Webhooks.Events;
using Octokit.Webhooks;
using Octokit.Webhooks.Events.Package;
using Docker.DotNet;
using Docker.DotNet.Models;
using Octokit.Webhooks.Events.RegistryPackage;

namespace pefi.servicemanager;

public sealed class ProcessPackageWebhookProcessor(ILogger<ProcessPackageWebhookProcessor> logger) : WebhookEventProcessor
{
    protected async override Task ProcessRegistryPackageWebhookAsync(WebhookHeaders headers, RegistryPackageEvent ProcessPackageWebhookAsync, RegistryPackageAction action)
    {
        //stop existing docker container
        //remove existin docker container
        //pull new docker image
        //run new docker image


        logger.LogInformation("Received package webhook: {HookId}", headers.HookId);
        logger.LogInformation("Action: {action}", action);
        logger.LogInformation("Package: {Package}", ProcessPackageWebhookAsync.Package.Name);
        DockerClient client = new DockerClientConfiguration(
            new Uri("unix:///var/run/docker.sock"))
             .CreateClient();

        IList<ContainerListResponse> containers = await client.Containers.ListContainersAsync(
            new ContainersListParameters()
            {
                Limit = 10,
            });

        foreach (var container in containers)
        {
            logger.LogInformation("container {container}", container.Names.First());
        }
    }
}
