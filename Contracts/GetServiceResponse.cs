namespace pefi.servicemanager.Contracts;

public record GetServiceResponse( string ServiceName, string? HostName, string? ContainerPortNumber, string? HostPortNumber);
