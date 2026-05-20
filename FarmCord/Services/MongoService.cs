using MongoDB.Driver;

namespace FarmCord.Services;

public class MongoService
{
    public IMongoDatabase Database { get; }

    public MongoService()
    {
        var client = new MongoClient("mongodb://mongo-service:27017");

        Database = client.GetDatabase("DiscordUser");
    }
}