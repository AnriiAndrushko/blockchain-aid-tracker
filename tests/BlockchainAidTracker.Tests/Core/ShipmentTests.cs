using BlockchainAidTracker.Core.Models;
using FluentAssertions;
using Xunit;

namespace BlockchainAidTracker.Tests.Core;

public class ShipmentTests
{
    [Fact]
    public void Constructor_Default_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var shipment = new Shipment();

        // Assert
        shipment.Id.Should().NotBeNullOrEmpty();
        shipment.Items.Should().NotBeNull().And.BeEmpty();
        shipment.Origin.Should().BeEmpty();
        shipment.Destination.Should().BeEmpty();
        shipment.ExpectedDeliveryTimeframe.Should().BeEmpty();
        shipment.AssignedRecipient.Should().BeEmpty();
        shipment.Status.Should().Be(ShipmentStatus.Created);
        shipment.QrCodeData.Should().BeEmpty();
        shipment.CoordinatorPublicKey.Should().BeEmpty();
        shipment.DonorPublicKey.Should().BeNull();
        shipment.Notes.Should().BeNull();
        shipment.TotalEstimatedValue.Should().Be(0);
        shipment.CreatedTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        shipment.UpdatedTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_Default_ShouldGenerateUniqueIds()
    {
        // Arrange & Act
        var shipment1 = new Shipment();
        var shipment2 = new Shipment();

        // Assert
        shipment1.Id.Should().NotBe(shipment2.Id);
    }

    [Fact]
    public void Constructor_Parameterized_ShouldInitializeWithProvidedValues()
    {
        // Arrange
        var items = new List<ShipmentItem>
        {
            new ShipmentItem("Medical Supplies", 100, "boxes", "Medical", 5000.00m),
            new ShipmentItem("Water Bottles", 500, "units", "Water", 1000.00m)
        };
        var origin = "Warehouse A";
        var destination = "Camp B";
        var timeframe = "2024-03-15 to 2024-03-20";
        var recipient = "recipient-public-key-123";
        var coordinator = "coordinator-public-key-456";
        var donor = "donor-public-key-789";
        var notes = "Urgent delivery required";

        // Act
        var shipment = new Shipment(items, origin, destination, timeframe, recipient, coordinator, donor, notes);

        // Assert
        shipment.Id.Should().NotBeNullOrEmpty();
        shipment.Items.Should().HaveCount(2);
        shipment.Origin.Should().Be(origin);
        shipment.Destination.Should().Be(destination);
        shipment.ExpectedDeliveryTimeframe.Should().Be(timeframe);
        shipment.AssignedRecipient.Should().Be(recipient);
        shipment.CoordinatorPublicKey.Should().Be(coordinator);
        shipment.DonorPublicKey.Should().Be(donor);
        shipment.Notes.Should().Be(notes);
        shipment.Status.Should().Be(ShipmentStatus.Created);
        shipment.QrCodeData.Should().NotBeNullOrEmpty();
        shipment.TotalEstimatedValue.Should().Be(6000.00m);
        shipment.CreatedTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        shipment.UpdatedTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_Parameterized_WithNullItems_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new Shipment(
            null!,
            "Origin",
            "Destination",
            "2024-03-15",
            "recipient",
            "coordinator");

        // Assert
        act.Should().Throw<ArgumentNullException>().WithMessage("*items*");
    }

    [Fact]
    public void Constructor_Parameterized_WithNullOrigin_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new Shipment(
            new List<ShipmentItem>(),
            null!,
            "Destination",
            "2024-03-15",
            "recipient",
            "coordinator");

        // Assert
        act.Should().Throw<ArgumentNullException>().WithMessage("*origin*");
    }

    [Fact]
    public void Constructor_Parameterized_WithNullDestination_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new Shipment(
            new List<ShipmentItem>(),
            "Origin",
            null!,
            "2024-03-15",
            "recipient",
            "coordinator");

        // Assert
        act.Should().Throw<ArgumentNullException>().WithMessage("*destination*");
    }

    [Fact]
    public void Constructor_Parameterized_WithNullTimeframe_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new Shipment(
            new List<ShipmentItem>(),
            "Origin",
            "Destination",
            null!,
            "recipient",
            "coordinator");

        // Assert
        act.Should().Throw<ArgumentNullException>().WithMessage("*expectedDeliveryTimeframe*");
    }

    [Fact]
    public void Constructor_Parameterized_WithNullRecipient_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new Shipment(
            new List<ShipmentItem>(),
            "Origin",
            "Destination",
            "2024-03-15",
            null!,
            "coordinator");

        // Assert
        act.Should().Throw<ArgumentNullException>().WithMessage("*assignedRecipient*");
    }

    [Fact]
    public void Constructor_Parameterized_WithNullCoordinator_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new Shipment(
            new List<ShipmentItem>(),
            "Origin",
            "Destination",
            "2024-03-15",
            "recipient",
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithMessage("*coordinatorPublicKey*");
    }

    [Fact]
    public void UpdateStatus_ShouldChangeStatusAndUpdateTimestamp()
    {
        // Arrange
        var shipment = new Shipment();
        var originalTimestamp = shipment.UpdatedTimestamp;
        Thread.Sleep(10); // Ensure timestamp difference

        // Act
        shipment.UpdateStatus(ShipmentStatus.Validated);

        // Assert
        shipment.Status.Should().Be(ShipmentStatus.Validated);
        shipment.UpdatedTimestamp.Should().BeAfter(originalTimestamp);
    }

    [Fact]
    public void AddItem_ShouldAddItemAndRecalculateTotalValue()
    {
        // Arrange
        var shipment = new Shipment();
        var item = new ShipmentItem("Medical Supplies", 100, "boxes", "Medical", 5000.00m);

        // Act
        shipment.AddItem(item);

        // Assert
        shipment.Items.Should().HaveCount(1);
        shipment.Items[0].Should().Be(item);
        shipment.TotalEstimatedValue.Should().Be(5000.00m);
    }

    [Fact]
    public void AddItem_WithMultipleItems_ShouldCalculateCorrectTotal()
    {
        // Arrange
        var shipment = new Shipment();
        var item1 = new ShipmentItem("Medical Supplies", 100, "boxes", "Medical", 5000.00m);
        var item2 = new ShipmentItem("Water Bottles", 500, "units", "Water", 1000.00m);
        var item3 = new ShipmentItem("Food Packages", 200, "boxes", "Food", 3000.00m);

        // Act
        shipment.AddItem(item1);
        shipment.AddItem(item2);
        shipment.AddItem(item3);

        // Assert
        shipment.Items.Should().HaveCount(3);
        shipment.TotalEstimatedValue.Should().Be(9000.00m);
    }

    [Fact]
    public void AddItem_WithNullItem_ShouldThrowArgumentNullException()
    {
        // Arrange
        var shipment = new Shipment();

        // Act
        Action act = () => shipment.AddItem(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithMessage("*item*");
    }

    [Fact]
    public void AddItem_ShouldUpdateTimestamp()
    {
        // Arrange
        var shipment = new Shipment();
        var originalTimestamp = shipment.UpdatedTimestamp;
        Thread.Sleep(10);
        var item = new ShipmentItem("Item", 10, "units", "Medical", 100.00m);

        // Act
        shipment.AddItem(item);

        // Assert
        shipment.UpdatedTimestamp.Should().BeAfter(originalTimestamp);
    }

    [Fact]
    public void RemoveItem_WithExistingItem_ShouldRemoveAndRecalculateTotal()
    {
        // Arrange
        var item1 = new ShipmentItem("Item 1", 10, "units", "Medical", 1000.00m);
        var item2 = new ShipmentItem("Item 2", 20, "units", "Food", 2000.00m);
        var items = new List<ShipmentItem> { item1, item2 };
        var shipment = new Shipment(items, "Origin", "Destination", "2024-03-15", "recipient", "coordinator");

        // Act
        var result = shipment.RemoveItem(item1.Id);

        // Assert
        result.Should().BeTrue();
        shipment.Items.Should().HaveCount(1);
        shipment.Items[0].Should().Be(item2);
        shipment.TotalEstimatedValue.Should().Be(2000.00m);
    }

    [Fact]
    public void RemoveItem_WithNonExistentItem_ShouldReturnFalse()
    {
        // Arrange
        var shipment = new Shipment();
        var nonExistentId = Guid.NewGuid().ToString();

        // Act
        var result = shipment.RemoveItem(nonExistentId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RemoveItem_ShouldUpdateTimestamp()
    {
        // Arrange
        var item = new ShipmentItem("Item", 10, "units", "Medical", 100.00m);
        var items = new List<ShipmentItem> { item };
        var shipment = new Shipment(items, "Origin", "Destination", "2024-03-15", "recipient", "coordinator");
        var originalTimestamp = shipment.UpdatedTimestamp;
        Thread.Sleep(10);

        // Act
        shipment.RemoveItem(item.Id);

        // Assert
        shipment.UpdatedTimestamp.Should().BeAfter(originalTimestamp);
    }

    [Fact]
    public void CalculateTotalValue_WithItemsHavingValues_ShouldReturnCorrectSum()
    {
        // Arrange
        var items = new List<ShipmentItem>
        {
            new ShipmentItem("Item 1", 10, "units", "Medical", 1000.00m),
            new ShipmentItem("Item 2", 20, "units", "Food", 2000.00m),
            new ShipmentItem("Item 3", 30, "units", "Water", 500.00m)
        };
        var shipment = new Shipment(items, "Origin", "Destination", "2024-03-15", "recipient", "coordinator");

        // Act
        var total = shipment.CalculateTotalValue();

        // Assert
        total.Should().Be(3500.00m);
    }

    [Fact]
    public void CalculateTotalValue_WithItemsWithoutValues_ShouldReturnZero()
    {
        // Arrange
        var items = new List<ShipmentItem>
        {
            new ShipmentItem("Item 1", 10, "units", "Medical"),
            new ShipmentItem("Item 2", 20, "units", "Food")
        };
        var shipment = new Shipment(items, "Origin", "Destination", "2024-03-15", "recipient", "coordinator");

        // Act
        var total = shipment.CalculateTotalValue();

        // Assert
        total.Should().Be(0);
    }

    [Fact]
    public void CalculateTotalValue_WithMixedItems_ShouldIgnoreNullValues()
    {
        // Arrange
        var items = new List<ShipmentItem>
        {
            new ShipmentItem("Item 1", 10, "units", "Medical", 1000.00m),
            new ShipmentItem("Item 2", 20, "units", "Food"),
            new ShipmentItem("Item 3", 30, "units", "Water", 500.00m)
        };
        var shipment = new Shipment(items, "Origin", "Destination", "2024-03-15", "recipient", "coordinator");

        // Act
        var total = shipment.CalculateTotalValue();

        // Assert
        total.Should().Be(1500.00m);
    }

    [Fact]
    public void GetSummary_ShouldReturnFormattedString()
    {
        // Arrange
        var items = new List<ShipmentItem>
        {
            new ShipmentItem("Item 1", 10, "units", "Medical", 1000.00m)
        };
        var shipment = new Shipment(items, "Warehouse A", "Camp B", "2024-03-15", "recipient", "coordinator");

        // Act
        var summary = shipment.GetSummary();

        // Assert
        summary.Should().Contain(shipment.Id);
        summary.Should().Contain("1 item");
        summary.Should().Contain("Warehouse A");
        summary.Should().Contain("Camp B");
        summary.Should().Contain("Created");
    }

    [Fact]
    public void GetSummary_WithMultipleItems_ShouldUsePluralForm()
    {
        // Arrange
        var items = new List<ShipmentItem>
        {
            new ShipmentItem("Item 1", 10, "units", "Medical", 1000.00m),
            new ShipmentItem("Item 2", 20, "units", "Food", 2000.00m)
        };
        var shipment = new Shipment(items, "Warehouse A", "Camp B", "2024-03-15", "recipient", "coordinator");

        // Act
        var summary = shipment.GetSummary();

        // Assert
        summary.Should().Contain("2 items");
    }

    [Fact]
    public void QrCodeData_ShouldBeGeneratedAutomatically()
    {
        // Arrange & Act
        var items = new List<ShipmentItem>
        {
            new ShipmentItem("Item", 10, "units", "Medical", 100.00m)
        };
        var shipment = new Shipment(items, "Origin", "Destination", "2024-03-15", "recipient", "coordinator");

        // Assert
        shipment.QrCodeData.Should().NotBeNullOrEmpty();
        shipment.QrCodeData.Should().StartWith("SHIPMENT-");
        shipment.QrCodeData.Should().Contain(shipment.Id);
    }

    [Theory]
    [InlineData(ShipmentStatus.Created, ShipmentStatus.Validated, true)]
    [InlineData(ShipmentStatus.Created, ShipmentStatus.InTransit, false)]
    [InlineData(ShipmentStatus.Created, ShipmentStatus.Delivered, false)]
    [InlineData(ShipmentStatus.Created, ShipmentStatus.Confirmed, false)]
    [InlineData(ShipmentStatus.Validated, ShipmentStatus.InTransit, true)]
    [InlineData(ShipmentStatus.Validated, ShipmentStatus.Delivered, false)]
    [InlineData(ShipmentStatus.InTransit, ShipmentStatus.Delivered, true)]
    [InlineData(ShipmentStatus.InTransit, ShipmentStatus.Confirmed, false)]
    [InlineData(ShipmentStatus.Delivered, ShipmentStatus.Confirmed, true)]
    [InlineData(ShipmentStatus.Delivered, ShipmentStatus.Created, false)]
    [InlineData(ShipmentStatus.Confirmed, ShipmentStatus.Created, false)]
    [InlineData(ShipmentStatus.Confirmed, ShipmentStatus.Validated, false)]
    public void CanTransitionTo_ShouldValidateStateTransitions(
        ShipmentStatus currentStatus, ShipmentStatus newStatus, bool expectedResult)
    {
        // Arrange
        var shipment = new Shipment();
        shipment.UpdateStatus(currentStatus);

        // Act
        var result = shipment.CanTransitionTo(newStatus);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void CanTransitionTo_FromConfirmedStatus_ShouldAlwaysReturnFalse()
    {
        // Arrange
        var shipment = new Shipment();
        shipment.UpdateStatus(ShipmentStatus.Confirmed);

        // Act & Assert
        shipment.CanTransitionTo(ShipmentStatus.Created).Should().BeFalse();
        shipment.CanTransitionTo(ShipmentStatus.Validated).Should().BeFalse();
        shipment.CanTransitionTo(ShipmentStatus.InTransit).Should().BeFalse();
        shipment.CanTransitionTo(ShipmentStatus.Delivered).Should().BeFalse();
        shipment.CanTransitionTo(ShipmentStatus.Confirmed).Should().BeFalse();
    }

    [Fact]
    public void CompleteLifecycle_ShouldTransitionThroughAllStates()
    {
        // Arrange
        var items = new List<ShipmentItem>
        {
            new ShipmentItem("Medical Supplies", 100, "boxes", "Medical", 5000.00m)
        };
        var shipment = new Shipment(items, "Warehouse", "Camp", "2024-03-15", "recipient", "coordinator");

        // Act & Assert
        shipment.Status.Should().Be(ShipmentStatus.Created);

        shipment.UpdateStatus(ShipmentStatus.Validated);
        shipment.Status.Should().Be(ShipmentStatus.Validated);

        shipment.UpdateStatus(ShipmentStatus.InTransit);
        shipment.Status.Should().Be(ShipmentStatus.InTransit);

        shipment.UpdateStatus(ShipmentStatus.Delivered);
        shipment.Status.Should().Be(ShipmentStatus.Delivered);

        shipment.UpdateStatus(ShipmentStatus.Confirmed);
        shipment.Status.Should().Be(ShipmentStatus.Confirmed);
    }
}
