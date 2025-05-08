namespace pefi.servicemanager
{
    public record ServiceDescription(string ServiceName, string HostName, string? ContainerPortNumber, string? HostPortNumber);
}
