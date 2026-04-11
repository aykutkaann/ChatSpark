using ChatSpark.Shared.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace ChatSpark.Infrastructure.Messaging;

public class MessageSentConsumer : BackgroundService
{
    private readonly string _connectionString;
    private readonly ILogger<MessageSentConsumer> _logger;
    private const string QueueName = "message-sent";

    public MessageSentConsumer(IConfiguration configuration, ILogger<MessageSentConsumer> logger)
    {
        _logger = logger;
        _connectionString = configuration.GetConnectionString("RabbitMQ")
                            ?? "amqp://chatspark:chatspark_dev@localhost:5672";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var factory = new ConnectionFactory { Uri = new Uri(_connectionString) };

            using var connection = await factory.CreateConnectionAsync(stoppingToken);
            using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await channel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (sender, args) =>
            {
                try
                {
                    var body = args.Body.ToArray();
                    var messageJson = Encoding.UTF8.GetString(body);
                    var @event = JsonSerializer.Deserialize<MessageSentEvent>(messageJson);

                    if (@event != null)
                    {
                        _logger.LogInformation(
                            "Processing message {MessageId} from {SenderId} in channel {ChannelId}",
                            @event.MessageId, @event.SenderId, @event.ChannelId);

                    }

                    await channel.BasicAckAsync(args.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message from RabbitMQ");
                    await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: true);
                }

            };

            await channel.BasicConsumeAsync(
                queue: QueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation("RabbitMQ Consumer started. Listening to {QueueName}...", QueueName);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("RabbitMQ Consumer is shutting down...");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "RabbitMQ Consumer failed to start");
        }
    }
}