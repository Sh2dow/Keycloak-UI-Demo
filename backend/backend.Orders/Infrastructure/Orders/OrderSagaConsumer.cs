using System.Text;
using backend.Application.Messaging;
using backend.Application.Messaging.Messages;
using backend.Application.Orders;
using backend.Data;
using backend.Infrastructure.Messaging;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace backend.Infrastructure.Orders;

public sealed class OrderSagaConsumer : BackgroundService
{
    private const string ConsumerName = "order-saga-consumer";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly RabbitMqOptions _rabbitOptions;
    private readonly ILogger<OrderSagaConsumer> _logger;

    public OrderSagaConsumer(
        IServiceScopeFactory scopeFactory,
        RabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqOptions> rabbitOptions,
        ILogger<OrderSagaConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _connectionFactory = connectionFactory;
        _rabbitOptions = rabbitOptions.Value;
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
                    queue: _rabbitOptions.OrderSagaQueue,
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
                _logger.LogWarning(ex, "Order saga consumer failed. Retrying.");
                await Task.Delay(TimeSpan.FromSeconds(_rabbitOptions.RetryDelaySeconds), stoppingToken);
            }
        }
    }

    private async Task HandleAsync(IChannel channel, BasicDeliverEventArgs args, CancellationToken ct)
    {
        var messageId = args.BasicProperties.MessageId ?? Guid.NewGuid().ToString("N");
        var eventType = args.BasicProperties.Type ?? string.Empty;

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

            switch (eventType)
            {
                case nameof(OrderPaymentRequestedMessage):
                {
                    var requested = IntegrationEventSerializer.Deserialize<OrderPaymentRequestedMessage>(payload);
                    await HandlePaymentRequestedAsync(db, requested, ct);
                    break;
                }
                case nameof(PaymentAuthorizedMessage):
                {
                    var authorized = IntegrationEventSerializer.Deserialize<PaymentAuthorizedMessage>(payload);
                    await HandlePaymentAuthorizedAsync(db, outbox, authorized, ct);
                    break;
                }
                case nameof(PaymentFailedMessage):
                {
                    var failed = IntegrationEventSerializer.Deserialize<PaymentFailedMessage>(payload);
                    await HandlePaymentFailedAsync(db, failed, ct);
                    break;
                }
                case nameof(OrderExecutionDispatchedMessage):
                {
                    var dispatched = IntegrationEventSerializer.Deserialize<OrderExecutionDispatchedMessage>(payload);
                    await HandleExecutionDispatchedAsync(db, dispatched, ct);
                    break;
                }
                default:
                    await channel.BasicAckAsync(args.DeliveryTag, false, ct);
                    return;
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
            _logger.LogWarning(ex, "Failed to handle saga message {MessageId}.", messageId);
            await channel.BasicNackAsync(args.DeliveryTag, false, requeue: true, cancellationToken: ct);
        }
    }

    private static async Task HandlePaymentRequestedAsync(
        AppDbContext db,
        OrderPaymentRequestedMessage requested,
        CancellationToken ct)
    {
        var order = await db.Orders.FirstOrDefaultAsync(x => x.Id == requested.OrderId, ct)
            ?? throw new InvalidOperationException($"Order {requested.OrderId} was not found for saga initialization.");

        var saga = await db.OrderSagaStates.FirstOrDefaultAsync(x => x.OrderId == requested.OrderId, ct);
        if (saga == null)
        {
            saga = new OrderSagaState
            {
                OrderId = requested.OrderId,
                State = OrderSagaStates.PaymentPending,
                LastPaymentRequestedAtUtc = requested.RequestedAtUtc,
                UpdatedAtUtc = DateTime.UtcNow,
                Version = 1
            };

            db.OrderSagaStates.Add(saga);
        }
        else if (!string.Equals(saga.State, OrderSagaStates.ExecutionDispatched, StringComparison.OrdinalIgnoreCase))
        {
            saga.State = OrderSagaStates.PaymentPending;
            saga.LastPaymentRequestedAtUtc = requested.RequestedAtUtc;
            saga.UpdatedAtUtc = DateTime.UtcNow;
            saga.Version += 1;
        }

        order.Status = OrderStatuses.PaymentPending;
    }

    private static async Task HandlePaymentAuthorizedAsync(
        AppDbContext db,
        IIntegrationEventOutbox outbox,
        PaymentAuthorizedMessage authorized,
        CancellationToken ct)
    {
        var order = await db.Orders.FirstOrDefaultAsync(x => x.Id == authorized.OrderId, ct)
            ?? throw new InvalidOperationException($"Order {authorized.OrderId} was not found for payment authorization.");

        var saga = await db.OrderSagaStates.FirstOrDefaultAsync(x => x.OrderId == authorized.OrderId, ct)
            ?? CreateMissingSaga(db, authorized.OrderId);

        if (string.Equals(saga.State, OrderSagaStates.ExecutionDispatched, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (string.Equals(saga.State, OrderSagaStates.PaymentAuthorized, StringComparison.OrdinalIgnoreCase)
            && saga.PaymentId == authorized.PaymentId)
        {
            return;
        }

        saga.PaymentId = authorized.PaymentId;
        saga.State = OrderSagaStates.PaymentAuthorized;
        saga.LastPaymentCompletedAtUtc = authorized.OccurredAtUtc;
        saga.UpdatedAtUtc = DateTime.UtcNow;
        saga.Version += 1;

        order.Status = OrderStatuses.PaymentAuthorized;

        await outbox.EnqueueAsync(
            IntegrationRoutingKeys.OrderExecutionDispatched,
            new OrderExecutionDispatchedMessage(order.Id, authorized.PaymentId, DateTime.UtcNow),
            order.Id.ToString(),
            ct);
    }

    private static async Task HandlePaymentFailedAsync(
        AppDbContext db,
        PaymentFailedMessage failed,
        CancellationToken ct)
    {
        var order = await db.Orders.FirstOrDefaultAsync(x => x.Id == failed.OrderId, ct)
            ?? throw new InvalidOperationException($"Order {failed.OrderId} was not found for payment failure.");

        var saga = await db.OrderSagaStates.FirstOrDefaultAsync(x => x.OrderId == failed.OrderId, ct)
            ?? CreateMissingSaga(db, failed.OrderId);

        if (string.Equals(saga.State, OrderSagaStates.ExecutionDispatched, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (string.Equals(saga.State, OrderSagaStates.PaymentFailed, StringComparison.OrdinalIgnoreCase)
            && saga.PaymentId == failed.PaymentId)
        {
            return;
        }

        saga.PaymentId = failed.PaymentId;
        saga.State = OrderSagaStates.PaymentFailed;
        saga.LastPaymentCompletedAtUtc = failed.OccurredAtUtc;
        saga.UpdatedAtUtc = DateTime.UtcNow;
        saga.Version += 1;

        order.Status = OrderStatuses.PaymentFailed;
    }

    private static async Task HandleExecutionDispatchedAsync(
        AppDbContext db,
        OrderExecutionDispatchedMessage dispatched,
        CancellationToken ct)
    {
        var order = await db.Orders.FirstOrDefaultAsync(x => x.Id == dispatched.OrderId, ct)
            ?? throw new InvalidOperationException($"Order {dispatched.OrderId} was not found for execution dispatch.");

        var saga = await db.OrderSagaStates.FirstOrDefaultAsync(x => x.OrderId == dispatched.OrderId, ct)
            ?? CreateMissingSaga(db, dispatched.OrderId);

        if (string.Equals(saga.State, OrderSagaStates.ExecutionDispatched, StringComparison.OrdinalIgnoreCase)
            && saga.PaymentId == dispatched.PaymentId)
        {
            return;
        }

        saga.PaymentId ??= dispatched.PaymentId;
        saga.State = OrderSagaStates.ExecutionDispatched;
        saga.ExecutionDispatchedAtUtc = dispatched.DispatchedAtUtc;
        saga.UpdatedAtUtc = DateTime.UtcNow;
        saga.Version += 1;

        order.Status = OrderStatuses.ExecutionDispatched;
    }

    private static OrderSagaState CreateMissingSaga(AppDbContext db, Guid orderId)
    {
        var saga = new OrderSagaState
        {
            OrderId = orderId,
            State = OrderSagaStates.PaymentPending,
            UpdatedAtUtc = DateTime.UtcNow,
            Version = 0
        };

        db.OrderSagaStates.Add(saga);
        return saga;
    }
}
