using BlockchainAidTracker.SmartContracts.Interfaces;
using BlockchainAidTracker.SmartContracts.Models;
using Microsoft.Extensions.Logging;

namespace BlockchainAidTracker.SmartContracts.Engine;

/// <summary>
/// Engine responsible for deploying and executing smart contracts
/// </summary>
public class SmartContractEngine
{
    private readonly Dictionary<string, ISmartContract> _deployedContracts;
    private readonly ILogger<SmartContractEngine>? _logger;
    private readonly object _contractsLock = new();

    public SmartContractEngine(ILogger<SmartContractEngine>? logger = null)
    {
        _deployedContracts = new Dictionary<string, ISmartContract>();
        _logger = logger;
    }

    /// <summary>
    /// Deploys a smart contract to the engine
    /// </summary>
    public bool DeployContract(ISmartContract contract)
    {
        if (contract == null)
            throw new ArgumentNullException(nameof(contract));

        lock (_contractsLock)
        {
            if (_deployedContracts.ContainsKey(contract.ContractId))
            {
                _logger?.LogWarning("Contract with ID {ContractId} is already deployed", contract.ContractId);
                return false;
            }

            _deployedContracts[contract.ContractId] = contract;
            _logger?.LogInformation("Deployed contract: {Name} (ID: {ContractId})",
                contract.Name, contract.ContractId);
            return true;
        }
    }

    /// <summary>
    /// Removes a deployed contract from the engine
    /// </summary>
    public bool UndeployContract(string contractId)
    {
        if (string.IsNullOrWhiteSpace(contractId))
            throw new ArgumentException("Contract ID cannot be null or empty", nameof(contractId));

        lock (_contractsLock)
        {
            if (_deployedContracts.Remove(contractId))
            {
                _logger?.LogInformation("Undeployed contract with ID: {ContractId}", contractId);
                return true;
            }

            _logger?.LogWarning("Contract with ID {ContractId} not found", contractId);
            return false;
        }
    }

    /// <summary>
    /// Gets a deployed contract by ID
    /// </summary>
    public ISmartContract? GetContract(string contractId)
    {
        if (string.IsNullOrWhiteSpace(contractId))
            throw new ArgumentException("Contract ID cannot be null or empty", nameof(contractId));

        lock (_contractsLock)
        {
            return _deployedContracts.TryGetValue(contractId, out var contract) ? contract : null;
        }
    }

    /// <summary>
    /// Gets all deployed contracts
    /// </summary>
    public IReadOnlyList<ISmartContract> GetAllContracts()
    {
        lock (_contractsLock)
        {
            return _deployedContracts.Values.ToList();
        }
    }

    /// <summary>
    /// Executes a specific contract by ID
    /// </summary>
    public async Task<ContractExecutionResult> ExecuteContractAsync(string contractId, ContractExecutionContext context)
    {
        if (string.IsNullOrWhiteSpace(contractId))
            throw new ArgumentException("Contract ID cannot be null or empty", nameof(contractId));

        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var contract = GetContract(contractId);
        if (contract == null)
        {
            _logger?.LogError("Contract with ID {ContractId} not found", contractId);
            return ContractExecutionResult.FailureResult($"Contract with ID {contractId} not found");
        }

        if (!contract.CanExecute(context))
        {
            _logger?.LogWarning("Contract {Name} cannot execute for this context", contract.Name);
            return ContractExecutionResult.FailureResult($"Contract {contract.Name} cannot execute for this context");
        }

        try
        {
            _logger?.LogInformation("Executing contract: {Name} (ID: {ContractId})",
                contract.Name, contractId);

            var result = await contract.ExecuteAsync(context);

            if (result.Success && result.StateChanges.Count > 0)
            {
                contract.UpdateState(result.StateChanges);
                _logger?.LogInformation("Contract {Name} executed successfully with {StateChangeCount} state changes",
                    contract.Name, result.StateChanges.Count);
            }
            else if (!result.Success)
            {
                _logger?.LogError("Contract {Name} execution failed: {ErrorMessage}",
                    contract.Name, result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error executing contract {Name}", contract.Name);
            return ContractExecutionResult.FailureResult($"Contract execution error: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes all applicable contracts for the given context
    /// </summary>
    public async Task<List<ContractExecutionResult>> ExecuteApplicableContractsAsync(ContractExecutionContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var results = new List<ContractExecutionResult>();
        var contracts = GetAllContracts();

        foreach (var contract in contracts)
        {
            if (contract.CanExecute(context))
            {
                var result = await ExecuteContractAsync(contract.ContractId, context);
                results.Add(result);
            }
        }

        _logger?.LogInformation("Executed {ExecutedCount} contracts out of {TotalCount} deployed contracts",
            results.Count, contracts.Count);

        return results;
    }

    /// <summary>
    /// Gets the state of a specific contract
    /// </summary>
    public Dictionary<string, object>? GetContractState(string contractId)
    {
        var contract = GetContract(contractId);
        return contract?.GetState();
    }
}
