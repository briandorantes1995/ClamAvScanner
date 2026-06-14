using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace ClamScanner.Services;

public interface IRabbitMqPublisher
{
    Task PublishAsync<T>(string queueName, T message);
}

public class RabbitMqPublisher : IRabbitMqPublisher, IAsyncDisposable
{
    private readonly ConnectionFactory _factory;
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqPublisher()
    {
        // Configure connection parameters
        _factory = new ConnectionFactory 
        { 
            HostName = "localhost", // Change to cloud/remote host if needed
            UserName = "guest",
            Password = "guest" 
        };
    }

    public async Task PublishAsync<T>(string queueName, T message)
    {
        // Lazy initialization of connection and channel
        if (_connection == null) _connection = await _factory.CreateConnectionAsync();
        if (_channel == null) _channel = await _connection.CreateChannelAsync();

        // Declare the queue before sending to ensure it exists
        await _channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        // Define properties (e.g., message persistence)
        var properties = new BasicProperties { DeliveryMode = DeliveryModes.Persistent };

        // Publish to default exchange targeting the specified queue
        await _channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: queueName,
            mandatory: true,
            basicProperties: properties,
            body: body
        );
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null) await _channel.CloseAsync();
        if (_connection != null) await _connection.CloseAsync();
    }
}