using System.Text.Json;
using BlockchainAidTracker.Blockchain.Configuration;
using BlockchainAidTracker.Blockchain.Interfaces;
using BlockchainAidTracker.Core.Models;
using Microsoft.Extensions.Logging;

namespace BlockchainAidTracker.Blockchain.Persistence;

/// <summary>
/// JSON file-based implementation of blockchain persistence.
/// Stores blockchain data in a JSON file for persistence across restarts.
/// </summary>
public class JsonBlockchainPersistence : IBlockchainPersistence
{
    private readonly BlockchainPersistenceSettings _settings;
    private readonly ILogger<JsonBlockchainPersistence> _logger;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    /// <summary>
    /// Blockchain data model for JSON serialization.
    /// </summary>
    private class BlockchainData
    {
        public List<Block> Chain { get; set; } = new();
        public List<Transaction> PendingTransactions { get; set; } = new();
        public DateTime SavedAt { get; set; }
        public string Version { get; set; } = "1.0";
    }

    public JsonBlockchainPersistence(
        BlockchainPersistenceSettings settings,
        ILogger<JsonBlockchainPersistence> logger)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task SaveAsync(
        List<Block> chain,
        List<Transaction> pendingTransactions,
        CancellationToken cancellationToken = default)
    {
        if (chain == null)
        {
            throw new ArgumentNullException(nameof(chain));
        }

        if (pendingTransactions == null)
        {
            throw new ArgumentNullException(nameof(pendingTransactions));
        }

        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            // Create backup if enabled
            if (_settings.CreateBackup && await ExistsAsync(cancellationToken))
            {
                await CreateBackupAsync(cancellationToken);
            }

            var data = new BlockchainData
            {
                Chain = chain,
                PendingTransactions = pendingTransactions,
                SavedAt = DateTime.UtcNow
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(data, options);

            // Ensure directory exists
            var directory = Path.GetDirectoryName(_settings.FilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(_settings.FilePath, json, cancellationToken);

            _logger.LogInformation(
                "Blockchain persisted successfully: {BlockCount} blocks, {TransactionCount} pending transactions",
                chain.Count,
                pendingTransactions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist blockchain data to {FilePath}", _settings.FilePath);
            throw;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<(List<Block> Chain, List<Transaction> PendingTransactions)?> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        if (!await ExistsAsync(cancellationToken))
        {
            _logger.LogInformation("No persisted blockchain data found at {FilePath}", _settings.FilePath);
            return null;
        }

        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            var json = await File.ReadAllTextAsync(_settings.FilePath, cancellationToken);

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var data = JsonSerializer.Deserialize<BlockchainData>(json, options);

            if (data == null)
            {
                _logger.LogWarning("Failed to deserialize blockchain data from {FilePath}", _settings.FilePath);
                return null;
            }

            _logger.LogInformation(
                "Blockchain loaded successfully: {BlockCount} blocks, {TransactionCount} pending transactions (saved at {SavedAt})",
                data.Chain.Count,
                data.PendingTransactions.Count,
                data.SavedAt);

            return (data.Chain, data.PendingTransactions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load blockchain data from {FilePath}", _settings.FilePath);
            throw;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(File.Exists(_settings.FilePath));
    }

    /// <inheritdoc />
    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        if (!await ExistsAsync(cancellationToken))
        {
            return;
        }

        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            File.Delete(_settings.FilePath);
            _logger.LogInformation("Blockchain data deleted from {FilePath}", _settings.FilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete blockchain data from {FilePath}", _settings.FilePath);
            throw;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// Creates a backup of the current blockchain data file.
    /// </summary>
    private async Task CreateBackupAsync(CancellationToken cancellationToken)
    {
        try
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var backupPath = $"{_settings.FilePath}.{timestamp}.bak";

            File.Copy(_settings.FilePath, backupPath, overwrite: true);

            _logger.LogInformation("Blockchain backup created at {BackupPath}", backupPath);

            // Rotate backups if needed
            if (_settings.MaxBackupFiles > 0)
            {
                await RotateBackupsAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create blockchain backup");
            // Don't throw - backup failure shouldn't prevent save
        }
    }

    /// <summary>
    /// Rotates backup files, keeping only the most recent ones.
    /// </summary>
    private Task RotateBackupsAsync()
    {
        try
        {
            var directory = Path.GetDirectoryName(_settings.FilePath);
            var fileName = Path.GetFileName(_settings.FilePath);

            if (string.IsNullOrEmpty(directory))
            {
                directory = Directory.GetCurrentDirectory();
            }

            var backupFiles = Directory.GetFiles(directory, $"{fileName}.*.bak")
                .OrderByDescending(f => f)
                .ToList();

            // Delete old backups beyond the limit
            for (int i = _settings.MaxBackupFiles; i < backupFiles.Count; i++)
            {
                File.Delete(backupFiles[i]);
                _logger.LogInformation("Deleted old backup: {BackupFile}", backupFiles[i]);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to rotate blockchain backups");
            // Don't throw - backup rotation failure shouldn't prevent save
        }

        return Task.CompletedTask;
    }
}
