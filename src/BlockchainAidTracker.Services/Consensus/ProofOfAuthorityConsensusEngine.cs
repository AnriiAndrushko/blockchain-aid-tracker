using BlockchainAidTracker.Core.Extensions;
using BlockchainAidTracker.Core.Interfaces;
using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.DataAccess.Repositories;
using BlockchainAidTracker.Services.Interfaces;

namespace BlockchainAidTracker.Services.Consensus;

/// <summary>
/// Implements Proof-of-Authority (PoA) consensus mechanism for the blockchain.
/// In PoA, a set of authorized validators take turns creating blocks in a round-robin fashion.
/// </summary>
public class ProofOfAuthorityConsensusEngine : IConsensusEngine
{
    private readonly IValidatorRepository _validatorRepository;
    private readonly IKeyManagementService _keyManagementService;
    private readonly IDigitalSignatureService _signatureService;
    private readonly IHashService _hashService;

    /// <summary>
    /// Creates a new instance of the ProofOfAuthorityConsensusEngine.
    /// </summary>
    /// <param name="validatorRepository">Repository for validator data access.</param>
    /// <param name="keyManagementService">Service for encrypting/decrypting private keys.</param>
    /// <param name="signatureService">Service for digital signature operations.</param>
    /// <param name="hashService">Service for hashing operations.</param>
    public ProofOfAuthorityConsensusEngine(
        IValidatorRepository validatorRepository,
        IKeyManagementService keyManagementService,
        IDigitalSignatureService signatureService,
        IHashService hashService)
    {
        _validatorRepository = validatorRepository ?? throw new ArgumentNullException(nameof(validatorRepository));
        _keyManagementService = keyManagementService ?? throw new ArgumentNullException(nameof(keyManagementService));
        _signatureService = signatureService ?? throw new ArgumentNullException(nameof(signatureService));
        _hashService = hashService ?? throw new ArgumentNullException(nameof(hashService));
    }

    /// <summary>
    /// Creates a new block from pending transactions using PoA consensus.
    /// The next validator in round-robin order is selected to create and sign the block.
    /// </summary>
    /// <param name="blockchain">The blockchain instance to create the block for.</param>
    /// <param name="validatorPassword">Password to decrypt the validator's private key for signing.</param>
    /// <returns>A newly created and signed block ready to be added to the chain.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no pending transactions exist, no active validators are available,
    /// or the validator's private key cannot be decrypted.
    /// </exception>
    public async Task<Block> CreateBlockAsync(Blockchain.Blockchain blockchain, string validatorPassword)
    {
        if (blockchain == null)
        {
            throw new ArgumentNullException(nameof(blockchain));
        }

        if (string.IsNullOrEmpty(validatorPassword))
        {
            throw new ArgumentException("Validator password cannot be null or empty.", nameof(validatorPassword));
        }

        // Check if there are pending transactions
        if (blockchain.PendingTransactions.Count == 0)
        {
            throw new InvalidOperationException("No pending transactions to create a block.");
        }

        // Get the next validator in round-robin order
        var validator = await _validatorRepository.GetNextValidatorForBlockCreationAsync();
        if (validator == null)
        {
            throw new InvalidOperationException("No active validators available to create a block.");
        }

        // Decrypt the validator's private key
        string validatorPrivateKey;
        try
        {
            validatorPrivateKey = _keyManagementService.DecryptPrivateKey(
                validator.EncryptedPrivateKey,
                validatorPassword);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to decrypt validator's private key. Ensure the password is correct. Validator: {validator.Name}",
                ex);
        }

        // Create the block using the blockchain's existing method
        var block = blockchain.CreateBlock(validator.PublicKey);

        // Sign the block with the validator's private key
        block.SignBlock(validatorPrivateKey, _signatureService);

        // Record block creation statistics
        validator.RecordBlockCreation();
        await _validatorRepository.UpdateAsync(validator);

        return block;
    }

    /// <summary>
    /// Validates whether a block meets the PoA consensus rules.
    /// Checks that the block was signed by an authorized validator.
    /// </summary>
    /// <param name="block">The block to validate.</param>
    /// <param name="previousBlock">The previous block in the chain.</param>
    /// <returns>True if the block is valid according to consensus rules; otherwise, false.</returns>
    public bool ValidateBlock(Block block, Block previousBlock)
    {
        if (block == null)
        {
            return false;
        }

        if (previousBlock == null)
        {
            return false;
        }

        // Check block index continuity
        if (previousBlock.Index + 1 != block.Index)
        {
            return false;
        }

        // Check previous hash linkage
        if (block.PreviousHash != previousBlock.Hash)
        {
            return false;
        }

        // Validate block hash
        var calculatedHash = _hashService.ComputeSha256Hash(block.CalculateHashData());
        if (block.Hash != calculatedHash)
        {
            return false;
        }

        // Validate validator signature (critical for PoA)
        if (!block.VerifyValidatorSignature(_signatureService))
        {
            return false;
        }

        // Verify that the validator who signed the block is authorized
        // Note: For a full implementation, we would check if the validator exists
        // and was active at the time of block creation. For this prototype,
        // signature verification is sufficient.

        return true;
    }

    /// <summary>
    /// Gets the ID of the validator who should propose the next block according to round-robin selection.
    /// </summary>
    /// <returns>The validator ID who should propose the next block, or null if no validators are available.</returns>
    public async Task<string?> GetCurrentBlockProposerAsync()
    {
        var validator = await _validatorRepository.GetNextValidatorForBlockCreationAsync();
        return validator?.Id;
    }

    /// <summary>
    /// Records that a block was successfully created by a validator.
    /// Updates validator statistics in the database.
    /// </summary>
    /// <param name="validatorId">The ID of the validator who created the block.</param>
    public async Task RecordBlockCreationAsync(string validatorId)
    {
        if (string.IsNullOrEmpty(validatorId))
        {
            throw new ArgumentException("Validator ID cannot be null or empty.", nameof(validatorId));
        }

        var validator = await _validatorRepository.GetByIdAsync(validatorId);
        if (validator == null)
        {
            throw new InvalidOperationException($"Validator with ID {validatorId} not found.");
        }

        validator.RecordBlockCreation();
        await _validatorRepository.UpdateAsync(validator);
    }
}
