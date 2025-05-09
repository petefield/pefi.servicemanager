namespace pefi.servicemanager.Contracts;

public record CreateServiceResponse( string ServiceName, string? HostName, string? ContainerPortNumber, string? HostPortNumber);
