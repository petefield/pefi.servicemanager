using Octokit.Webhooks.Events;
using Octokit.Webhooks;
using Octokit.Webhooks.Events.Package;
using Docker.DotNet;
using Docker.DotNet.Models;
using Octokit.Webhooks.Events.RegistryPackage;

namespace pefi.servicemanager;

public sealed class ProcessPackageWebhookProcessor(ILogger<ProcessPackageWebhookProcessor> logger) : WebhookEventProcessor
{
    protected async override Task ProcessRegistryPackageWebhookAsync(WebhookHeaders headers, RegistryPackageEvent evt, RegistryPackageAction action)
    {
        //stop existing docker container
        //remove existin docker container
        //pull new docker image
        //run new docker image


        logger.LogInformation("Package {packageName}:{tag}", evt.Package.Name, evt.Package.PackageVersion.PackageUrl );

        DockerClient client = new DockerClientConfiguration(
            new Uri("unix:///var/run/docker.sock"))
             .CreateClient();

        IList<ContainerListResponse> containers = await client.Containers.ListContainersAsync(
            new ContainersListParameters()
            {
                Limit = 100
            });

        foreach (var container in containers)
        {
            logger.LogInformation("container {container}", container.Image);
        }
    }
}
