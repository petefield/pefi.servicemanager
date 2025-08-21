using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using Docker.DotNet.Models;

namespace pefi.servicemanager.Models
{
    public class Service(
        string ServiceName, 
        string? HostName, 
        string? ContainerPortNumber, 
        string? HostPortNumber, 
        string? DockerImageUrl, 
        string? NetworkName,
        Dictionary<string, string>? environmentVariables = null)
    {

        [BsonId]
        public ObjectId Id { get; set; } 

        [BsonElement("ServiceName")]
        public string ServiceName { get; set; } = ServiceName;

        [BsonElement("HostName")]
        public string? HostName { get; set; } = HostName;

        [BsonElement("ContainerPortNumber")]
        public string? ContainerPortNumber { get; set; } = ContainerPortNumber;

        [BsonElement("HostPortNumber")]
        public string? HostPortNumber { get; set; } = HostPortNumber;

        [BsonElement(nameof(DockerImageUrl))]
        public string? DockerImageUrl { get; set; } = DockerImageUrl;

        [BsonElement(nameof(NetworkName))]
        public string? NetworkName { get; set; } = NetworkName;

        public Dictionary<string, string>? EnvironmentVariables { get; set; } = environmentVariables;
    }
}
