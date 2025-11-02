using BlockchainAidTracker.Blockchain;
using BlockchainAidTracker.Core.Extensions;
using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.Cryptography;
using BlockchainAidTracker.DataAccess;
using BlockchainAidTracker.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;

Console.WriteLine("=== Blockchain Aid Tracker - Database & Blockchain Demo ===\n");

// ============================================
// PART 1: Database Setup & Test
// ============================================
Console.WriteLine("[PART 1: DATABASE OPERATIONS]\n");

// Create DbContext
var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "src", "BlockchainAidTracker.DataAccess", "blockchain-aid-tracker.db");
optionsBuilder.UseSqlite($"Data Source={dbPath}");
var dbContext = new ApplicationDbContext(optionsBuilder.Options);

// Verify database connection
Console.WriteLine("✓ Database connection established");
Console.WriteLine($"  Database: {dbContext.Database.GetConnectionString()}\n");

// Create repositories
var shipmentRepository = new ShipmentRepository(dbContext);
var userRepository = new UserRepository(dbContext);

// Initialize services
var hashService = new HashService();
var signatureService = new DigitalSignatureService();

// Generate key pairs for test users
var (coordinatorPublicKey, coordinatorPrivateKey) = signatureService.GenerateKeyPair();
var (recipientPublicKey, recipientPrivateKey) = signatureService.GenerateKeyPair();
var (donorPublicKey, donorPrivateKey) = signatureService.GenerateKeyPair();

Console.WriteLine("✓ Generated cryptographic key pairs for 3 users\n");

// Create test users
Console.WriteLine("--- Creating Test Users ---");

var coordinator = new User(
    username: "coordinator1",
    email: "coordinator@aidtracker.org",
    passwordHash: "$2a$11$dummy.hash.for.testing.purposes.only",
    publicKey: coordinatorPublicKey,
    encryptedPrivateKey: Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(coordinatorPrivateKey)),
    role: UserRole.Coordinator,
    firstName: "Alice",
    lastName: "Johnson",
    organization: "Global Aid Foundation"
);

var recipient = new User(
    username: "recipient1",
    email: "recipient@local.org",
    passwordHash: "$2a$11$dummy.hash.for.testing.purposes.only",
    publicKey: recipientPublicKey,
    encryptedPrivateKey: Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(recipientPrivateKey)),
    role: UserRole.Recipient,
    firstName: "Bob",
    lastName: "Smith",
    organization: "Local Clinic"
);

var donor = new User(
    username: "donor1",
    email: "donor@foundation.org",
    passwordHash: "$2a$11$dummy.hash.for.testing.purposes.only",
    publicKey: donorPublicKey,
    encryptedPrivateKey: Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(donorPrivateKey)),
    role: UserRole.Donor,
    firstName: "Carol",
    lastName: "Williams",
    organization: "Health Foundation Inc"
);

await userRepository.AddAsync(coordinator);
await userRepository.AddAsync(recipient);
await userRepository.AddAsync(donor);

Console.WriteLine($"✓ Created user: {coordinator.GetFullName()} ({coordinator.Role})");
Console.WriteLine($"✓ Created user: {recipient.GetFullName()} ({recipient.Role})");
Console.WriteLine($"✓ Created user: {donor.GetFullName()} ({donor.Role})\n");

// Create test shipment with items
Console.WriteLine("--- Creating Test Shipment ---");

var shipment = new Shipment(
    items: new List<ShipmentItem>(),
    origin: "Warehouse - New York, USA",
    destination: "Community Center - Nairobi, Kenya",
    expectedDeliveryTimeframe: "2025-11-15 to 2025-11-25",
    assignedRecipient: recipientPublicKey,
    coordinatorPublicKey: coordinatorPublicKey,
    donorPublicKey: donorPublicKey,
    notes: "Urgent medical supplies for community health initiative"
);

// Add items to shipment
shipment.AddItem(new ShipmentItem("Medical Masks (N95)", 500, "boxes", "Medical", 2500.00m));
shipment.AddItem(new ShipmentItem("Antibiotics (Amoxicillin)", 200, "bottles", "Medical", 1200.00m));
shipment.AddItem(new ShipmentItem("Bandages and Gauze", 1000, "units", "Medical", 800.00m));
shipment.AddItem(new ShipmentItem("Water Purification Tablets", 300, "boxes", "Water", 450.00m));

Console.WriteLine($"✓ Created shipment: {shipment.Id}");
Console.WriteLine($"  Origin: {shipment.Origin}");
Console.WriteLine($"  Destination: {shipment.Destination}");
Console.WriteLine($"  Items: {shipment.Items.Count}");
Console.WriteLine($"  Total Value: ${shipment.TotalEstimatedValue:N2}");
Console.WriteLine($"  QR Code: {shipment.QrCodeData}\n");

// Save shipment to database
await shipmentRepository.AddAsync(shipment);
Console.WriteLine("✓ Shipment saved to database\n");

// Test repository queries
Console.WriteLine("--- Testing Repository Queries ---");

var allShipments = await shipmentRepository.GetAllWithItemsAsync();
Console.WriteLine($"✓ Total shipments in database: {allShipments.Count}");

var allUsers = await userRepository.GetAllAsync();
Console.WriteLine($"✓ Total users in database: {allUsers.Count}");

var coordinators = await userRepository.GetByRoleAsync(UserRole.Coordinator);
Console.WriteLine($"✓ Coordinators in database: {coordinators.Count}");

var foundShipment = await shipmentRepository.GetByQrCodeAsync(shipment.QrCodeData);
Console.WriteLine($"✓ Found shipment by QR code: {foundShipment?.Id ?? "Not found"}");

var recipientShipments = await shipmentRepository.GetByRecipientAsync(recipientPublicKey);
Console.WriteLine($"✓ Shipments for recipient: {recipientShipments.Count}\n");

// ============================================
// PART 2: Blockchain Operations
// ============================================
Console.WriteLine("[PART 2: BLOCKCHAIN OPERATIONS]\n");

var (validatorPublicKey, validatorPrivateKey) = signatureService.GenerateKeyPair();
var blockchain = new Blockchain(hashService, signatureService);

Console.WriteLine("✓ Blockchain initialized with genesis block");
Console.WriteLine($"  Genesis Block Hash: {blockchain.Chain[0].Hash[..16]}...\n");

// Create blockchain transaction for shipment creation
var shipmentData = System.Text.Json.JsonSerializer.Serialize(new
{
    shipmentId = shipment.Id,
    origin = shipment.Origin,
    destination = shipment.Destination,
    totalValue = shipment.TotalEstimatedValue,
    itemCount = shipment.Items.Count,
    qrCode = shipment.QrCodeData
});

var transaction = new Transaction(
    TransactionType.ShipmentCreated,
    coordinatorPublicKey,
    shipmentData
);

transaction.Sign(coordinatorPrivateKey, signatureService);
Console.WriteLine("✓ Shipment transaction created and signed");
Console.WriteLine($"  Transaction ID: {transaction.Id}");
Console.WriteLine($"  Type: {transaction.Type}\n");

// Add to blockchain
blockchain.AddTransaction(transaction);
var newBlock = blockchain.CreateBlock(validatorPublicKey);
newBlock.SignBlock(validatorPrivateKey, signatureService);
blockchain.AddBlock(newBlock);

Console.WriteLine("✓ Transaction added to blockchain");
Console.WriteLine($"  Block Index: {newBlock.Index}");
Console.WriteLine($"  Block Hash: {newBlock.Hash[..16]}...");
Console.WriteLine($"  Blockchain Valid: {blockchain.IsValidChain()}\n");

// Update shipment status
Console.WriteLine("--- Simulating Shipment Lifecycle ---");

shipment.UpdateStatus(ShipmentStatus.Validated);
shipmentRepository.Update(shipment);
Console.WriteLine($"✓ Shipment status updated to: {shipment.Status}");

// Create status update transaction
var statusTransaction = new Transaction(
    TransactionType.StatusUpdated,
    coordinatorPublicKey,
    $"{{\"shipmentId\": \"{shipment.Id}\", \"newStatus\": \"{shipment.Status}\"}}"
);
statusTransaction.Sign(coordinatorPrivateKey, signatureService);
blockchain.AddTransaction(statusTransaction);

var statusBlock = blockchain.CreateBlock(validatorPublicKey);
statusBlock.SignBlock(validatorPrivateKey, signatureService);
blockchain.AddBlock(statusBlock);

Console.WriteLine($"✓ Status update recorded in blockchain (Block #{statusBlock.Index})\n");

// ============================================
// SUMMARY
// ============================================
Console.WriteLine("=== SUMMARY ===");
Console.WriteLine($"Database:");
Console.WriteLine($"  ├─ Users: {allUsers.Count}");
Console.WriteLine($"  ├─ Shipments: {allShipments.Count}");
Console.WriteLine($"  └─ Total Shipment Value: ${allShipments.Sum(s => s.TotalEstimatedValue):N2}");
Console.WriteLine();
Console.WriteLine($"Blockchain:");
Console.WriteLine($"  ├─ Total Blocks: {blockchain.GetChainLength()}");
Console.WriteLine($"  ├─ Total Transactions: {blockchain.Chain.Sum(b => b.Transactions.Count)}");
Console.WriteLine($"  └─ Chain Valid: {blockchain.IsValidChain()}");
Console.WriteLine();

// Display blocks
Console.WriteLine("Blockchain Blocks:");
foreach (var block in blockchain.Chain)
{
    Console.WriteLine($"  Block #{block.Index} - {block.Transactions.Count} tx - Hash: {block.Hash[..16]}...");
}

Console.WriteLine("\n=== Demo Complete! ===");
Console.WriteLine("\n💡 Next Steps:");
Console.WriteLine("   1. Check the database file: src/BlockchainAidTracker.DataAccess/blockchain-aid-tracker.db");
Console.WriteLine("   2. Use a SQLite browser to inspect tables (e.g., DB Browser for SQLite)");
Console.WriteLine("   3. Run 'dotnet test' to verify all tests still pass");
Console.WriteLine("   4. Implement Services layer for business logic");
