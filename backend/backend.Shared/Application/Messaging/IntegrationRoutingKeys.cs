namespace backend.Application.Messaging;

public static class IntegrationRoutingKeys
{
    public const string OrderPaymentRequested = "orders.payment-requested";
    public const string PaymentInitiated = "payments.initiated";
    public const string PaymentAuthorized = "payments.authorized";
    public const string PaymentFailed = "payments.failed";
    public const string OrderExecutionDispatched = "orders.execution-dispatched";
}
