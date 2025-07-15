namespace Zeus.People.Domain.Exceptions;

/// <summary>
/// Base class for all domain exceptions
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
    protected DomainException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when a business rule is violated
/// </summary>
public class BusinessRuleViolationException : DomainException
{
    public BusinessRuleViolationException(string message) : base(message) { }
}

/// <summary>
/// Exception thrown when trying to access an entity that doesn't exist
/// </summary>
public class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entityName, object id)
        : base($"{entityName} with ID '{id}' was not found") { }
}

/// <summary>
/// Exception thrown when a required invariant is violated
/// </summary>
public class InvariantViolationException : DomainException
{
    public InvariantViolationException(string message) : base(message) { }
}

/// <summary>
/// Exception thrown when an invalid operation is attempted
/// </summary>
public class InvalidDomainOperationException : DomainException
{
    public InvalidDomainOperationException(string message) : base(message) { }
}
