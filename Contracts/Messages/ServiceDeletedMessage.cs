using OpenTelemetry.Instrumentation.AspNetCore;
using pefi.servicemanager.Models;

namespace pefi.servicemanager.Contracts.Messages
{
    public record ServiceDeletedMessage(Service Service);

}
