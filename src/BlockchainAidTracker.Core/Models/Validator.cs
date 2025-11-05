namespace BlockchainAidTracker.Core.Models;

/// <summary>
/// Represents a validator node in the Proof-of-Authority consensus network
/// </summary>
public class Validator
{
    /// <summary>
    /// Unique identifier for the validator
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Validator name or identifier (e.g., "Validator-1", "UNHCR Node")
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Validator's public key for block signing and verification (ECDSA)
    /// </summary>
    public string PublicKey { get; set; }

    /// <summary>
    /// Validator's encrypted private key (encrypted with validator password)
    /// </summary>
    public string EncryptedPrivateKey { get; set; }

    /// <summary>
    /// Network address/endpoint for validator communication (e.g., "http://validator1:5000")
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Whether the validator is currently active and can participate in consensus
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Priority/order in the validator set for block proposer selection (0-based, lower = higher priority)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Timestamp when the validator was registered (UTC)
    /// </summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>
    /// Timestamp when the validator was last updated (UTC)
    /// </summary>
    public DateTime UpdatedTimestamp { get; set; }

    /// <summary>
    /// Timestamp when the validator last created a block (UTC)
    /// </summary>
    public DateTime? LastBlockCreatedTimestamp { get; set; }

    /// <summary>
    /// Total number of blocks created by this validator
    /// </summary>
    public int TotalBlocksCreated { get; set; }

    /// <summary>
    /// Description or notes about the validator
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Default constructor - initializes a new validator with default values
    /// </summary>
    public Validator()
    {
        Id = Guid.NewGuid().ToString();
        Name = string.Empty;
        PublicKey = string.Empty;
        EncryptedPrivateKey = string.Empty;
        IsActive = true;
        Priority = 0;
        TotalBlocksCreated = 0;
        CreatedTimestamp = DateTime.UtcNow;
        UpdatedTimestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Parameterized constructor for creating a validator with specific details
    /// </summary>
    /// <param name="name">Validator name</param>
    /// <param name="publicKey">Public key</param>
    /// <param name="encryptedPrivateKey">Encrypted private key</param>
    /// <param name="priority">Priority in validator set</param>
    /// <param name="address">Network address (optional)</param>
    /// <param name="description">Description (optional)</param>
    public Validator(
        string name,
        string publicKey,
        string encryptedPrivateKey,
        int priority,
        string? address = null,
        string? description = null)
    {
        Id = Guid.NewGuid().ToString();
        Name = name ?? throw new ArgumentNullException(nameof(name));
        PublicKey = publicKey ?? throw new ArgumentNullException(nameof(publicKey));
        EncryptedPrivateKey = encryptedPrivateKey ?? throw new ArgumentNullException(nameof(encryptedPrivateKey));
        Priority = priority >= 0 ? priority : throw new ArgumentException("Priority must be non-negative", nameof(priority));
        Address = address;
        Description = description;
        IsActive = true;
        TotalBlocksCreated = 0;
        CreatedTimestamp = DateTime.UtcNow;
        UpdatedTimestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the validator
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedTimestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the validator
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedTimestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Records that this validator created a new block
    /// </summary>
    public void RecordBlockCreation()
    {
        TotalBlocksCreated++;
        LastBlockCreatedTimestamp = DateTime.UtcNow;
        UpdatedTimestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the validator's priority
    /// </summary>
    /// <param name="newPriority">New priority value</param>
    public void UpdatePriority(int newPriority)
    {
        if (newPriority < 0)
        {
            throw new ArgumentException("Priority must be non-negative", nameof(newPriority));
        }

        Priority = newPriority;
        UpdatedTimestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the validator's address
    /// </summary>
    /// <param name="newAddress">New network address</param>
    public void UpdateAddress(string? newAddress)
    {
        Address = newAddress;
        UpdatedTimestamp = DateTime.UtcNow;
    }
}
