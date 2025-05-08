
namespace pefi.servicemanager
{
    public interface IServiceRepository
    {
        Task<ServiceDescription?> GetService(string Name);

        Task<IEnumerable<ServiceDescription>> GetServices();

        Task<ServiceDescription> Add(string Name, string? hostName, string? containerPortNumber, string? hostPortNumber);
    }
}