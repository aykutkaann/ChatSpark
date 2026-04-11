using System.Text;
using System.Text.Json;
using ChatSpark.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace ChatSpark.Infrastructure.Messaging;

public class RabbitMqPublisher : IEventPublisher, IDisposable
{
    private readonly string _connectionString;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly string _queueName = "chat_messages";

    public RabbitMqPublisher(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("RabbitMq")
                            ?? "amqp://chatspark:chatspark_dev@localhost:5672";
    }

    private async Task InitializeAsync()
    {
        if (_channel is not null) return;

        await _lock.WaitAsync();
        try
        {
            if (_channel is not null) return;

            var factory = new ConnectionFactory
            {
                Uri = new Uri(_connectionString)
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task PublishAsync<T>(string queueName, T message, CancellationToken ct = default)
    {
        await InitializeAsync();

        await _channel!.QueueDeclareAsync(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: ct);

        var json = JsonSerializer.Serialize(queueName);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = new BasicProperties
        {
            Persistent = true
        };

        await _channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: _queueName,
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: ct);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        _lock.Dispose();
    }
}