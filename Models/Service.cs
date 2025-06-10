using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace pefi.servicemanager.Models
{
    public class Service(
        string ServiceName, 
        string? HostName, 
        string? ContainerPortNumber, 
        string? HostPortNumber, 
        string? DockerImageUrl)
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
        public string? DockerImageUrl { get; } = DockerImageUrl;
    }
}
