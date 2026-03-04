using backend.Application.Abstractions;
using backend.Data;
using MediatR;

namespace backend.Application.Behaviors;

public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    private readonly AppDbContext _db;

    public TransactionBehavior(AppDbContext db)
    {
        _db = db;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_db.Database.CurrentTransaction != null)
        {
            return await next();
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var response = await next();
        await transaction.CommitAsync(cancellationToken);
        return response;
    }
}
