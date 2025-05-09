namespace pefi.servicemanager.Contracts;

public record CreateServiceRequest( string ServiceName, string? HostName, string? ContainerPortNumber, string? HostPortNumber);
