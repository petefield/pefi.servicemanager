using System.ComponentModel.DataAnnotations;

namespace pefi.servicemanager.Contracts;

public record CreateServiceRequest(
    [Required]
    string ServiceName, 
    string? HostName, 
    string? ContainerPortNumber, 
    string? HostPortNumber, 
    string? DockerImageUrl
    );
