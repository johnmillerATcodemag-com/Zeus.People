using Zeus.People.Application.Commands;
using Zeus.People.Application.Common;
using Zeus.People.Application.DTOs;

namespace Zeus.People.Application.Commands.Extension;

public record CreateExtensionCommand(ExtensionDto Extension) : ICommand<Result<ExtensionDto>>;
public record UpdateExtensionCommand(ExtensionDto Extension) : ICommand<Result<ExtensionDto>>;
public record DeleteExtensionCommand(Guid Id) : ICommand<Result<bool>>;
public record AssignExtensionToAcademicCommand(Guid ExtensionId, Guid AcademicId) : ICommand<Result<bool>>;
public record UnassignExtensionCommand(Guid ExtensionId) : ICommand<Result<bool>>;
