namespace backend.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Uri { get; set; } = "amqp://guest:guest@localhost:5672";
    public string Exchange { get; set; } = "keycloak-demo.events";
    public string DeadLetterExchange { get; set; } = "keycloak-demo.events.dlx";
    public string PaymentRequestQueue { get; set; } = "payments.stub.requests";
    public string OrderSagaQueue { get; set; } = "orders.saga";
    public string OrderExecutionQueue { get; set; } = "orders.execution.dispatch";
    public int OutboxBatchSize { get; set; } = 20;
    public int RetryDelaySeconds { get; set; } = 5;
}
