namespace Jobuler.Application.Common;

/// <summary>
/// Thrown when a domain operation is rejected because the entity is in an invalid
/// state for that operation (e.g. applying a personal constraint to an unregistered member).
/// Maps to HTTP 422 Unprocessable Entity.
/// </summary>
public class DomainValidationException : Exception
{
    public DomainValidationException(string message) : base(message) { }
}
