namespace backend.Services.Implementations;

public sealed class OrderStatusTransitionService : IOrderStatusTransitionService
{
    private static readonly Dictionary<string, HashSet<string>> _allowedTransitions = new()
    {
        // Payment-related transitions
        ["PaymentPending"] = new HashSet<string> { "PaymentAuthorized", "PaymentFailed" },
        ["PaymentAuthorized"] = new HashSet<string> { "ExecutionDispatched", "PaymentFailed" },
        ["PaymentFailed"] = new HashSet<string> { "PaymentPending", "ExecutionFailed" },
        
        // Execution-related transitions
        ["ExecutionDispatched"] = new HashSet<string> { "ExecutionStarted", "ExecutionFailed" },
        ["ExecutionStarted"] = new HashSet<string> { "ExecutionCompleted", "ExecutionFailed" },
        ["ExecutionCompleted"] = new HashSet<string> { "ExecutionFailed" },
        ["ExecutionFailed"] = new HashSet<string> { "ExecutionDispatched", "PaymentFailed" }
    };

    public bool ValidateStatusTransition(string fromStatus, string toStatus)
    {
        if (string.IsNullOrEmpty(fromStatus) || string.IsNullOrEmpty(toStatus))
            return false;

        if (!_allowedTransitions.TryGetValue(fromStatus, out var allowedTransitions))
            return false;

        return allowedTransitions.Contains(toStatus);
    }

    public IEnumerable<string> GetAllowedTransitions(string currentStatus)
    {
        if (string.IsNullOrEmpty(currentStatus))
            return Enumerable.Empty<string>();

        return _allowedTransitions.TryGetValue(currentStatus, out var allowed)
            ? allowed
            : Enumerable.Empty<string>();
    }
}
