namespace Jobuler.Application.Common;

public class ConflictException : InvalidOperationException
{
    public ConflictException(string message) : base(message) { }
}
