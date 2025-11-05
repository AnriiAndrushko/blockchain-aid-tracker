namespace BlockchainAidTracker.Services.DTOs.SmartContract;

/// <summary>
/// Request to execute a smart contract
/// </summary>
public class ExecuteContractRequest
{
    /// <summary>
    /// ID of the contract to execute
    /// </summary>
    public string ContractId { get; set; } = string.Empty;

    /// <summary>
    /// Transaction ID to execute the contract for
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Optional additional data to pass to the contract
    /// </summary>
    public Dictionary<string, object>? AdditionalData { get; set; }
}
