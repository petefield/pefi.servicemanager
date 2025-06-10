using pefi.servicemanager.Models;

namespace pefi.servicemanager.Contracts;

public record GetServiceResponse(
    string ServiceName, 
    string? HostName, 
    string? ContainerPortNumber, 
    string? HostPortNumber,
    string? DockerImageUrl
)
{
    public static GetServiceResponse From(Service service) => new(
        service.ServiceName, 
        service.HostName, 
        service.ContainerPortNumber, 
        service.HostPortNumber, 
        service.DockerImageUrl);
    }