using MediatR;

namespace backend.Application.Abstractions;

public interface IQuery<out TResponse> : IRequest<TResponse>;
