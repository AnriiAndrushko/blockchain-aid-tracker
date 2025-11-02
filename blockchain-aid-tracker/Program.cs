using BlockchainAidTracker.Blockchain;
using BlockchainAidTracker.Core.Extensions;
using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.Cryptography;

Console.WriteLine("=== Blockchain Aid Tracker - Demo ===\n");

// Initialize services
var hashService = new HashService();
var signatureService = new DigitalSignatureService();

// Create blockchain
var blockchain = new Blockchain(hashService, signatureService);

Console.WriteLine("✓ Blockchain initialized with genesis block");
Console.WriteLine($"  Genesis Block Hash: {blockchain.Chain[0].Hash[..16]}...\n");

// Generate key pairs for coordinator and validator
var (coordinatorPublicKey, coordinatorPrivateKey) = signatureService.GenerateKeyPair();
var (validatorPublicKey, validatorPrivateKey) = signatureService.GenerateKeyPair();

Console.WriteLine("✓ Key pairs generated");
Console.WriteLine($"  Coordinator Public Key: {coordinatorPublicKey[..30]}...");
Console.WriteLine($"  Validator Public Key: {validatorPublicKey[..30]}...\n");

// Create a shipment transaction
var shipmentData = @"{
    ""shipmentId"": ""SHP-001"",
    ""items"": ""Medical Supplies"",
    ""quantity"": 100,
    ""origin"": ""Warehouse A"",
    ""destination"": ""Hospital B""
}";

var transaction = new Transaction(
    TransactionType.ShipmentCreated,
    coordinatorPublicKey,
    shipmentData
);

// Sign the transaction
transaction.Sign(coordinatorPrivateKey, signatureService);

Console.WriteLine("✓ Transaction created and signed");
Console.WriteLine($"  Transaction ID: {transaction.Id}");
Console.WriteLine($"  Type: {transaction.Type}");
Console.WriteLine($"  Signature: {transaction.Signature[..30]}...\n");

// Verify transaction signature
var isValidTransaction = transaction.VerifySignature(signatureService);
Console.WriteLine($"✓ Transaction signature verified: {isValidTransaction}\n");

// Add transaction to blockchain
blockchain.AddTransaction(transaction);
Console.WriteLine("✓ Transaction added to pending pool\n");

// Create a block
var newBlock = blockchain.CreateBlock(validatorPublicKey);

// Sign the block with validator's private key
newBlock.SignBlock(validatorPrivateKey, signatureService);

Console.WriteLine("✓ Block created and signed by validator");
Console.WriteLine($"  Block Index: {newBlock.Index}");
Console.WriteLine($"  Block Hash: {newBlock.Hash[..16]}...");
Console.WriteLine($"  Transactions: {newBlock.Transactions.Count}");
Console.WriteLine($"  Validator Signature: {newBlock.ValidatorSignature[..30]}...\n");

// Verify block signature
var isValidBlock = newBlock.VerifyValidatorSignature(signatureService);
Console.WriteLine($"✓ Block signature verified: {isValidBlock}\n");

// Add block to chain
var blockAdded = blockchain.AddBlock(newBlock);
Console.WriteLine($"✓ Block added to chain: {blockAdded}\n");

// Validate entire chain
var isValidChain = blockchain.IsValidChain();
Console.WriteLine($"✓ Blockchain validation: {isValidChain}\n");

// Display chain summary
Console.WriteLine("=== Blockchain Summary ===");
Console.WriteLine($"Total Blocks: {blockchain.GetChainLength()}");
Console.WriteLine($"Pending Transactions: {blockchain.PendingTransactions.Count}");

Console.WriteLine("\nBlocks in chain:");
foreach (var block in blockchain.Chain)
{
    Console.WriteLine($"  Block #{block.Index} - Hash: {block.Hash[..16]}... - Transactions: {block.Transactions.Count}");
}

Console.WriteLine("\n=== Demo Complete ===");