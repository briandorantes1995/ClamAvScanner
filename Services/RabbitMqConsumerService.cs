using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ClamScanner.Services;

public class RabbitMqConsumerService : BackgroundService
{
    private readonly ILogger<RabbitMqConsumerService> _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    private const string QueueName = "my_service_queue";

    public RabbitMqConsumerService(ILogger<RabbitMqConsumerService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken
        );

        // Optimizes message distribution among multiple instances (Fair Dispatch)
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        
        // Handle incoming messages asynchronously
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            try
            {
                _logger.LogInformation("Received message: {Message}", message);

                // Simulate processing logic here
                await Task.Delay(100, stoppingToken); 

                // Acknowledge successful processing
                await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing RabbitMQ message.");
                
                // Requeue message if processing failed
                await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false, // Explicit acknowledgement managed manually above
            consumer: consumer,
            cancellationToken: stoppingToken
        );

        // Keeps the background task active until the service stops
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null) await _channel.CloseAsync(cancellationToken: cancellationToken);
        if (_connection != null) await _connection.CloseAsync(cancellationToken: cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
