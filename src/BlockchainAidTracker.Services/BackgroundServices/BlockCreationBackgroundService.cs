using BlockchainAidTracker.DataAccess.Repositories;
using BlockchainAidTracker.Services.Configuration;
using BlockchainAidTracker.Services.Consensus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BlockchainAidTracker.Services.BackgroundServices;

/// <summary>
/// Background service that automatically creates blocks from pending transactions
/// using the Proof-of-Authority consensus mechanism.
/// </summary>
public class BlockCreationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BlockCreationBackgroundService> _logger;
    private readonly ConsensusSettings _consensusSettings;
    private readonly Blockchain.Blockchain _blockchain;

    /// <summary>
    /// Creates a new instance of the BlockCreationBackgroundService.
    /// </summary>
    /// <param name="serviceProvider">Service provider for creating scoped services.</param>
    /// <param name="logger">Logger for service operations.</param>
    /// <param name="consensusSettings">Configuration settings for consensus and block creation.</param>
    /// <param name="blockchain">The blockchain instance.</param>
    public BlockCreationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<BlockCreationBackgroundService> logger,
        ConsensusSettings consensusSettings,
        Blockchain.Blockchain blockchain)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _consensusSettings = consensusSettings ?? throw new ArgumentNullException(nameof(consensusSettings));
        _blockchain = blockchain ?? throw new ArgumentNullException(nameof(blockchain));
    }

    /// <summary>
    /// Executes the background service, periodically checking for pending transactions
    /// and creating blocks when the minimum threshold is met.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token to stop the service.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_consensusSettings.EnableAutomatedBlockCreation)
        {
            _logger.LogInformation("Automated block creation is disabled in configuration");
            return;
        }

        _logger.LogInformation(
            "Block creation background service started. Checking every {Interval} seconds for at least {MinTx} pending transaction(s)",
            _consensusSettings.BlockCreationIntervalSeconds,
            _consensusSettings.MinimumTransactionsPerBlock);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(_consensusSettings.BlockCreationIntervalSeconds),
                    stoppingToken);

                await TryCreateBlockAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Service is stopping, exit gracefully
                _logger.LogInformation("Block creation background service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in block creation background service");
                // Continue running despite errors
            }
        }
    }

    /// <summary>
    /// Attempts to create a new block if there are sufficient pending transactions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task TryCreateBlockAsync(CancellationToken cancellationToken)
    {
        // Check if there are enough pending transactions
        var pendingCount = _blockchain.PendingTransactions.Count;

        if (pendingCount < _consensusSettings.MinimumTransactionsPerBlock)
        {
            _logger.LogDebug(
                "Skipping block creation: only {Count} pending transaction(s), minimum is {Min}",
                pendingCount,
                _consensusSettings.MinimumTransactionsPerBlock);
            return;
        }

        // Create a scope for scoped services (repository, consensus engine)
        using var scope = _serviceProvider.CreateScope();
        var consensusEngine = scope.ServiceProvider.GetRequiredService<IConsensusEngine>();
        var validatorRepository = scope.ServiceProvider.GetRequiredService<IValidatorRepository>();

        try
        {
            // Get the next validator
            var nextValidator = await validatorRepository.GetNextValidatorForBlockCreationAsync();
            if (nextValidator == null)
            {
                _logger.LogWarning("No active validators available for block creation");
                return;
            }

            _logger.LogInformation(
                "Creating block with {Count} pending transaction(s). Next validator: {Validator}",
                pendingCount,
                nextValidator.Name);

            // Create the block using the consensus engine
            var newBlock = await consensusEngine.CreateBlockAsync(
                _blockchain,
                _consensusSettings.ValidatorPassword);

            // Add the block to the blockchain
            _blockchain.AddBlock(newBlock);

            // Save blockchain to persistence if configured
            await _blockchain.SaveToPersistenceAsync(cancellationToken);

            // Note: Validator statistics are already saved by the consensusEngine.CreateBlockAsync
            // which calls validatorRepository.Update() that automatically saves changes

            _logger.LogInformation(
                "Block #{Index} created successfully by validator {Validator}. Hash: {Hash}, Transactions: {TxCount}",
                newBlock.Index,
                nextValidator.Name,
                newBlock.Hash,
                newBlock.Transactions.Count);
        }
        catch (InvalidOperationException ex)
        {
            // Expected exceptions (no transactions, no validators, decryption failure)
            _logger.LogWarning("Could not create block: {Message}", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating block");
        }
    }
}
