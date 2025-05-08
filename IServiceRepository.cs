
namespace pefi.servicemanager
{
    public interface IServiceRepository
    {
        ServiceDescription? GetService(string Name);

        Task<ServiceDescription> Add(string Name, string hostName, string containerPortNumber, string hostPortNumber);
    }
}