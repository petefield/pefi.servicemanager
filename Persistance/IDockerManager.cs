using Docker.DotNet.Models;

namespace pefi.servicemanager.Docker
{
    public interface IDockerManager
    {
        Task<ContainerListResponse?> CreateContainer(string packageUrl, string packageName, string? containerPortNumber, string? hostPortNumber);
        Task CreateImage(string packageUrl);
        Task DeleteImage(string packageUrl);
        Task<ContainerListResponse?> GetContainer(string name);

        Task<ContainerListResponse?> GetContainerFromImageUrl(string packageUrl);
        Task<ImagesListResponse?> GetImageFromImageUrl(string packageUrl);

        Task StopContainer(string containerId);
        Task StartContainer(string containerId);

        Task RemoveContainer(string containerId);

    }
}