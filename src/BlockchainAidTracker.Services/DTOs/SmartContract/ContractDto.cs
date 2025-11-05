using BlockchainAidTracker.SmartContracts.Interfaces;

namespace BlockchainAidTracker.Services.DTOs.SmartContract;

/// <summary>
/// Data transfer object for smart contract information
/// </summary>
public class ContractDto
{
    /// <summary>
    /// Unique identifier for the contract
    /// </summary>
    public string ContractId { get; set; } = string.Empty;

    /// <summary>
    /// Name of the contract
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what the contract does
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Version of the contract
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Current state of the contract
    /// </summary>
    public Dictionary<string, object> State { get; set; } = new();

    /// <summary>
    /// Creates a ContractDto from an ISmartContract
    /// </summary>
    public static ContractDto FromContract(ISmartContract contract)
    {
        return new ContractDto
        {
            ContractId = contract.ContractId,
            Name = contract.Name,
            Description = contract.Description,
            Version = contract.Version,
            State = contract.GetState()
        };
    }
}
