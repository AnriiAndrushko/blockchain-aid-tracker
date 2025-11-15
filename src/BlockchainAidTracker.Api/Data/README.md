# Blockchain Data Directory

This directory stores the persisted blockchain data for the Blockchain Aid Tracker application.

## Files

- **blockchain-data.json** - Main blockchain persistence file (created automatically)
- **blockchain-data.json.{timestamp}.bak** - Backup files (created automatically)

## Configuration

Blockchain persistence is configured in `appsettings.json`:

```json
"BlockchainPersistenceSettings": {
  "Enabled": true,
  "FilePath": "Data/blockchain-data.json",
  "AutoSaveAfterBlockCreation": true,
  "AutoLoadOnStartup": true,
  "CreateBackup": true,
  "MaxBackupFiles": 5
}
```

## How It Works

1. **On Application Startup**: The blockchain is automatically loaded from `blockchain-data.json` if it exists
2. **During Operation**: After each new block is created, the blockchain is automatically saved
3. **Backup Creation**: Before saving, a backup of the previous state is created with a timestamp
4. **Backup Rotation**: Only the most recent 5 backups are kept (configurable)

## Notes

- The blockchain data file is excluded from git via `.gitignore`
- Backups are also excluded from git
- This directory is tracked in git via `.gitkeep`
