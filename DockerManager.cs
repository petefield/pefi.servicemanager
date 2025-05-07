using Docker.DotNet;
using Docker.DotNet.Models;

namespace pefi.servicemanager;

public class DockerManager : IDockerManager
{
    DockerClient _dockerClient;
    ILogger<DockerManager> _logger;

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
        var createContainerResponse = await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters()
        {
            Image = packageUrl,
            Name = packageName
        });

        var containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters { });

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
