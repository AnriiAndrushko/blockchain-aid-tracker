using BlockchainAidTracker.SmartContracts.Interfaces;
using BlockchainAidTracker.SmartContracts.Models;

namespace BlockchainAidTracker.SmartContracts;

/// <summary>
/// Base class for smart contracts providing common functionality
/// </summary>
public abstract class SmartContract : ISmartContract
{
    private readonly Dictionary<string, object> _state;
    private readonly object _stateLock = new();

    public string ContractId { get; }
    public abstract string Name { get; }
    public abstract string Description { get; }
    public virtual string Version => "1.0.0";

    protected SmartContract(string contractId)
    {
        ContractId = contractId ?? throw new ArgumentNullException(nameof(contractId));
        _state = new Dictionary<string, object>();
    }

    public abstract bool CanExecute(ContractExecutionContext context);

    public abstract Task<ContractExecutionResult> ExecuteAsync(ContractExecutionContext context);

    public Dictionary<string, object> GetState()
    {
        lock (_stateLock)
        {
            return new Dictionary<string, object>(_state);
        }
    }

    public void UpdateState(Dictionary<string, object> stateChanges)
    {
        if (stateChanges == null)
            throw new ArgumentNullException(nameof(stateChanges));

        lock (_stateLock)
        {
            foreach (var (key, value) in stateChanges)
            {
                _state[key] = value;
            }
        }
    }

    protected object? GetStateValue(string key)
    {
        lock (_stateLock)
        {
            return _state.TryGetValue(key, out var value) ? value : null;
        }
    }

    protected void SetStateValue(string key, object value)
    {
        lock (_stateLock)
        {
            _state[key] = value;
        }
    }

    protected ContractEvent EmitEvent(string eventName, Dictionary<string, object>? data = null)
    {
        return new ContractEvent(eventName, data);
    }
}
