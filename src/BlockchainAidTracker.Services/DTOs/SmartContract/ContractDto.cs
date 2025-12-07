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
    /// Whether the contract is enabled (always true for deployed contracts)
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// When the contract was deployed
    /// </summary>
    public DateTime DeployedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Contract type (class name)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Creates a ContractDto from an ISmartContract (without database lookup)
    /// </summary>
    public static ContractDto FromContract(ISmartContract contract)
    {
        var state = contract.GetState();

        // Try to get deployment timestamp from contract state
        DateTime deployedAt = DateTime.UtcNow;
        if (state.TryGetValue("_deployedAt", out var deployedAtObj))
        {
            if (deployedAtObj is DateTime dt)
            {
                deployedAt = dt;
            }
            else if (DateTime.TryParse(deployedAtObj.ToString(), out var parsedDate))
            {
                deployedAt = parsedDate;
            }
        }

        return new ContractDto
        {
            ContractId = contract.ContractId,
            Name = contract.Name,
            Description = contract.Description,
            Version = contract.Version,
            State = state,
            Enabled = true, // Deployed contracts are always enabled
            DeployedAt = deployedAt,
            Type = contract.GetType().Name
        };
    }

    /// <summary>
    /// Creates a ContractDto from an ISmartContract with database entity data
    /// </summary>
    public static ContractDto FromContractWithEntity(ISmartContract contract, object? dbEntity)
    {
        var dto = FromContract(contract);

        // Override with database values if available
        if (dbEntity != null)
        {
            var entityType = dbEntity.GetType();
            var deployedAtProp = entityType.GetProperty("DeployedAt");
            if (deployedAtProp?.GetValue(dbEntity) is DateTime deployedAt)
            {
                dto.DeployedAt = deployedAt;
            }

            var isEnabledProp = entityType.GetProperty("IsEnabled");
            if (isEnabledProp?.GetValue(dbEntity) is bool isEnabled)
            {
                dto.Enabled = isEnabled;
            }
        }

        return dto;
    }
}
