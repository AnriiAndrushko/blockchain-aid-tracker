using BlockchainAidTracker.Core.Interfaces;
using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlockchainAidTracker.DataAccess.Seeding;

/// <summary>
/// Seeds the database with initial demo data for testing and showcase
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Seeds the database with demo users and validators
    /// </summary>
    public static async Task SeedAsync(
        ApplicationDbContext context,
        IHashService hashService,
        IDigitalSignatureService signatureService,
        ILogger logger)
    {
        // Check if database is already seeded
        if (await context.Users.AnyAsync())
        {
            logger.LogInformation("Database already seeded, skipping...");
            return;
        }

        logger.LogInformation("Seeding database with demo data...");

        // Demo password (BCrypt hash for "Demo123!")
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("Demo123!", workFactor: 12);

        // Create demo users
        var users = new List<User>();

        // 1. Administrator
        var (adminPubKey, adminPrivKey) = signatureService.GenerateKeyPair();
        var adminUser = new User
        {
            Id = Guid.NewGuid().ToString(),
            Username = "admin",
            Email = "admin@aidtracker.demo",
            FirstName = "System",
            LastName = "Administrator",
            PasswordHash = passwordHash,
            PublicKey = adminPubKey,
            EncryptedPrivateKey = adminPrivKey, // In production, this should be encrypted with user password
            Role = UserRole.Administrator,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        users.Add(adminUser);

        // 2. Coordinator
        var (coordPubKey, coordPrivKey) = signatureService.GenerateKeyPair();
        var coordinatorUser = new User
        {
            Id = Guid.NewGuid().ToString(),
            Username = "coordinator",
            Email = "coordinator@aidtracker.demo",
            FirstName = "Sarah",
            LastName = "Johnson",
            PasswordHash = passwordHash,
            PublicKey = coordPubKey,
            EncryptedPrivateKey = coordPrivKey,
            Role = UserRole.Coordinator,
            Organization = "Global Aid Foundation",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        users.Add(coordinatorUser);

        // 3. Recipient
        var (recipientPubKey, recipientPrivKey) = signatureService.GenerateKeyPair();
        var recipientUser = new User
        {
            Id = Guid.NewGuid().ToString(),
            Username = "recipient",
            Email = "recipient@aidtracker.demo",
            FirstName = "Ahmed",
            LastName = "Hassan",
            PasswordHash = passwordHash,
            PublicKey = recipientPubKey,
            EncryptedPrivateKey = recipientPrivKey,
            Role = UserRole.Recipient,
            Organization = "Community Relief Center",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        users.Add(recipientUser);

        // 4. Donor
        var (donorPubKey, donorPrivKey) = signatureService.GenerateKeyPair();
        var donorUser = new User
        {
            Id = Guid.NewGuid().ToString(),
            Username = "donor",
            Email = "donor@aidtracker.demo",
            FirstName = "Maria",
            LastName = "Garcia",
            PasswordHash = passwordHash,
            PublicKey = donorPubKey,
            EncryptedPrivateKey = donorPrivKey,
            Role = UserRole.Donor,
            Organization = "International Donors Alliance",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        users.Add(donorUser);

        // 5. Logistics Partner
        var (logisticsPubKey, logisticsPrivKey) = signatureService.GenerateKeyPair();
        var logisticsUser = new User
        {
            Id = Guid.NewGuid().ToString(),
            Username = "logistics",
            Email = "logistics@aidtracker.demo",
            FirstName = "David",
            LastName = "Chen",
            PasswordHash = passwordHash,
            PublicKey = logisticsPubKey,
            EncryptedPrivateKey = logisticsPrivKey,
            Role = UserRole.LogisticsPartner,
            Organization = "Swift Logistics Inc.",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        users.Add(logisticsUser);

        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();

        logger.LogInformation("Created {UserCount} demo users", users.Count);

        // Create validators
        var validators = new List<Validator>();

        for (int i = 1; i <= 5; i++)
        {
            var (validatorPubKey, validatorPrivKey) = signatureService.GenerateKeyPair();
            var validator = new Validator
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"Validator-Node-{i}",
                PublicKey = validatorPubKey,
                EncryptedPrivateKey = validatorPrivKey, // In production, should be encrypted
                Priority = i,
                NetworkAddress = $"validator{i}.aidtracker.local:5000",
                IsActive = i <= 3, // Activate first 3 validators
                BlocksCreated = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            validators.Add(validator);
        }

        await context.Validators.AddRangeAsync(validators);
        await context.SaveChangesAsync();

        logger.LogInformation("Created {ValidatorCount} validators ({ActiveCount} active)",
            validators.Count, validators.Count(v => v.IsActive));

        // Create sample shipments in various states
        var shipments = new List<Shipment>();

        // Shipment 1: Created
        var shipment1 = new Shipment
        {
            Id = Guid.NewGuid().ToString(),
            Origin = "Distribution Center A, Nairobi",
            Destination = "Relief Camp Alpha, Kakuma",
            AssignedRecipient = recipientUser.Id,
            ExpectedDeliveryTimeframe = $"Expected by {DateTime.UtcNow.AddDays(7):yyyy-MM-dd}",
            Status = ShipmentStatus.Created,
            QrCodeData = Guid.NewGuid().ToString("N").Substring(0, 16),
            CoordinatorPublicKey = coordPubKey,
            CreatedTimestamp = DateTime.UtcNow.AddDays(-2),
            UpdatedTimestamp = DateTime.UtcNow.AddDays(-2),
            Items = new List<ShipmentItem>
            {
                new ShipmentItem
                {
                    Id = Guid.NewGuid().ToString(),
                    Description = "Rice Bags (50kg)",
                    Quantity = 100,
                    Unit = "bags",
                    Category = "Food"
                },
                new ShipmentItem
                {
                    Id = Guid.NewGuid().ToString(),
                    Description = "Water Purification Tablets",
                    Quantity = 500,
                    Unit = "boxes",
                    Category = "Medical"
                }
            }
        };
        shipments.Add(shipment1);

        // Shipment 2: In Transit
        var shipment2 = new Shipment
        {
            Id = Guid.NewGuid().ToString(),
            Origin = "Distribution Center B, Mombasa",
            Destination = "Relief Camp Beta, Dadaab",
            AssignedRecipient = recipientUser.Id,
            ExpectedDeliveryTimeframe = $"Expected by {DateTime.UtcNow.AddDays(5):yyyy-MM-dd}",
            Status = ShipmentStatus.InTransit,
            QrCodeData = Guid.NewGuid().ToString("N").Substring(0, 16),
            CoordinatorPublicKey = coordPubKey,
            CreatedTimestamp = DateTime.UtcNow.AddDays(-5),
            UpdatedTimestamp = DateTime.UtcNow.AddDays(-1),
            Items = new List<ShipmentItem>
            {
                new ShipmentItem
                {
                    Id = Guid.NewGuid().ToString(),
                    Description = "Medical Supplies Kit",
                    Quantity = 50,
                    Unit = "kits",
                    Category = "Medical"
                },
                new ShipmentItem
                {
                    Id = Guid.NewGuid().ToString(),
                    Description = "Blankets",
                    Quantity = 200,
                    Unit = "pieces",
                    Category = "Shelter"
                }
            }
        };
        shipments.Add(shipment2);

        // Shipment 3: Delivered
        var shipment3 = new Shipment
        {
            Id = Guid.NewGuid().ToString(),
            Origin = "Distribution Center C, Kisumu",
            Destination = "Relief Camp Gamma, Turkana",
            AssignedRecipient = recipientUser.Id,
            ExpectedDeliveryTimeframe = $"Expected by {DateTime.UtcNow.AddDays(-1):yyyy-MM-dd}",
            Status = ShipmentStatus.Delivered,
            QrCodeData = Guid.NewGuid().ToString("N").Substring(0, 16),
            CoordinatorPublicKey = coordPubKey,
            CreatedTimestamp = DateTime.UtcNow.AddDays(-10),
            UpdatedTimestamp = DateTime.UtcNow.AddHours(-2),
            Items = new List<ShipmentItem>
            {
                new ShipmentItem
                {
                    Id = Guid.NewGuid().ToString(),
                    Description = "Tents (Family Size)",
                    Quantity = 30,
                    Unit = "units",
                    Category = "Shelter"
                }
            }
        };
        shipments.Add(shipment3);

        await context.Shipments.AddRangeAsync(shipments);
        await context.SaveChangesAsync();

        logger.LogInformation("Created {ShipmentCount} sample shipments", shipments.Count);

        logger.LogInformation("Database seeding completed successfully!");
        logger.LogInformation("=== DEMO CREDENTIALS ===");
        logger.LogInformation("Username: admin | Password: Demo123! | Role: Administrator");
        logger.LogInformation("Username: coordinator | Password: Demo123! | Role: Coordinator");
        logger.LogInformation("Username: recipient | Password: Demo123! | Role: Recipient");
        logger.LogInformation("Username: donor | Password: Demo123! | Role: Donor");
        logger.LogInformation("Username: logistics | Password: Demo123! | Role: Logistics Partner");
        logger.LogInformation("========================");
    }
}
