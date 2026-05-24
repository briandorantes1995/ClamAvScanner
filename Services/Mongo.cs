using MongoDB.Driver;

namespace ClamScanner.Services;

public class Mongo : IMongoConnection
{
    private readonly IConfiguration _configuration;

    public Mongo(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public async Task<IMongoCollection<T>> ConnectAsync<T>(string db, string collection)
    {
        var connectionString = _configuration["Mongo:ConnectionString"];
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("La cadena de conexión 'Mongo:ConnectionString' no está configurada.");
        }
        
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(db);
        
        var collect = database.GetCollection<T>(collection);

        return await Task.FromResult(collect);
    }
}