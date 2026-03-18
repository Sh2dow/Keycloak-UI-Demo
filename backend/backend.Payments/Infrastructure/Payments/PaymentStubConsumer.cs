using System.Text;
using backend.Domain.Data;
using backend.Domain.Models;
using backend.Infrastructure.Infrastructure.Messaging;
using backend.Shared.Application.Messaging;
using backend.Shared.Application.Messaging.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace backend.Payments.Infrastructure.Payments;

public sealed class PaymentStubConsumer : BackgroundService
{
    private const string ConsumerName = "payment-stub-consumer";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly RabbitMqOptions _rabbitOptions;
    private readonly PaymentOptions _paymentOptions;
    private readonly ILogger<PaymentStubConsumer> _logger;

    public PaymentStubConsumer(
        IServiceScopeFactory scopeFactory,
        RabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqOptions> rabbitOptions,
        IOptions<PaymentOptions> paymentOptions,
        ILogger<PaymentStubConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _connectionFactory = connectionFactory;
        _rabbitOptions = rabbitOptions.Value;
        _paymentOptions = paymentOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var connection = await _connectionFactory.CreateConnectionAsync(stoppingToken);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

                await RabbitMqTopology.EnsureConfiguredAsync(channel, _rabbitOptions, stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (_, args) =>
                {
                    await HandleAsync(channel, args, stoppingToken);
                };

                await channel.BasicConsumeAsync(
                    queue: _rabbitOptions.PaymentRequestQueue,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: stoppingToken);

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Payment stub consumer failed. Retrying.");
                await Task.Delay(TimeSpan.FromSeconds(_rabbitOptions.RetryDelaySeconds), stoppingToken);
            }
        }
    }

    private async Task HandleAsync(IChannel channel, BasicDeliverEventArgs args, CancellationToken ct)
    {
        var messageId = args.BasicProperties.MessageId ?? Guid.NewGuid().ToString("N");

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var outbox = scope.ServiceProvider.GetRequiredService<IIntegrationEventOutbox>();

            var alreadyProcessed = await db.ConsumedMessages
                .AnyAsync(x => x.Consumer == ConsumerName && x.MessageId == messageId, ct);

            if (alreadyProcessed)
            {
                await channel.BasicAckAsync(args.DeliveryTag, false, ct);
                return;
            }

            var payload = Encoding.UTF8.GetString(args.Body.ToArray());
            var orderPaymentRequested = IntegrationEventSerializer.Deserialize<OrderPaymentRequestedMessage>(payload);

            var currentAttemptNumber = await db.PaymentEventRecords
                .Where(x => x.OrderId == orderPaymentRequested.OrderId)
                .MaxAsync(x => (int?)x.AttemptNumber, ct) ?? 0;

            var paymentId = Guid.NewGuid();
            var attemptNumber = currentAttemptNumber + 1;
            const int initiatedSequence = 1;
            const int finalSequence = 2;

            var initiated = new PaymentInitiatedMessage(
                paymentId,
                orderPaymentRequested.OrderId,
                orderPaymentRequested.TotalAmount,
                DateTime.UtcNow);

            db.PaymentEventRecords.Add(new PaymentEventRecord
            {
                PaymentId = paymentId,
                OrderId = orderPaymentRequested.OrderId,
                AttemptNumber = attemptNumber,
                SequenceNumber = initiatedSequence,
                EventType = nameof(PaymentInitiatedMessage),
                Data = IntegrationEventSerializer.Serialize(initiated),
                OccurredAtUtc = initiated.OccurredAtUtc
            });

            await outbox.EnqueueAsync(
                IntegrationRoutingKeys.PaymentInitiated,
                initiated,
                orderPaymentRequested.OrderId.ToString(),
                ct);

            if (_paymentOptions.StubDelayMilliseconds > 0)
            {
                await Task.Delay(_paymentOptions.StubDelayMilliseconds, ct);
            }

            if (_paymentOptions.AutoAuthorize)
            {
                var authorized = new PaymentAuthorizedMessage(
                    paymentId,
                    orderPaymentRequested.OrderId,
                    orderPaymentRequested.TotalAmount,
                    DateTime.UtcNow);

                db.PaymentEventRecords.Add(new PaymentEventRecord
                {
                    PaymentId = paymentId,
                    OrderId = orderPaymentRequested.OrderId,
                    AttemptNumber = attemptNumber,
                    SequenceNumber = finalSequence,
                    EventType = nameof(PaymentAuthorizedMessage),
                    Data = IntegrationEventSerializer.Serialize(authorized),
                    OccurredAtUtc = authorized.OccurredAtUtc
                });

                await outbox.EnqueueAsync(
                    IntegrationRoutingKeys.PaymentAuthorized,
                    authorized,
                    orderPaymentRequested.OrderId.ToString(),
                    ct);
            }
            else
            {
                var failed = new PaymentFailedMessage(
                    paymentId,
                    orderPaymentRequested.OrderId,
                    "Stub payment rejection.",
                    DateTime.UtcNow);

                db.PaymentEventRecords.Add(new PaymentEventRecord
                {
                    PaymentId = paymentId,
                    OrderId = orderPaymentRequested.OrderId,
                    AttemptNumber = attemptNumber,
                    SequenceNumber = finalSequence,
                    EventType = nameof(PaymentFailedMessage),
                    Data = IntegrationEventSerializer.Serialize(failed),
                    OccurredAtUtc = failed.OccurredAtUtc
                });

                await outbox.EnqueueAsync(
                    IntegrationRoutingKeys.PaymentFailed,
                    failed,
                    orderPaymentRequested.OrderId.ToString(),
                    ct);
            }

            db.ConsumedMessages.Add(new ConsumedMessage
            {
                Consumer = ConsumerName,
                MessageId = messageId
            });

            await db.SaveChangesAsync(ct);
            await channel.BasicAckAsync(args.DeliveryTag, false, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to handle order payment request message {MessageId}.", messageId);
            await channel.BasicNackAsync(args.DeliveryTag, false, requeue: true, cancellationToken: ct);
        }
    }
}
