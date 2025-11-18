using BlockchainAidTracker.Core.Models;

namespace BlockchainAidTracker.Tests.Infrastructure;

/// <summary>
/// Builder for creating test User entities with fluent API
/// </summary>
public class UserBuilder
{
    private string _id = Guid.NewGuid().ToString();
    private string _username = "testuser";
    private string _email = "test@example.com";
    private string _passwordHash = "$2a$11$test.hash.value";
    private string _publicKey = "test-public-key-" + Guid.NewGuid().ToString();
    private string _encryptedPrivateKey = "encrypted-private-key";
    private UserRole _role = UserRole.Recipient;
    private string _firstName = "Test";
    private string _lastName = "User";
    private string? _organization = null;
    private string? _phoneNumber = null;
    private bool _isActive = true;

    public UserBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public UserBuilder WithUsername(string username)
    {
        _username = username;
        return this;
    }

    public UserBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public UserBuilder WithPasswordHash(string passwordHash)
    {
        _passwordHash = passwordHash;
        return this;
    }

    public UserBuilder WithPublicKey(string publicKey)
    {
        _publicKey = publicKey;
        return this;
    }

    public UserBuilder WithRole(UserRole role)
    {
        _role = role;
        return this;
    }

    public UserBuilder WithName(string firstName, string lastName)
    {
        _firstName = firstName;
        _lastName = lastName;
        return this;
    }

    public UserBuilder WithOrganization(string organization)
    {
        _organization = organization;
        return this;
    }

    public UserBuilder WithPhoneNumber(string phoneNumber)
    {
        _phoneNumber = phoneNumber;
        return this;
    }

    public UserBuilder AsInactive()
    {
        _isActive = false;
        return this;
    }

    public UserBuilder AsCoordinator()
    {
        _role = UserRole.Coordinator;
        return this;
    }

    public UserBuilder AsDonor()
    {
        _role = UserRole.Donor;
        return this;
    }

    public UserBuilder AsValidator()
    {
        _role = UserRole.Validator;
        return this;
    }

    public UserBuilder AsAdministrator()
    {
        _role = UserRole.Administrator;
        return this;
    }

    public UserBuilder AsRecipient()
    {
        _role = UserRole.Recipient;
        return this;
    }

    public UserBuilder AsLogisticsPartner()
    {
        _role = UserRole.LogisticsPartner;
        return this;
    }

    public User Build()
    {
        return new User(
            username: _username,
            email: _email,
            passwordHash: _passwordHash,
            publicKey: _publicKey,
            encryptedPrivateKey: _encryptedPrivateKey,
            role: _role,
            firstName: _firstName,
            lastName: _lastName,
            organization: _organization,
            phoneNumber: _phoneNumber
        )
        {
            Id = _id,
            IsActive = _isActive
        };
    }
}

/// <summary>
/// Builder for creating test Shipment entities with fluent API
/// </summary>
public class ShipmentBuilder
{
    private string _id = Guid.NewGuid().ToString();
    private List<ShipmentItem> _items = new();
    private string _origin = "Test Origin";
    private string _destination = "Test Destination";
    private string _expectedDeliveryTimeframe = "2025-12-01 to 2025-12-15";
    private string _assignedRecipient = "recipient-public-key";
    private string _coordinatorPublicKey = "coordinator-public-key";
    private string? _donorPublicKey = null;
    private string? _notes = null;
    private ShipmentStatus _status = ShipmentStatus.Created;

    public ShipmentBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public ShipmentBuilder WithOrigin(string origin)
    {
        _origin = origin;
        return this;
    }

    public ShipmentBuilder WithDestination(string destination)
    {
        _destination = destination;
        return this;
    }

    public ShipmentBuilder WithDeliveryTimeframe(string timeframe)
    {
        _expectedDeliveryTimeframe = timeframe;
        return this;
    }

    public ShipmentBuilder WithRecipient(string recipientPublicKey)
    {
        _assignedRecipient = recipientPublicKey;
        return this;
    }

    public ShipmentBuilder WithCoordinator(string coordinatorPublicKey)
    {
        _coordinatorPublicKey = coordinatorPublicKey;
        return this;
    }

    public ShipmentBuilder WithDonor(string donorPublicKey)
    {
        _donorPublicKey = donorPublicKey;
        return this;
    }

    public ShipmentBuilder WithNotes(string notes)
    {
        _notes = notes;
        return this;
    }

    public ShipmentBuilder WithStatus(ShipmentStatus status)
    {
        _status = status;
        return this;
    }

    public ShipmentBuilder WithItem(string description, int quantity, string unit, string category, decimal? value = null)
    {
        _items.Add(new ShipmentItem(description, quantity, unit, category, value));
        return this;
    }

    public ShipmentBuilder WithItems(params ShipmentItem[] items)
    {
        _items.AddRange(items);
        return this;
    }

    public ShipmentBuilder WithMedicalSupplies()
    {
        WithItem("Medical Masks", 100, "boxes", "Medical", 500.00m);
        WithItem("Bandages", 200, "units", "Medical", 150.00m);
        return this;
    }

    public ShipmentBuilder AsValidated()
    {
        _status = ShipmentStatus.Validated;
        return this;
    }

    public ShipmentBuilder AsInTransit()
    {
        _status = ShipmentStatus.InTransit;
        return this;
    }

    public ShipmentBuilder AsDelivered()
    {
        _status = ShipmentStatus.Delivered;
        return this;
    }

    public Shipment Build()
    {
        var shipment = new Shipment(
            items: _items,
            origin: _origin,
            destination: _destination,
            expectedDeliveryTimeframe: _expectedDeliveryTimeframe,
            assignedRecipient: _assignedRecipient,
            coordinatorPublicKey: _coordinatorPublicKey,
            donorPublicKey: _donorPublicKey,
            notes: _notes
        )
        {
            Id = _id
        };

        // Update status if different from Created
        if (_status != ShipmentStatus.Created)
        {
            shipment.UpdateStatus(_status);
        }

        return shipment;
    }
}

/// <summary>
/// Builder for creating test Validator entities with fluent API
/// </summary>
public class ValidatorBuilder
{
    private string _id = Guid.NewGuid().ToString();
    private string _name = "Test-Validator-" + Guid.NewGuid().ToString().Substring(0, 8);
    private string _publicKey = "test-validator-public-key-" + Guid.NewGuid().ToString();
    private string _encryptedPrivateKey = "encrypted-validator-private-key";
    private int _priority = 0;
    private string? _address = null;
    private string? _description = null;
    private bool _isActive = true;
    private int _totalBlocksCreated = 0;
    private DateTime? _lastBlockCreatedTimestamp = null;

    public ValidatorBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public ValidatorBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public ValidatorBuilder WithPublicKey(string publicKey)
    {
        _publicKey = publicKey;
        return this;
    }

    public ValidatorBuilder WithPriority(int priority)
    {
        _priority = priority;
        return this;
    }

    public ValidatorBuilder WithAddress(string address)
    {
        _address = address;
        return this;
    }

    public ValidatorBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public ValidatorBuilder AsInactive()
    {
        _isActive = false;
        return this;
    }

    public ValidatorBuilder WithBlocksCreated(int count)
    {
        _totalBlocksCreated = count;
        if (count > 0)
        {
            _lastBlockCreatedTimestamp = DateTime.UtcNow.AddMinutes(-30);
        }
        return this;
    }

    public Validator Build()
    {
        var validator = new Validator(
            name: _name,
            publicKey: _publicKey,
            encryptedPrivateKey: _encryptedPrivateKey,
            priority: _priority,
            address: _address,
            description: _description
        )
        {
            Id = _id,
            IsActive = _isActive,
            TotalBlocksCreated = _totalBlocksCreated,
            LastBlockCreatedTimestamp = _lastBlockCreatedTimestamp
        };

        return validator;
    }
}

/// <summary>
/// Static helper class for creating test data
/// </summary>
public static class TestData
{
    public static UserBuilder CreateUser() => new UserBuilder();

    public static ShipmentBuilder CreateShipment() => new ShipmentBuilder();

    public static ValidatorBuilder CreateValidator() => new ValidatorBuilder();

    public static ShipmentItem CreateShipmentItem(
        string description = "Test Item",
        int quantity = 10,
        string unit = "units",
        string category = "General",
        decimal? estimatedValue = 100.00m)
    {
        return new ShipmentItem(description, quantity, unit, category, estimatedValue);
    }
}

