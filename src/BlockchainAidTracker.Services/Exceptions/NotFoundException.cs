namespace BlockchainAidTracker.Services.Exceptions;

/// <summary>
/// Exception for resource not found errors
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
