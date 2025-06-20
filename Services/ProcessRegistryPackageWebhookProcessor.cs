﻿using Octokit.Webhooks.Events;
using Octokit.Webhooks;
using Octokit.Webhooks.Events.RegistryPackage;
using pefi.servicemanager.Docker;

namespace pefi.servicemanager.Services;

public sealed class ProcessRegistryPackageWebhookProcessor(IDockerManager dockerManager, IServiceRepository serviceRepository, ILogger<ProcessRegistryPackageWebhookProcessor> logger) : WebhookEventProcessor
{
    protected async override Task ProcessRegistryPackageWebhookAsync(WebhookHeaders headers, RegistryPackageEvent evt, RegistryPackageAction action)
    {
        var packageUrl = evt.Package.PackageVersion!.PackageUrl;
        var packageName = evt.Package.Name;

        var service = await serviceRepository.GetService(packageName);

        if (service is null)
        {
            logger.LogError("Service not found: {service_name}", packageName);
            throw new Exception($"Service not found: {packageName}");
        }

        if (packageUrl == null)
        {
            logger.LogError("Package URL is null");
            throw new Exception("Package URL is null");
        }

        logger.LogInformation("Service {service_name} is being updated.", service.ServiceName);


        var currentImage = await dockerManager.GetImageFromImageUrl(packageUrl);
        var currentContainer = await dockerManager.GetContainerFromImageUrl(packageUrl);

        if (currentImage != null)
        {
            logger.LogInformation("Removing image: {image_id}", currentImage.ID);
            await dockerManager.DeleteImage(packageUrl); // Wait for the image to be deleted before proceeding
        }

        logger.LogInformation("Pulling image: {image_url}", packageUrl);
        await dockerManager.CreateImage(packageUrl);

        if (currentContainer != null)
        {
            logger.LogInformation("Stopping Container: {container_name}", packageName);
            await dockerManager.StopContainer(currentContainer.ID); // Ensure the container is stopped before removing it

            logger.LogInformation("Removing Container: {container_name}", packageName);
            await dockerManager.RemoveContainer(currentContainer.ID);
        }

        logger.LogInformation("Creating container '{packageName}' from image '{image_url}'", packageName, packageUrl);
        var newContainer = await dockerManager.CreateContainer(packageUrl, packageName, service.ContainerPortNumber, service.HostPortNumber);

        if (newContainer == null)
        {
            logger.LogError("Failed to create container from image: {image_url}", packageUrl);
            throw new Exception("Failed to create container");
        }

        logger.LogInformation("Starting container {container_name}", packageName);
        await dockerManager.StartContainer(newContainer.ID);
    }

}
