namespace backend.Application.Orders;

public static class OrderSagaStates
{
    public const string PaymentPending = "PaymentPending";
    public const string PaymentAuthorized = "PaymentAuthorized";
    public const string PaymentFailed = "PaymentFailed";
    public const string ExecutionDispatched = "ExecutionDispatched";
}
