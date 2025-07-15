using MediatR;

namespace Zeus.People.Application.Commands;

/// <summary>
/// Base interface for all commands
/// </summary>
/// <typeparam name="TResponse">Type of the command response</typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse>
{
}

/// <summary>
/// Base interface for commands that don't return a value
/// </summary>
public interface ICommand : IRequest
{
}
