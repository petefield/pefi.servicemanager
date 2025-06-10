using pefi.servicemanager.Models;
using System.ComponentModel.DataAnnotations;

namespace pefi.servicemanager.Contracts;

public record CreateServiceResponse(
    [Required]
    string ServiceName,
    string? HostName,
    string? ContainerPortNumber,
    string? HostPortNumber,
    string? DockerImageUrl)

{
    public static CreateServiceResponse From(Service service) => new(service.ServiceName, service.HostName, service.ContainerPortNumber, service.HostPortNumber, service.DockerImageUrl);
};
