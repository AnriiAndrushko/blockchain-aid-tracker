namespace BlockchainAidTracker.SmartContracts.Models;

/// <summary>
/// Represents the result of a smart contract execution
/// </summary>
public class ContractExecutionResult
{
    /// <summary>
    /// Indicates whether the contract execution was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if execution failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Output data from the contract execution
    /// </summary>
    public Dictionary<string, object> Output { get; init; }

    /// <summary>
    /// State changes that should be applied
    /// </summary>
    public Dictionary<string, object> StateChanges { get; init; }

    /// <summary>
    /// Events emitted during contract execution
    /// </summary>
    public List<ContractEvent> Events { get; init; }

    public ContractExecutionResult()
    {
        Output = new Dictionary<string, object>();
        StateChanges = new Dictionary<string, object>();
        Events = new List<ContractEvent>();
    }

    public static ContractExecutionResult SuccessResult(Dictionary<string, object>? output = null,
        Dictionary<string, object>? stateChanges = null,
        List<ContractEvent>? events = null)
    {
        return new ContractExecutionResult
        {
            Success = true,
            Output = output ?? new Dictionary<string, object>(),
            StateChanges = stateChanges ?? new Dictionary<string, object>(),
            Events = events ?? new List<ContractEvent>()
        };
    }

    public static ContractExecutionResult FailureResult(string errorMessage)
    {
        return new ContractExecutionResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}

/// <summary>
/// Represents an event emitted by a smart contract
/// </summary>
public class ContractEvent
{
    /// <summary>
    /// Name of the event
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Event data
    /// </summary>
    public Dictionary<string, object> Data { get; init; }

    /// <summary>
    /// Timestamp when the event was emitted
    /// </summary>
    public DateTime Timestamp { get; init; }

    public ContractEvent(string name, Dictionary<string, object>? data = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Data = data ?? new Dictionary<string, object>();
        Timestamp = DateTime.UtcNow;
    }
}
