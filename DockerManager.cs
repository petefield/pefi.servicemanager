using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging.Configuration;

namespace pefi.servicemanager;

public class DockerManager : IDockerManager
{
    DockerClient _dockerClient;
    ILogger<DockerManager> _logger;

    public Dictionary<string, int> ports = new()
    {        
        { "pefi.home", 5551 }
    };

    public DockerManager(ILogger<DockerManager> logger)
    {
        _logger = logger;
        _dockerClient = new DockerClientConfiguration(
            new Uri("http://host.docker.internal:2375"))
            .CreateClient();
    }

    public async Task<ContainerListResponse?> GetContainerFromImageUrl(string packageUrl)
    {
        IList<ContainerListResponse> containers = await _dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters()
            {
                All = true
            });

        var container = containers.FirstOrDefault(c => c.Image == packageUrl);

        return container;
    }

    public async Task<ImagesListResponse?> GetImageFromImageUrl(string packageUrl)
    {
        var images = await _dockerClient.Images.ListImagesAsync(new ImagesListParameters()
        {
            All = true
        });

        var image = images.FirstOrDefault(i => i.RepoTags != null && i.RepoTags.Contains(packageUrl));

        return image;
    }

    public async Task DeleteImage(string packageUrl)
    {
        await _dockerClient.Images.DeleteImageAsync(packageUrl, new ImageDeleteParameters() { Force = true });
    }

    public async Task CreateImage(string packageUrl)
    {
        await _dockerClient.Images.CreateImageAsync(new ImagesCreateParameters()
        {
            FromImage = packageUrl,
            Tag = "latest"
        },
        new AuthConfig(),
        new Progress<JSONMessage>());
    }

    public async Task<ContainerListResponse?> CreateContainer(string packageUrl, string packageName)
    {
        var hostPort = ports.TryGetValue(packageName, out var portNo)
            ? portNo.ToString()
            : null;

        if (hostPort == null)
        {
            _logger.LogWarning("No exposed ports found for container: {container_name}", packageName);
        }
        else {
            _logger.LogWarning("exposing port {porNumber}", portNo);
        }


        var createContainerResponse = await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters()
        {
            Image = packageUrl,
            Name = packageName,
            HostConfig = new HostConfig()
            {
               PortBindings = new Dictionary<string, IList<PortBinding>>

                {

                    { "8080", new List<PortBinding> { new PortBinding() { HostPort = hostPort } } }

                }
            }
        });

        var containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = true });

        _logger.LogInformation(packageName + " created with ID: " + createContainerResponse.ID + " from image: " + packageUrl + " with name: " + packageName);

        var newContainer = containers.Single(c => c.ID == createContainerResponse.ID);

        return newContainer;
    }

    public async Task StopContainer(string containerId)
    {
        await _dockerClient.Containers.StopContainerAsync(containerId, new ContainerStopParameters());
    }

    public async Task StartContainer(string containerId)
    {
        await _dockerClient.Containers.StartContainerAsync(containerId, new ContainerStartParameters());
    }

    public async Task RemoveContainer(string containerId)
    {
        await _dockerClient.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters());
    }
}
