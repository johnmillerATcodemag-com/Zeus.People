using MediatR;

namespace Zeus.People.Application.Queries;

/// <summary>
/// Base interface for all queries
/// </summary>
/// <typeparam name="TResponse">Type of the query response</typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse>
{
}
