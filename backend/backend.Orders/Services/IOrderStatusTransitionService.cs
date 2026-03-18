namespace backend.Services;

public interface IOrderStatusTransitionService
{
    bool ValidateStatusTransition(string fromStatus, string toStatus);

    IEnumerable<string> GetAllowedTransitions(string currentStatus);
}
