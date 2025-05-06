using Octokit.Webhooks.Events;
using Octokit.Webhooks;
using Octokit.Webhooks.Events.Package;

namespace pefi.servicemanager;
public sealed class ProcessPackageWebhookProcessor : WebhookEventProcessor
{
    protected override Task ProcessPackageWebhookAsync(WebhookHeaders headers, PackageEvent ProcessPackageWebhookAsync, PackageAction action)
    {
        //stop existing docker container
        //remove existin docker container
        //pull new docker image
        //run new docker image

        Console.WriteLine($"Received package webhook: {headers.HookId}");
        Console.WriteLine($"Action: {action}");
        Console.WriteLine($"Package: {ProcessPackageWebhookAsync.Package.Name}");
        

        return Task.CompletedTask;
    }
}
