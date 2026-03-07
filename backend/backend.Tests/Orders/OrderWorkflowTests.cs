using backend.Application.Messaging;
using backend.Application.Orders;
using backend.Application.Users;
using backend.Data;
using backend.Handlers.Orders;
using backend.Models;
using backend.Requests.Orders;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.Orders;

public sealed class OrderWorkflowTests
{
    [Fact]
    public async Task RetryPayment_RequeuesFailedOrder_AsNewAttempt()
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Subject = "subject-1",
            Username = "tester"
        };

        var orderId = Guid.NewGuid();
        var firstPaymentId = Guid.NewGuid();
        await using var db = CreateDbContext();

        db.AppUsers.Add(user);
        db.Orders.Add(new DigitalOrder
        {
            Id = orderId,
            UserId = user.Id,
            DownloadUrl = "https://example.test/file",
            TotalAmount = 25m,
            Status = OrderStatuses.PaymentFailed
        });
        db.OrderSagaStates.Add(new OrderSagaState
        {
            OrderId = orderId,
            PaymentId = firstPaymentId,
            State = OrderSagaStates.PaymentFailed,
            Version = 2,
            LastPaymentRequestedAtUtc = DateTime.UtcNow.AddMinutes(-2),
            LastPaymentCompletedAtUtc = DateTime.UtcNow.AddMinutes(-1),
            ExecutionFailedAtUtc = DateTime.UtcNow.AddSeconds(-30),
            ExecutionFailureReason = "old execution failure"
        });
        db.PaymentEventRecords.Add(new PaymentEventRecord
        {
            PaymentId = firstPaymentId,
            OrderId = orderId,
            AttemptNumber = 1,
            SequenceNumber = 1,
            EventType = "PaymentInitiatedMessage",
            Data = "{}",
            OccurredAtUtc = DateTime.UtcNow.AddMinutes(-2)
        });
        db.PaymentEventRecords.Add(new PaymentEventRecord
        {
            PaymentId = firstPaymentId,
            OrderId = orderId,
            AttemptNumber = 1,
            SequenceNumber = 2,
            EventType = "PaymentFailedMessage",
            Data = "{}",
            OccurredAtUtc = DateTime.UtcNow.AddMinutes(-1)
        });
        await db.SaveChangesAsync();

        var outbox = new RecordingOutbox();
        var handler = new RetryOrderPaymentHandler(db, new FakeEffectiveUserAccessor(user), outbox);

        var result = await handler.Handle(new RetryOrderPaymentCommand(orderId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(OrderStatuses.PaymentPending, result.Value!.Status);

        var persistedOrder = await db.Orders.SingleAsync(x => x.Id == orderId);
        Assert.Equal(OrderStatuses.PaymentPending, persistedOrder.Status);

        var saga = await db.OrderSagaStates.SingleAsync(x => x.OrderId == orderId);
        Assert.Equal(OrderSagaStates.PaymentPending, saga.State);
        Assert.Null(saga.PaymentId);
        Assert.Null(saga.LastPaymentCompletedAtUtc);
        Assert.Null(saga.ExecutionDispatchedAtUtc);
        Assert.Null(saga.ExecutionStartedAtUtc);
        Assert.Null(saga.ExecutionCompletedAtUtc);
        Assert.Null(saga.ExecutionFailedAtUtc);
        Assert.Null(saga.ExecutionFailureReason);

        var message = Assert.Single(outbox.Messages);
        Assert.Equal(IntegrationRoutingKeys.OrderPaymentRequested, message.RoutingKey);
        Assert.Equal(orderId.ToString(), message.CorrelationId);
    }

    [Fact]
    public async Task GetWorkflow_ReturnsLatestAttempt_AndExecutionCompletionState()
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Subject = "subject-2",
            Username = "tester-2"
        };

        var orderId = Guid.NewGuid();
        var attemptOnePaymentId = Guid.NewGuid();
        var attemptTwoPaymentId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await using var db = CreateDbContext();
        db.AppUsers.Add(user);
        db.Orders.Add(new PhysicalOrder
        {
            Id = orderId,
            UserId = user.Id,
            ShippingAddress = "221B Baker Street",
            TrackingNumber = "TRACK-42",
            TotalAmount = 49m,
            Status = OrderStatuses.ExecutionCompleted,
            CreatedAtUtc = now.AddMinutes(-10)
        });
        db.OrderSagaStates.Add(new OrderSagaState
        {
            OrderId = orderId,
            PaymentId = attemptTwoPaymentId,
            State = OrderSagaStates.ExecutionCompleted,
            Version = 5,
            LastPaymentRequestedAtUtc = now.AddMinutes(-4),
            LastPaymentCompletedAtUtc = now.AddMinutes(-3),
            ExecutionDispatchedAtUtc = now.AddMinutes(-2),
            ExecutionStartedAtUtc = now.AddMinutes(-1),
            ExecutionCompletedAtUtc = now
        });
        db.PaymentEventRecords.AddRange(
            new PaymentEventRecord
            {
                PaymentId = attemptOnePaymentId,
                OrderId = orderId,
                AttemptNumber = 1,
                SequenceNumber = 1,
                EventType = "PaymentInitiatedMessage",
                Data = "{}",
                OccurredAtUtc = now.AddMinutes(-8)
            },
            new PaymentEventRecord
            {
                PaymentId = attemptOnePaymentId,
                OrderId = orderId,
                AttemptNumber = 1,
                SequenceNumber = 2,
                EventType = "PaymentFailedMessage",
                Data = "{\"paymentId\":\"" + attemptOnePaymentId + "\",\"orderId\":\"" + orderId + "\",\"reason\":\"Card declined\",\"occurredAtUtc\":\"" + now.AddMinutes(-7).ToString("O") + "\"}",
                OccurredAtUtc = now.AddMinutes(-7)
            },
            new PaymentEventRecord
            {
                PaymentId = attemptTwoPaymentId,
                OrderId = orderId,
                AttemptNumber = 2,
                SequenceNumber = 1,
                EventType = "PaymentInitiatedMessage",
                Data = "{}",
                OccurredAtUtc = now.AddMinutes(-4)
            },
            new PaymentEventRecord
            {
                PaymentId = attemptTwoPaymentId,
                OrderId = orderId,
                AttemptNumber = 2,
                SequenceNumber = 2,
                EventType = "PaymentAuthorizedMessage",
                Data = "{}",
                OccurredAtUtc = now.AddMinutes(-3)
            });
        await db.SaveChangesAsync();

        var handler = new GetOrderWorkflowHandler(db, new FakeEffectiveUserAccessor(user));

        var workflow = await handler.Handle(new GetOrderWorkflowQuery(orderId), CancellationToken.None);

        Assert.NotNull(workflow);
        Assert.Equal(OrderStatuses.ExecutionCompleted, workflow!.Order.Status);
        Assert.Equal(2, workflow.Payment.CurrentAttemptNumber);
        Assert.Equal(OrderSagaStates.ExecutionCompleted, workflow.Payment.SagaState);
        Assert.Equal(attemptTwoPaymentId, workflow.Payment.PaymentId);
        Assert.Equal(now, workflow.Payment.ExecutionCompletedAtUtc);
        Assert.Contains(workflow.Payment.Events, x => x.AttemptNumber == 1 && x.Reason == "Card declined");
        Assert.Contains(workflow.Timeline, x => x.Key == "execution-completed" && x.State == "Completed");
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AppDbContext(options);
    }

    private sealed class FakeEffectiveUserAccessor : IEffectiveUserAccessor
    {
        private readonly AppUser _user;

        public FakeEffectiveUserAccessor(AppUser user)
        {
            _user = user;
        }

        public Task<Guid> GetUserIdAsync(CancellationToken ct = default) => Task.FromResult(_user.Id);

        public Task<AppUser> GetUserAsync(CancellationToken ct = default) => Task.FromResult(_user);
    }

    private sealed class RecordingOutbox : IIntegrationEventOutbox
    {
        public List<(string RoutingKey, object? Message, string? CorrelationId)> Messages { get; } = [];

        public Task EnqueueAsync<T>(string routingKey, T message, string? correlationId = null, CancellationToken ct = default)
        {
            Messages.Add((routingKey, message, correlationId));
            return Task.CompletedTask;
        }
    }
}
