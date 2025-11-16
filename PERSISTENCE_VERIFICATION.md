# Blockchain Persistence Verification Guide

This guide helps you verify that blockchain persistence is working correctly.

## How to Test Persistence

### Step 1: Start the API Application

```bash
cd /home/user/blockchain-aid-tracker
dotnet run --project src/BlockchainAidTracker.Api/BlockchainAidTracker.Api.csproj
```

### Step 2: Check Startup Logs

You should see these log messages on startup:

```
[Startup] Blockchain persistence enabled: true, file path: Data/blockchain-data.json
[Startup] Blockchain created with persistence support
[Startup] Attempting to load blockchain from persistence: Data/blockchain-data.json
```

On first run, you'll see:
```
No persisted blockchain data found at Data/blockchain-data.json
Blockchain loaded successfully with 1 blocks and 0 pending transactions
```

### Step 3: Create Some Blockchain Data

Use the API to create data (example with curl):

1. **Register a user:**
```bash
curl -X POST http://localhost:5000/api/authentication/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "email": "test@example.com",
    "password": "Password123!",
    "fullName": "Test User",
    "role": "Coordinator"
  }'
```

2. **Login to get token:**
```bash
curl -X POST http://localhost:5000/api/authentication/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "password": "Password123!"
  }'
```

Save the `accessToken` from the response.

3. **Create a shipment (creates a blockchain transaction):**
```bash
curl -X POST http://localhost:5000/api/shipments \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN_HERE" \
  -d '{
    "originPoint": "Warehouse A",
    "destinationPoint": "Shelter B",
    "recipientId": "recipient-guid-here",
    "expectedDeliveryDate": "2025-12-31T23:59:59Z",
    "items": [
      {
        "description": "Medical Supplies",
        "quantity": 100,
        "unit": "boxes"
      }
    ]
  }'
```

### Step 4: Wait for Block Creation

The background service runs every 30 seconds. Wait and watch for these logs:

```
[BlockCreationBackgroundService] Creating block with 1 pending transaction(s). Next validator: ValidatorName
[BlockCreationBackgroundService] Attempting to save blockchain to persistence after block creation
[JsonBlockchainPersistence] Blockchain persisted successfully: 2 blocks, 0 pending transactions
[BlockCreationBackgroundService] Blockchain saved to persistence successfully
[BlockCreationBackgroundService] Block #1 created successfully by validator ValidatorName
```

### Step 5: Verify File Creation

Check that the blockchain data file was created:

```bash
ls -lah /home/user/blockchain-aid-tracker/src/BlockchainAidTracker.Api/Data/
```

You should see:
- `blockchain-data.json` - Main blockchain file
- `blockchain-data.json.YYYYMMDD-HHMMSS.bak` - Backup file (if this is not the first save)

### Step 6: View the Blockchain Data

```bash
cat /home/user/blockchain-aid-tracker/src/BlockchainAidTracker.Api/Data/blockchain-data.json
```

You should see JSON with:
- `chain` array with blocks (genesis + created blocks)
- `pendingTransactions` array
- `savedAt` timestamp
- `version` field

### Step 7: Restart the Application

Stop the application (Ctrl+C) and start it again:

```bash
dotnet run --project src/BlockchainAidTracker.Api/BlockchainAidTracker.Api.csproj
```

### Step 8: Verify Data Was Loaded

Check the startup logs. You should now see:

```
[Startup] Attempting to load blockchain from persistence: Data/blockchain-data.json
[JsonBlockchainPersistence] Blockchain loaded successfully: 2 blocks, 0 pending transactions (saved at 2025-11-16T...)
[Startup] Blockchain loaded successfully with 2 blocks and 0 pending transactions
```

The block count should match what you had before restart!

### Step 9: Verify in Blockchain Explorer

1. Open the Blazor Web UI: http://localhost:5002
2. Login with your credentials
3. Navigate to "Blockchain" in the menu
4. You should see all the blocks that were created before the restart

## Troubleshooting

### No blockchain-data.json file created

**Possible causes:**
1. No blocks have been created yet (wait 30 seconds after creating a transaction)
2. No transactions have been created (create at least one shipment)
3. Check logs for errors in `SaveToPersistenceAsync`

**Solution:**
Check the logs for errors. Ensure:
- `BlockchainPersistenceSettings:Enabled` is `true` in appsettings.json
- Data directory exists and is writable
- Background service is running

### Blockchain resets after restart

**Possible causes:**
1. File path is incorrect
2. File permissions issue
3. Persistence not enabled

**Solution:**
Check appsettings.json:
```json
"BlockchainPersistenceSettings": {
  "Enabled": true,
  "FilePath": "Data/blockchain-data.json",
  "AutoLoadOnStartup": true
}
```

### File exists but data isn't loaded

**Possible causes:**
1. `AutoLoadOnStartup` is false
2. Corrupted JSON file
3. Validation error during load

**Solution:**
Check startup logs for errors. Verify JSON is valid:
```bash
cat Data/blockchain-data.json | jq .
```

## Expected File Structure

```json
{
  "chain": [
    {
      "index": 0,
      "timestamp": "2025-11-16T00:00:00Z",
      "hash": "...",
      "previousHash": "0",
      "transactions": [],
      "validatorPublicKey": "GENESIS",
      "validatorSignature": ""
    },
    {
      "index": 1,
      "timestamp": "2025-11-16T14:30:00Z",
      "hash": "...",
      "previousHash": "...",
      "transactions": [
        {
          "id": "...",
          "type": "ShipmentCreated",
          "timestamp": "...",
          "senderPublicKey": "...",
          "payloadData": "...",
          "signature": "..."
        }
      ],
      "validatorPublicKey": "...",
      "validatorSignature": "..."
    }
  ],
  "pendingTransactions": [],
  "savedAt": "2025-11-16T14:30:05Z",
  "version": "1.0"
}
```

## Configuration Reference

Full configuration in `appsettings.json`:

```json
"BlockchainPersistenceSettings": {
  "Enabled": true,                          // Enable/disable persistence
  "FilePath": "Data/blockchain-data.json",  // Where to save the file
  "AutoSaveAfterBlockCreation": true,       // Save automatically after each block
  "AutoLoadOnStartup": true,                // Load automatically on startup
  "CreateBackup": true,                     // Create backup before saving
  "MaxBackupFiles": 5                       // Keep last 5 backups
}
```

## Success Indicators

✅ Persistence is working correctly if:
1. Log shows "Blockchain created with persistence support"
2. `blockchain-data.json` file is created after first block
3. Backup files (`.bak`) are created on subsequent saves
4. After restart, log shows "Blockchain loaded successfully with X blocks"
5. Block count matches before and after restart
6. Blockchain Explorer shows historical blocks after restart

## Additional Notes

- Blockchain is saved **after each block creation** (every 30 seconds when transactions exist)
- Backups are timestamped and automatically rotated
- Only the last 5 backups are kept by default
- File is excluded from git via `.gitignore`
- Persistence can be disabled by setting `Enabled: false` in appsettings.json
