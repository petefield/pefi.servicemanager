using pefi.servicemanager.Contracts;
using pefi.servicemanager.Models;

namespace pefi.servicemanager.Tests.Contracts;

/// <summary>
/// Unit tests for the static <c>From</c> factory methods on the contract
/// response types.  These tests verify that every Service property is
/// correctly projected into the corresponding response record.
/// </summary>
public class ContractMappingTests
{
    // -------------------------------------------------------------------------
    // CreateServiceResponse
    // -------------------------------------------------------------------------

    /// <summary>
    /// CreateServiceResponse.From should map all fields from the source
    /// Service to the response record.
    /// </summary>
    [Fact]
    public void CreateServiceResponse_From_MapsAllProperties()
    {
        var service = new Service(
            "test-svc",
            "test-host",
            "80",
            "8080",
            "ghcr.io/org/image:latest",
            "bridge");

        var response = CreateServiceResponse.From(service);

        Assert.Equal(service.ServiceName, response.ServiceName);
        Assert.Equal(service.HostName, response.HostName);
        Assert.Equal(service.ContainerPortNumber, response.ContainerPortNumber);
        Assert.Equal(service.HostPortNumber, response.HostPortNumber);
        Assert.Equal(service.DockerImageUrl, response.DockerImageUrl);
    }

    /// <summary>
    /// CreateServiceResponse.From should handle null optional properties
    /// without throwing.
    /// </summary>
    [Fact]
    public void CreateServiceResponse_From_WithNullOptionalFields_MapsWithoutError()
    {
        var service = new Service("min-svc", null, null, null, null, null);

        var response = CreateServiceResponse.From(service);

        Assert.Equal("min-svc", response.ServiceName);
        Assert.Null(response.HostName);
        Assert.Null(response.ContainerPortNumber);
        Assert.Null(response.HostPortNumber);
        Assert.Null(response.DockerImageUrl);
    }

    // -------------------------------------------------------------------------
    // GetServiceResponse
    // -------------------------------------------------------------------------

    /// <summary>
    /// GetServiceResponse.From should map all fields, including NetworkName and
    /// EnvironmentVariables, from the source Service.
    /// </summary>
    [Fact]
    public void GetServiceResponse_From_MapsAllProperties()
    {
        var envVars = new Dictionary<string, string> { { "FOO", "bar" }, { "DB", "mongo" } };
        var service = new Service(
            "full-svc",
            "full-host",
            "443",
            "8443",
            "ghcr.io/org/full:1.0",
            "host-net",
            envVars);

        var response = GetServiceResponse.From(service);

        Assert.Equal(service.ServiceName, response.ServiceName);
        Assert.Equal(service.HostName, response.HostName);
        Assert.Equal(service.ContainerPortNumber, response.ContainerPortNumber);
        Assert.Equal(service.HostPortNumber, response.HostPortNumber);
        Assert.Equal(service.DockerImageUrl, response.DockerImageUrl);
        Assert.Equal(service.NetworkName, response.NetworkName);
        Assert.Equal(service.EnvironmentVariables, response.EnvironmentVariables);
    }

    /// <summary>
    /// GetServiceResponse.From should handle null optional properties
    /// (HostName, ports, image URL, network, env vars) without throwing.
    /// </summary>
    [Fact]
    public void GetServiceResponse_From_WithNullOptionalFields_MapsWithoutError()
    {
        var service = new Service("bare-svc", null, null, null, null, null);

        var response = GetServiceResponse.From(service);

        Assert.Equal("bare-svc", response.ServiceName);
        Assert.Null(response.HostName);
        Assert.Null(response.ContainerPortNumber);
        Assert.Null(response.HostPortNumber);
        Assert.Null(response.DockerImageUrl);
        Assert.Null(response.NetworkName);
        Assert.Null(response.EnvironmentVariables);
    }

    /// <summary>
    /// GetServiceResponse.From should map an empty environment-variables
    /// dictionary without throwing.
    /// </summary>
    [Fact]
    public void GetServiceResponse_From_WithEmptyEnvironmentVariables_MapsEmptyDictionary()
    {
        var service = new Service("empty-env-svc", null, null, null, null, null,
            new Dictionary<string, string>());

        var response = GetServiceResponse.From(service);

        Assert.NotNull(response.EnvironmentVariables);
        Assert.Empty(response.EnvironmentVariables);
    }
}
