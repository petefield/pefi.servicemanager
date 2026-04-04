using Moq;
using pefi.Rabbit;
using pefi.servicemanager.Contracts.Messages;
using pefi.servicemanager.Services;
using System.Text.Json;
using Service = pefi.servicemanager.Models.Service;

namespace pefi.servicemanager.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ServiceRepository"/>.
///
/// All external dependencies (IMessageBroker, IDataStore) are replaced with
/// Moq fakes so that each test exercises only the logic inside
/// ServiceRepository without requiring a live database or message broker.
/// </summary>
public class ServiceRepositoryTests
{
    private const string DatabaseName = "ServiceDb";
    private const string CollectionName = "services";

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates a pre-configured <see cref="Mock{IMessageBroker}"/> whose
    /// <c>CreateTopic</c> method returns a fake topic that accepts any Publish
    /// call without error.
    /// </summary>
    private static Mock<IMessageBroker> BuildBrokerMock()
    {
        var topicMock = new Mock<ITopic>();
        topicMock
            .Setup(t => t.Publish(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var brokerMock = new Mock<IMessageBroker>();
        brokerMock
            .Setup(b => b.CreateTopic(It.IsAny<string>()))
            .ReturnsAsync(topicMock.Object);

        return brokerMock;
    }

    /// <summary>
    /// Builds a <see cref="ServiceRepository"/> wired up to the supplied mocks.
    /// </summary>
    private static ServiceRepository BuildRepository(
        Mock<IMessageBroker> brokerMock,
        Mock<IDataStore> datastoreMock)
        => new(brokerMock.Object, datastoreMock.Object);

    // -------------------------------------------------------------------------
    // GetServices
    // -------------------------------------------------------------------------

    /// <summary>
    /// GetServices should delegate to the datastore and return whatever the
    /// datastore returns.
    /// </summary>
    [Fact]
    public async Task GetServices_ReturnsAllServicesFromDatastore()
    {
        // Arrange
        var expected = new[]
        {
            new Service("svc-a", "host-a", "80", "8080", "image-a", "net-a"),
            new Service("svc-b", "host-b", "443", "8443", "image-b", "net-b"),
        };

        var datastoreMock = new Mock<IDataStore>();
        datastoreMock
            .Setup(d => d.Get<Service>(DatabaseName, CollectionName))
            .ReturnsAsync(expected);

        var repo = BuildRepository(BuildBrokerMock(), datastoreMock);

        // Act
        var result = await repo.GetServices();

        // Assert
        Assert.Equal(expected, result);
        datastoreMock.Verify(d => d.Get<Service>(DatabaseName, CollectionName), Times.Once);
    }

    /// <summary>
    /// GetServices should return an empty sequence (not throw) when the
    /// datastore has no records.
    /// </summary>
    [Fact]
    public async Task GetServices_WhenDatastoreIsEmpty_ReturnsEmptySequence()
    {
        var datastoreMock = new Mock<IDataStore>();
        datastoreMock
            .Setup(d => d.Get<Service>(DatabaseName, CollectionName))
            .ReturnsAsync(Enumerable.Empty<Service>());

        var repo = BuildRepository(BuildBrokerMock(), datastoreMock);

        var result = await repo.GetServices();

        Assert.Empty(result);
    }

    // -------------------------------------------------------------------------
    // GetService
    // -------------------------------------------------------------------------

    /// <summary>
    /// GetService should return the matching service when one exists.
    /// </summary>
    [Fact]
    public async Task GetService_WhenServiceExists_ReturnsThatService()
    {
        var service = new Service("my-svc", null, null, null, null, null);

        var datastoreMock = new Mock<IDataStore>();
        datastoreMock
            .Setup(d => d.Get<Service>(DatabaseName, CollectionName, It.IsAny<System.Linq.Expressions.Expression<Func<Service, bool>>>()))
            .ReturnsAsync(new[] { service });

        var repo = BuildRepository(BuildBrokerMock(), datastoreMock);

        var result = await repo.GetService("my-svc");

        Assert.NotNull(result);
        Assert.Equal("my-svc", result.ServiceName);
    }

    /// <summary>
    /// GetService should return null when no service with that name exists.
    /// </summary>
    [Fact]
    public async Task GetService_WhenServiceDoesNotExist_ReturnsNull()
    {
        var datastoreMock = new Mock<IDataStore>();
        datastoreMock
            .Setup(d => d.Get<Service>(DatabaseName, CollectionName, It.IsAny<System.Linq.Expressions.Expression<Func<Service, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<Service>());

        var repo = BuildRepository(BuildBrokerMock(), datastoreMock);

        var result = await repo.GetService("unknown");

        Assert.Null(result);
    }

    // -------------------------------------------------------------------------
    // Add
    // -------------------------------------------------------------------------

    /// <summary>
    /// Add should persist a new Service to the datastore with the supplied
    /// properties.
    /// </summary>
    [Fact]
    public async Task Add_SavesServiceToDatastore()
    {
        var datastoreMock = new Mock<IDataStore>();
        datastoreMock
            .Setup(d => d.Add(DatabaseName, CollectionName, It.IsAny<Service>()))
            .ReturnsAsync((string _, string _, Service s) => s);

        var repo = BuildRepository(BuildBrokerMock(), datastoreMock);

        var result = await repo.Add("new-svc", "host", "80", "8080", "my-image", "my-net");

        Assert.Equal("new-svc", result.ServiceName);
        Assert.Equal("host", result.HostName);
        Assert.Equal("80", result.ContainerPortNumber);
        Assert.Equal("8080", result.HostPortNumber);
        Assert.Equal("my-image", result.DockerImageUrl);
        Assert.Equal("my-net", result.NetworkName);

        datastoreMock.Verify(
            d => d.Add(DatabaseName, CollectionName, It.Is<Service>(s => s.ServiceName == "new-svc")),
            Times.Once);
    }

    /// <summary>
    /// Add should publish a ServiceCreatedMessage to the "Events" topic on the
    /// "events.service.created" routing key.
    /// </summary>
    [Fact]
    public async Task Add_PublishesServiceCreatedMessage()
    {
        var datastoreMock = new Mock<IDataStore>();
        datastoreMock
            .Setup(d => d.Add(DatabaseName, CollectionName, It.IsAny<Service>()))
            .ReturnsAsync((string _, string _, Service s) => s);

        var topicMock = new Mock<ITopic>();
        topicMock
            .Setup(t => t.Publish(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var brokerMock = new Mock<IMessageBroker>();
        brokerMock
            .Setup(b => b.CreateTopic("Events"))
            .ReturnsAsync(topicMock.Object);

        var repo = BuildRepository(brokerMock, datastoreMock);

        await repo.Add("pub-svc", null, null, null, null, null);

        // The broker must have been asked for the "Events" topic.
        brokerMock.Verify(b => b.CreateTopic("Events"), Times.Once);

        // A ServiceCreatedMessage containing the service name must have been published.
        topicMock.Verify(
            t => t.Publish(
                "events.service.created",
                It.Is<string>(msg => msg.Contains("pub-svc"))),
            Times.Once);
    }

    /// <summary>
    /// Add should propagate optional environment variables to the stored Service.
    /// </summary>
    [Fact]
    public async Task Add_WithEnvironmentVariables_StoresThemOnTheService()
    {
        var envVars = new Dictionary<string, string> { { "KEY", "VALUE" } };

        var datastoreMock = new Mock<IDataStore>();
        datastoreMock
            .Setup(d => d.Add(DatabaseName, CollectionName, It.IsAny<Service>()))
            .ReturnsAsync((string _, string _, Service s) => s);

        var repo = BuildRepository(BuildBrokerMock(), datastoreMock);

        var result = await repo.Add("env-svc", null, null, null, null, null, envVars);

        Assert.NotNull(result.EnvironmentVariables);
        Assert.Equal("VALUE", result.EnvironmentVariables["KEY"]);
    }

    // -------------------------------------------------------------------------
    // Delete
    // -------------------------------------------------------------------------

    /// <summary>
    /// Delete should remove the service from the datastore when it exists.
    /// </summary>
    [Fact]
    public async Task Delete_WhenServiceExists_DeletesFromDatastore()
    {
        var service = new Service("del-svc", null, null, null, null, null);

        var datastoreMock = new Mock<IDataStore>();

        // GetService lookup
        datastoreMock
            .Setup(d => d.Get<Service>(DatabaseName, CollectionName, It.IsAny<System.Linq.Expressions.Expression<Func<Service, bool>>>()))
            .ReturnsAsync(new[] { service });

        datastoreMock
            .Setup(d => d.Delete<Service>(DatabaseName, CollectionName, It.IsAny<System.Linq.Expressions.Expression<Func<Service, bool>>>()))
            .Returns(Task.CompletedTask);

        var repo = BuildRepository(BuildBrokerMock(), datastoreMock);

        await repo.Delete("del-svc");

        datastoreMock.Verify(
            d => d.Delete<Service>(DatabaseName, CollectionName, It.IsAny<System.Linq.Expressions.Expression<Func<Service, bool>>>()),
            Times.Once);
    }

    /// <summary>
    /// Delete should publish a ServiceDeletedMessage when the service exists.
    /// </summary>
    [Fact]
    public async Task Delete_WhenServiceExists_PublishesServiceDeletedMessage()
    {
        var service = new Service("del-svc", null, null, null, null, null);

        var datastoreMock = new Mock<IDataStore>();
        datastoreMock
            .Setup(d => d.Get<Service>(DatabaseName, CollectionName, It.IsAny<System.Linq.Expressions.Expression<Func<Service, bool>>>()))
            .ReturnsAsync(new[] { service });
        datastoreMock
            .Setup(d => d.Delete<Service>(DatabaseName, CollectionName, It.IsAny<System.Linq.Expressions.Expression<Func<Service, bool>>>()))
            .Returns(Task.CompletedTask);

        var topicMock = new Mock<ITopic>();
        topicMock
            .Setup(t => t.Publish(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var brokerMock = new Mock<IMessageBroker>();
        brokerMock
            .Setup(b => b.CreateTopic("Events"))
            .ReturnsAsync(topicMock.Object);

        var repo = BuildRepository(brokerMock, datastoreMock);

        await repo.Delete("del-svc");

        brokerMock.Verify(b => b.CreateTopic("Events"), Times.Once);
        topicMock.Verify(
            t => t.Publish(
                "events.service.deleted",
                It.Is<string>(msg => msg.Contains("del-svc"))),
            Times.Once);
    }

    /// <summary>
    /// Delete should do nothing (no datastore delete, no message published) when
    /// the service does not exist.
    /// </summary>
    [Fact]
    public async Task Delete_WhenServiceDoesNotExist_DoesNotDeleteOrPublish()
    {
        var datastoreMock = new Mock<IDataStore>();
        datastoreMock
            .Setup(d => d.Get<Service>(DatabaseName, CollectionName, It.IsAny<System.Linq.Expressions.Expression<Func<Service, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<Service>());

        var brokerMock = BuildBrokerMock();
        var repo = BuildRepository(brokerMock, datastoreMock);

        await repo.Delete("ghost");

        datastoreMock.Verify(
            d => d.Delete<Service>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.Linq.Expressions.Expression<Func<Service, bool>>>()),
            Times.Never);

        brokerMock.Verify(b => b.CreateTopic(It.IsAny<string>()), Times.Never);
    }
}
