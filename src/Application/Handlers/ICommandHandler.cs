using MediatR;
using Zeus.People.Application.Commands;

namespace Zeus.People.Application.Handlers;

/// <summary>
/// Base interface for command handlers
/// </summary>
/// <typeparam name="TCommand">Type of the command</typeparam>
/// <typeparam name="TResponse">Type of the response</typeparam>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
}

/// <summary>
/// Base interface for command handlers that don't return a value
/// </summary>
/// <typeparam name="TCommand">Type of the command</typeparam>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand>
    where TCommand : ICommand
{
}
