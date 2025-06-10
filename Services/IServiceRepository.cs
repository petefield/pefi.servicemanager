using pefi.servicemanager.Models;

namespace pefi.servicemanager.Services
{
    public interface IServiceRepository
    {
        Task<Service?> GetService(string Name);

        Task<IEnumerable<Service>> GetServices();

        Task<Service> Add(string Name, string? hostName, string? containerPortNumber, string? hostPortNumber, string? dockerImageUrl);

        Task Delete(string serviceName);

    }
}