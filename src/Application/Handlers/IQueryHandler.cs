using MediatR;
using Zeus.People.Application.Queries;

namespace Zeus.People.Application.Handlers;

/// <summary>
/// Base interface for query handlers
/// </summary>
/// <typeparam name="TQuery">Type of the query</typeparam>
/// <typeparam name="TResponse">Type of the response</typeparam>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
}
