using MongoDB.Driver;

namespace ClamScanner.Services;

public interface IMongoConnection
{
    Task<IMongoCollection<T>> ConnectAsync<T>(string db, string collection);
}