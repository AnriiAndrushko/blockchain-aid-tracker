using BlockchainAidTracker.SmartContracts.Models;

namespace BlockchainAidTracker.Services.DTOs.SmartContract;

/// <summary>
/// Data transfer object for contract events
/// </summary>
public class ContractEventDto
{
    /// <summary>
    /// Name of the event
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Event data
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();

    /// <summary>
    /// Timestamp when the event was emitted
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Creates a ContractEventDto from a ContractEvent
    /// </summary>
    public static ContractEventDto FromEvent(ContractEvent contractEvent)
    {
        return new ContractEventDto
        {
            Name = contractEvent.Name,
            Data = contractEvent.Data,
            Timestamp = contractEvent.Timestamp
        };
    }
}
