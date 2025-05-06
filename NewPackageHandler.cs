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

        var url = evt.Package.PackageVersion.PackageUrl;
        logger.LogInformation("Updated Image :  {image}", url);

        DockerClient client = new DockerClientConfiguration(
            new Uri("unix:///var/run/docker.sock"))
             .CreateClient();

        IList<ContainerListResponse> containers = await client.Containers.ListContainersAsync(
            new ContainersListParameters()
            {
                Limit = 100
            });

        foreach(var c in containers)
        {
            logger.LogInformation("Container :  {container}", c.Image);
        }

        var container = containers.First(c => c.Image == url);

        await client.Containers.StopContainerAsync(container.ID, new ContainerStopParameters());

        await client.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters());
        
       

        await client.Images.DeleteImageAsync(url, new ImageDeleteParameters()
        {
            Force = true,
        });

        await client.Images.CreateImageAsync(new ImagesCreateParameters()
        {
            FromImage = url,
            Tag = "latest"
        }, 
        new AuthConfig(),
        new Progress<JSONMessage>());


        await client.Containers.CreateContainerAsync(new CreateContainerParameters()
        {
            Image = url,
            ExposedPorts = new Dictionary<string, EmptyStruct>
            {
                { "80", new EmptyStruct() }
            },
            HostConfig = new HostConfig
            {
                PortBindings = new Dictionary<string, IList<PortBinding>>
                {
                    { "80", new List<PortBinding> { new PortBinding { HostPort = "80" } } }
                }
            }
        });

    }
}
