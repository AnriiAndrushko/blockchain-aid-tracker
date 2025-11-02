using BlockchainAidTracker.Core.Extensions;
using BlockchainAidTracker.Core.Interfaces;
using BlockchainAidTracker.Core.Models;

namespace BlockchainAidTracker.Blockchain;

/// <summary>
/// Manages the blockchain and its operations.
/// </summary>
public class Blockchain
{
    private readonly IHashService _hashService;
    private readonly IDigitalSignatureService _signatureService;

    /// <summary>
    /// The chain of blocks.
    /// </summary>
    public List<Block> Chain { get; private set; } = new();

    /// <summary>
    /// Pool of pending transactions waiting to be added to a block.
    /// </summary>
    public List<Transaction> PendingTransactions { get; private set; } = new();

    /// <summary>
    /// When true, transaction signatures will be validated when adding to pending pool.
    /// </summary>
    public bool ValidateTransactionSignatures { get; set; } = true;

    /// <summary>
    /// When true, block validator signatures will be validated when adding blocks.
    /// </summary>
    public bool ValidateBlockSignatures { get; set; } = true;

    /// <summary>
    /// Creates a new blockchain with the specified services and initializes it with a genesis block.
    /// </summary>
    /// <param name="hashService">The hash service to use for computing block hashes.</param>
    /// <param name="signatureService">The digital signature service for signing and verifying.</param>
    public Blockchain(IHashService hashService, IDigitalSignatureService signatureService)
    {
        _hashService = hashService ?? throw new ArgumentNullException(nameof(hashService));
        _signatureService = signatureService ?? throw new ArgumentNullException(nameof(signatureService));
        InitializeChain();
    }

    /// <summary>
    /// Initializes the blockchain with a genesis block.
    /// </summary>
    private void InitializeChain()
    {
        var genesisBlock = CreateGenesisBlock();
        Chain.Add(genesisBlock);
    }

    /// <summary>
    /// Creates the first block in the blockchain (Genesis Block).
    /// </summary>
    private Block CreateGenesisBlock()
    {
        var genesisBlock = new Block
        {
            Index = 0,
            Timestamp = DateTime.UtcNow,
            Transactions = new List<Transaction>(),
            PreviousHash = "0",
            Hash = string.Empty,
            Nonce = 0,
            ValidatorPublicKey = "GENESIS",
            ValidatorSignature = string.Empty
        };

        // Calculate hash using SHA-256
        genesisBlock.Hash = _hashService.ComputeSha256Hash(genesisBlock.CalculateHashData());

        return genesisBlock;
    }

    /// <summary>
    /// Gets the latest block in the chain.
    /// </summary>
    public Block GetLatestBlock()
    {
        return Chain[^1];
    }

    /// <summary>
    /// Adds a transaction to the pending transaction pool.
    /// </summary>
    public void AddTransaction(Transaction transaction)
    {
        if (string.IsNullOrEmpty(transaction.SenderPublicKey))
        {
            throw new ArgumentException("Transaction must have a sender public key.");
        }

        if (string.IsNullOrEmpty(transaction.PayloadData))
        {
            throw new ArgumentException("Transaction must have payload data.");
        }

        // Validate transaction signature if enabled
        if (ValidateTransactionSignatures)
        {
            if (!transaction.VerifySignature(_signatureService))
            {
                throw new InvalidOperationException("Transaction signature is invalid.");
            }
        }

        PendingTransactions.Add(transaction);
    }

    /// <summary>
    /// Creates a new block from pending transactions.
    /// </summary>
    public Block CreateBlock(string validatorPublicKey)
    {
        if (PendingTransactions.Count == 0)
        {
            throw new InvalidOperationException("No pending transactions to create a block.");
        }

        var newBlock = new Block
        {
            Index = Chain.Count,
            Timestamp = DateTime.UtcNow,
            Transactions = new List<Transaction>(PendingTransactions),
            PreviousHash = GetLatestBlock().Hash,
            ValidatorPublicKey = validatorPublicKey
        };

        // Calculate hash using SHA-256
        newBlock.Hash = _hashService.ComputeSha256Hash(newBlock.CalculateHashData());

        return newBlock;
    }

    /// <summary>
    /// Adds a block to the blockchain after validation.
    /// </summary>
    public bool AddBlock(Block block)
    {
        if (!IsValidNewBlock(block, GetLatestBlock()))
        {
            return false;
        }

        Chain.Add(block);

        // Clear pending transactions that were included in the block
        PendingTransactions.Clear();

        return true;
    }

    /// <summary>
    /// Validates if a new block can be added to the chain.
    /// </summary>
    public bool IsValidNewBlock(Block newBlock, Block previousBlock)
    {
        if (previousBlock.Index + 1 != newBlock.Index)
        {
            return false;
        }

        if (newBlock.PreviousHash != previousBlock.Hash)
        {
            return false;
        }

        var calculatedHash = _hashService.ComputeSha256Hash(newBlock.CalculateHashData());
        if (newBlock.Hash != calculatedHash)
        {
            return false;
        }

        // Validate validator signature if enabled
        if (ValidateBlockSignatures)
        {
            if (!newBlock.VerifyValidatorSignature(_signatureService))
            {
                return false;
            }
        }

        // Validate all transaction signatures in the block if enabled
        if (ValidateTransactionSignatures)
        {
            foreach (var transaction in newBlock.Transactions)
            {
                if (!transaction.VerifySignature(_signatureService))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Validates the entire blockchain.
    /// </summary>
    public bool IsValidChain()
    {
        // Check genesis block
        if (Chain[0].Hash != _hashService.ComputeSha256Hash(Chain[0].CalculateHashData()))
        {
            return false;
        }

        // Validate each block against the previous one
        for (int i = 1; i < Chain.Count; i++)
        {
            if (!IsValidNewBlock(Chain[i], Chain[i - 1]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Gets a block by its index.
    /// </summary>
    public Block? GetBlockByIndex(int index)
    {
        if (index < 0 || index >= Chain.Count)
        {
            return null;
        }

        return Chain[index];
    }

    /// <summary>
    /// Gets a transaction by its ID from the entire blockchain.
    /// </summary>
    public Transaction? GetTransactionById(string transactionId)
    {
        foreach (var block in Chain)
        {
            var transaction = block.Transactions.FirstOrDefault(t => t.Id == transactionId);
            if (transaction != null)
            {
                return transaction;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the total number of blocks in the chain.
    /// </summary>
    public int GetChainLength()
    {
        return Chain.Count;
    }

    /// <summary>
    /// Gets all transactions from a specific sender.
    /// </summary>
    public List<Transaction> GetTransactionsBySender(string senderPublicKey)
    {
        var transactions = new List<Transaction>();

        foreach (var block in Chain)
        {
            transactions.AddRange(
                block.Transactions.Where(t => t.SenderPublicKey == senderPublicKey)
            );
        }

        return transactions;
    }
}
