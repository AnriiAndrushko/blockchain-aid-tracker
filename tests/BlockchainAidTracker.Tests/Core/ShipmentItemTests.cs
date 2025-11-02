using BlockchainAidTracker.Core.Models;
using FluentAssertions;
using Xunit;

namespace BlockchainAidTracker.Tests.Core;

public class ShipmentItemTests
{
    [Fact]
    public void Constructor_Default_ShouldInitializeWithUniqueId()
    {
        // Arrange & Act
        var item = new ShipmentItem();

        // Assert
        item.Id.Should().NotBeNullOrEmpty();
        item.Description.Should().BeEmpty();
        item.Unit.Should().BeEmpty();
        item.Category.Should().BeEmpty();
        item.Quantity.Should().Be(0);
        item.EstimatedValue.Should().BeNull();
    }

    [Fact]
    public void Constructor_Default_ShouldGenerateUniqueIds()
    {
        // Arrange & Act
        var item1 = new ShipmentItem();
        var item2 = new ShipmentItem();

        // Assert
        item1.Id.Should().NotBe(item2.Id);
    }

    [Fact]
    public void Constructor_Parameterized_ShouldInitializeWithProvidedValues()
    {
        // Arrange
        var description = "Medical Supplies - Bandages";
        var quantity = 100;
        var unit = "boxes";
        var category = "Medical";
        var estimatedValue = 5000.00m;

        // Act
        var item = new ShipmentItem(description, quantity, unit, category, estimatedValue);

        // Assert
        item.Id.Should().NotBeNullOrEmpty();
        item.Description.Should().Be(description);
        item.Quantity.Should().Be(quantity);
        item.Unit.Should().Be(unit);
        item.Category.Should().Be(category);
        item.EstimatedValue.Should().Be(estimatedValue);
    }

    [Fact]
    public void Constructor_Parameterized_WithNullEstimatedValue_ShouldInitializeWithoutValue()
    {
        // Arrange
        var description = "Food Supplies";
        var quantity = 50;
        var unit = "kg";
        var category = "Food";

        // Act
        var item = new ShipmentItem(description, quantity, unit, category);

        // Assert
        item.Id.Should().NotBeNullOrEmpty();
        item.Description.Should().Be(description);
        item.Quantity.Should().Be(quantity);
        item.Unit.Should().Be(unit);
        item.Category.Should().Be(category);
        item.EstimatedValue.Should().BeNull();
    }

    [Fact]
    public void Constructor_Parameterized_WithNullDescription_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new ShipmentItem(null!, 10, "units", "Medical");

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithMessage("*description*");
    }

    [Fact]
    public void Constructor_Parameterized_WithNullUnit_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new ShipmentItem("Item", 10, null!, "Medical");

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithMessage("*unit*");
    }

    [Fact]
    public void Constructor_Parameterized_WithNullCategory_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new ShipmentItem("Item", 10, "units", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithMessage("*category*");
    }

    [Fact]
    public void Constructor_Parameterized_WithZeroQuantity_ShouldThrowArgumentException()
    {
        // Arrange & Act
        Action act = () => new ShipmentItem("Item", 0, "units", "Medical");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Quantity must be greater than 0*");
    }

    [Fact]
    public void Constructor_Parameterized_WithNegativeQuantity_ShouldThrowArgumentException()
    {
        // Arrange & Act
        Action act = () => new ShipmentItem("Item", -10, "units", "Medical");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Quantity must be greater than 0*");
    }

    [Theory]
    [InlineData("Medical Supplies", 100, "boxes", "Medical", 5000.00)]
    [InlineData("Water Bottles", 500, "units", "Water", 1000.00)]
    [InlineData("Tents", 20, "units", "Shelter", 10000.00)]
    [InlineData("Rice", 1000, "kg", "Food", 2000.00)]
    public void Constructor_WithVariousCategories_ShouldCreateValidItems(
        string description, int quantity, string unit, string category, decimal value)
    {
        // Act
        var item = new ShipmentItem(description, quantity, unit, category, value);

        // Assert
        item.Description.Should().Be(description);
        item.Quantity.Should().Be(quantity);
        item.Unit.Should().Be(unit);
        item.Category.Should().Be(category);
        item.EstimatedValue.Should().Be(value);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var item = new ShipmentItem();

        // Act
        item.Description = "Updated Description";
        item.Quantity = 200;
        item.Unit = "liters";
        item.Category = "Water";
        item.EstimatedValue = 3000.00m;

        // Assert
        item.Description.Should().Be("Updated Description");
        item.Quantity.Should().Be(200);
        item.Unit.Should().Be("liters");
        item.Category.Should().Be("Water");
        item.EstimatedValue.Should().Be(3000.00m);
    }

    [Fact]
    public void EstimatedValue_CanBeSetToNull()
    {
        // Arrange
        var item = new ShipmentItem("Item", 10, "units", "Medical", 1000.00m);

        // Act
        item.EstimatedValue = null;

        // Assert
        item.EstimatedValue.Should().BeNull();
    }

    [Fact]
    public void Constructor_Parameterized_ShouldGenerateUniqueIdsForDifferentItems()
    {
        // Act
        var item1 = new ShipmentItem("Item 1", 10, "units", "Medical");
        var item2 = new ShipmentItem("Item 2", 20, "boxes", "Food");

        // Assert
        item1.Id.Should().NotBe(item2.Id);
    }
}
