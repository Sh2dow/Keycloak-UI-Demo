using MediatR;

namespace backend.Application.Abstractions;

public interface ICommand<out TResponse> : IRequest<TResponse>;
