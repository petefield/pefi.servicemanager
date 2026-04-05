using Moq;
using pefi.Rabbit;
using pefi.servicemanager.Contracts.Messages;
using pefi.servicemanager.Models;
using pefi.servicemanager.Services;
using System.Linq.Expressions;
using System.Text.Json;

namespace pefi.servicemanager.tests;

public class ServiceRepositoryTests
{
    private const string DatabaseName = "ServiceDb";
    private const string CollectionName = "services";

    private readonly Mock<IDataStore> _mockDataStore;
    private readonly Mock<IMessageBroker> _mockMessageBroker;
    private readonly Mock<ITopic> _mockTopic;
    private readonly ServiceRepository _sut;

    public ServiceRepositoryTests()
    {
        _mockDataStore = new Mock<IDataStore>();
        _mockMessageBroker = new Mock<IMessageBroker>();
        _mockTopic = new Mock<ITopic>();

        _mockMessageBroker
            .Setup(m => m.CreateTopic(It.IsAny<string>()))
            .ReturnsAsync(_mockTopic.Object);

        _mockTopic
            .Setup(t => t.Publish(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _sut = new ServiceRepository(_mockMessageBroker.Object, _mockDataStore.Object);
    }

    // Returns all services from the data store
    [Fact]
    public async Task GetServices_ReturnsAllServices()
    {
        var services = new List<Service>
        {
            new("svc-a", "host-a", "3000", "3000", "image-a", "bridge"),
            new("svc-b", "host-b", "4000", "4000", "image-b", "bridge"),
        };

        _mockDataStore
            .Setup(d => d.Get<Service>(DatabaseName, CollectionName))
            .ReturnsAsync(services);

        var result = await _sut.GetServices();

        Assert.Equal(2, result.Count());
    }

    // Returns an empty collection when no services exist
    [Fact]
    public async Task GetServices_ReturnsEmpty_WhenNoServicesExist()
    {
        _mockDataStore
            .Setup(d => d.Get<Service>(DatabaseName, CollectionName))
            .ReturnsAsync(Enumerable.Empty<Service>());

        var result = await _sut.GetServices();

        Assert.Empty(result);
    }

    // Returns the matching service when it exists
    [Fact]
    public async Task GetService_ReturnsService_WhenFound()
    {
        var service = new Service("my-service", "host", "8080", "8080", "image:latest", "bridge");

        _mockDataStore
            .Setup(d => d.Get<Service>(DatabaseName, CollectionName, It.IsAny<Expression<Func<Service, bool>>>()))
            .ReturnsAsync(new List<Service> { service });

        var result = await _sut.GetService("my-service");

        Assert.NotNull(result);
        Assert.Equal("my-service", result.ServiceName);
    }

    // Returns null when the requested service does not exist
    [Fact]
    public async Task GetService_ReturnsNull_WhenNotFound()
    {
        _mockDataStore
            .Setup(d => d.Get<Service>(DatabaseName, CollectionName, It.IsAny<Expression<Func<Service, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<Service>());

        var result = await _sut.GetService("non-existent");

        Assert.Null(result);
    }

    // Persists the new service and publishes a service.created event
    [Fact]
    public async Task Add_PersistsServiceAndPublishesCreatedEvent()
    {
        _mockDataStore
            .Setup(d => d.Add<Service>(DatabaseName, CollectionName, It.IsAny<Service>()))
            .ReturnsAsync((string _, string _, Service s) => s);

        var result = await _sut.Add("new-svc", "host", "80", "8080", "image:latest", "network");

        Assert.Equal("new-svc", result.ServiceName);

        _mockDataStore.Verify(
            d => d.Add<Service>(DatabaseName, CollectionName, It.Is<Service>(s => s.ServiceName == "new-svc")),
            Times.Once);

        _mockTopic.Verify(
            t => t.Publish("events.service.created", It.Is<string>(msg =>
                msg.Contains("new-svc"))),
            Times.Once);
    }

    // Removes the service record and publishes a service.deleted event when the service exists
    [Fact]
    public async Task Delete_RemovesServiceAndPublishesDeletedEvent_WhenServiceExists()
    {
        var service = new Service("my-service", "host", "8080", "8080", "image:latest", "bridge");

        _mockDataStore
            .Setup(d => d.Get<Service>(DatabaseName, CollectionName, It.IsAny<Expression<Func<Service, bool>>>()))
            .ReturnsAsync(new List<Service> { service });

        _mockDataStore
            .Setup(d => d.Delete<Service>(DatabaseName, CollectionName, It.IsAny<Expression<Func<Service, bool>>>()))
            .Returns(Task.CompletedTask);

        await _sut.Delete("my-service");

        _mockDataStore.Verify(
            d => d.Delete<Service>(DatabaseName, CollectionName, It.IsAny<Expression<Func<Service, bool>>>()),
            Times.Once);

        _mockTopic.Verify(
            t => t.Publish("events.service.deleted", It.IsAny<string>()),
            Times.Once);
    }

    // Does not attempt to delete or publish an event when the service does not exist
    [Fact]
    public async Task Delete_DoesNothing_WhenServiceNotFound()
    {
        _mockDataStore
            .Setup(d => d.Get<Service>(DatabaseName, CollectionName, It.IsAny<Expression<Func<Service, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<Service>());

        await _sut.Delete("non-existent");

        _mockDataStore.Verify(
            d => d.Delete<Service>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<Service, bool>>>()),
            Times.Never);

        _mockTopic.Verify(
            t => t.Publish(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }
}
