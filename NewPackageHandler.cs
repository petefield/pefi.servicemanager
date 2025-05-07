using Octokit.Webhooks.Events;
using Octokit.Webhooks;
using Octokit.Webhooks.Events.RegistryPackage;

namespace pefi.servicemanager;

public sealed class ProcessRegistryPackageWebhookProcessor : WebhookEventProcessor
{
    IDockerManager _dockerManager;
    ILogger<ProcessRegistryPackageWebhookProcessor> _logger;

    public ProcessRegistryPackageWebhookProcessor(IDockerManager dockerManager, ILogger<ProcessRegistryPackageWebhookProcessor> logger)
    {
        _dockerManager = dockerManager;
        _logger = logger;
    }

    protected async override Task ProcessRegistryPackageWebhookAsync(WebhookHeaders headers, RegistryPackageEvent evt, RegistryPackageAction action)
    {
        var packageUrl = evt.Package.PackageVersion.PackageUrl;
        var packageName = evt.Package.Name;

        _logger.LogInformation("Updated Image :  {image}", packageUrl);

        var currentContainer = await _dockerManager.GetContainerFromImageUrl(packageUrl);

        if (currentContainer != null)
        {
            _logger.LogInformation("Stopping Container: {container_name}", currentContainer.Names);
            await _dockerManager.StopContainer(currentContainer.ID); // Ensure the container is stopped before removing it

            _logger.LogInformation("Removing Container: {container_name}", currentContainer.Names);
            await _dockerManager.RemoveContainer(currentContainer.ID);
        }

        var currentImage = await _dockerManager.GetImageFromImageUrl(packageUrl);

        if (currentImage != null)
        {
            _logger.LogInformation("Removing image: {image_id}", currentImage.ID);
            await _dockerManager.DeleteImage(packageUrl); // Wait for the image to be deleted before proceeding
        }

        _logger.LogInformation("Creating image: {image_url}", packageUrl);
         await _dockerManager.CreateImage(packageUrl);

        _logger.LogInformation("Creating container '{packageName}' from image '{image_url}'", packageName, packageUrl);
        var newContainer = await _dockerManager.CreateContainer(packageUrl, packageName);

        _logger.LogInformation("Starting container {container_name}", string.Join(" | ", newContainer.Names));
        await _dockerManager.StartContainer(newContainer.ID);
    }

}
