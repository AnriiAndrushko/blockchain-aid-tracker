using BlockchainAidTracker.SmartContracts.Models;

namespace BlockchainAidTracker.Services.DTOs.SmartContract;

/// <summary>
/// Data transfer object for contract execution results
/// </summary>
public class ContractExecutionResultDto
{
    /// <summary>
    /// Indicates whether the contract execution was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if execution failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Output data from the contract execution
    /// </summary>
    public Dictionary<string, object> Output { get; set; } = new();

    /// <summary>
    /// State changes that were applied
    /// </summary>
    public Dictionary<string, object> StateChanges { get; set; } = new();

    /// <summary>
    /// Events emitted during contract execution
    /// </summary>
    public List<ContractEventDto> Events { get; set; } = new();

    /// <summary>
    /// Creates a ContractExecutionResultDto from a ContractExecutionResult
    /// </summary>
    public static ContractExecutionResultDto FromResult(ContractExecutionResult result)
    {
        return new ContractExecutionResultDto
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            Output = result.Output,
            StateChanges = result.StateChanges,
            Events = result.Events.Select(ContractEventDto.FromEvent).ToList()
        };
    }
}
