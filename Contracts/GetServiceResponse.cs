using pefi.servicemanager.Models;

namespace pefi.servicemanager.Contracts;

public record GetServiceResponse(
    string ServiceName, 
    string? HostName, 
    string? ContainerPortNumber, 
    string? HostPortNumber,
    string? DockerImageUrl,
    string? NetworkName,
    Dictionary<string, string>? EnvironmentVariables
)

{
    public static GetServiceResponse From(Service service) => new(
        service.ServiceName, 
        service.HostName, 
        service.ContainerPortNumber, 
        service.HostPortNumber, 
        service.DockerImageUrl,
        service.NetworkName,
        service.EnvironmentVariables);
    }