using pefi.servicemanager.Models;

namespace pefi.servicemanager.Tests.Models;

/// <summary>
/// Unit tests for the <see cref="Service"/> domain model.
///
/// These tests confirm that the primary constructor assigns all properties
/// correctly and that optional fields default to null when omitted.
/// </summary>
public class ServiceModelTests
{
    /// <summary>
    /// Constructor should assign every property from the supplied arguments.
    /// </summary>
    [Fact]
    public void Service_Constructor_AssignsAllProperties()
    {
        var envVars = new Dictionary<string, string> { { "KEY", "VALUE" } };

        var service = new Service(
            "my-svc",
            "my-host",
            "8080",
            "80",
            "ghcr.io/org/img:latest",
            "bridge-net",
            envVars);

        Assert.Equal("my-svc", service.ServiceName);
        Assert.Equal("my-host", service.HostName);
        Assert.Equal("8080", service.ContainerPortNumber);
        Assert.Equal("80", service.HostPortNumber);
        Assert.Equal("ghcr.io/org/img:latest", service.DockerImageUrl);
        Assert.Equal("bridge-net", service.NetworkName);
        Assert.Equal(envVars, service.EnvironmentVariables);
    }

    /// <summary>
    /// When optional parameters are omitted the corresponding properties
    /// should be null.
    /// </summary>
    [Fact]
    public void Service_Constructor_WithOnlyRequiredFields_NullablePropertiesAreNull()
    {
        var service = new Service("bare-svc", null, null, null, null, null);

        Assert.Equal("bare-svc", service.ServiceName);
        Assert.Null(service.HostName);
        Assert.Null(service.ContainerPortNumber);
        Assert.Null(service.HostPortNumber);
        Assert.Null(service.DockerImageUrl);
        Assert.Null(service.NetworkName);
        Assert.Null(service.EnvironmentVariables);
    }

    /// <summary>
    /// EnvironmentVariables defaults to null when not supplied (it carries a
    /// default value of null in the primary constructor).
    /// </summary>
    [Fact]
    public void Service_Constructor_WithoutEnvironmentVariables_EnvironmentVariablesIsNull()
    {
        // The default value of the environmentVariables parameter is null.
        var service = new Service("svc", "host", "80", "8080", "image", "net");

        Assert.Null(service.EnvironmentVariables);
    }

    /// <summary>
    /// An empty environment-variables dictionary should be stored as-is
    /// (not converted to null).
    /// </summary>
    [Fact]
    public void Service_Constructor_WithEmptyEnvironmentVariables_StoresEmptyDictionary()
    {
        var service = new Service("svc", null, null, null, null, null,
            new Dictionary<string, string>());

        Assert.NotNull(service.EnvironmentVariables);
        Assert.Empty(service.EnvironmentVariables);
    }

    /// <summary>
    /// The Id property should be the default ObjectId (all zeros) for a new
    /// Service that has not been persisted yet.
    /// </summary>
    [Fact]
    public void Service_NewInstance_IdIsDefaultObjectId()
    {
        var service = new Service("svc", null, null, null, null, null);

        Assert.Equal(MongoDB.Bson.ObjectId.Empty, service.Id);
    }

    /// <summary>
    /// Properties should be mutable after construction (needed by MongoDB
    /// BSON serialisation which may set properties after deserialisation).
    /// </summary>
    [Fact]
    public void Service_Properties_AreMutable()
    {
        var service = new Service("svc", null, null, null, null, null);

        service.ServiceName = "updated-svc";
        service.HostName = "new-host";
        service.ContainerPortNumber = "9090";
        service.HostPortNumber = "9091";
        service.DockerImageUrl = "new-image";
        service.NetworkName = "new-net";
        service.EnvironmentVariables = new Dictionary<string, string> { { "A", "B" } };

        Assert.Equal("updated-svc", service.ServiceName);
        Assert.Equal("new-host", service.HostName);
        Assert.Equal("9090", service.ContainerPortNumber);
        Assert.Equal("9091", service.HostPortNumber);
        Assert.Equal("new-image", service.DockerImageUrl);
        Assert.Equal("new-net", service.NetworkName);
        Assert.Equal("B", service.EnvironmentVariables["A"]);
    }
}
