using pefi.servicemanager.Models;

namespace pefi.servicemanager.Services
{
    public interface IServiceRepository
    {
        Task<Service?> GetService(string Name);

        Task<IEnumerable<Service>> GetServices();

        Task<Service> Add(string Name, string? hostName, string? containerPortNumber, string? hostPortNumber, string? dockerImageUrl, string? networkName, Dictionary<string, string>? EnvironmentVariables = null );

        Task Delete(string serviceName);

    }
}