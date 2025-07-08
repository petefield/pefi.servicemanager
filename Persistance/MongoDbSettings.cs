using System;

namespace pefi.servicemanager.Persistance;

public class MongoDbSettings
{
    public MongoDbSettings()
    {
            
    }



    public MongoDbSettings(string connectionString)
    {
        ConnectionString = connectionString;
    }

    public string ConnectionString { get; set; } 
}
