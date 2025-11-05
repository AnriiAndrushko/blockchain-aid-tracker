using System.Text.Json;
using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.SmartContracts.Contracts;
using BlockchainAidTracker.SmartContracts.Models;
using FluentAssertions;

namespace BlockchainAidTracker.Tests.SmartContracts;

public class ShipmentTrackingContractTests
{
    private readonly ShipmentTrackingContract _contract;

    public ShipmentTrackingContractTests()
    {
        _contract = new ShipmentTrackingContract();
    }

    [Fact]
    public void Contract_ShouldHaveCorrectMetadata()
    {
        // Assert
        _contract.ContractId.Should().Be("shipment-tracking-v1");
        _contract.Name.Should().Be("Shipment Tracking Contract");
        _contract.Description.Should().NotBeNullOrEmpty();
        _contract.Version.Should().Be("1.0.0");
    }

    [Theory]
    [InlineData(TransactionType.ShipmentCreated)]
    [InlineData(TransactionType.StatusUpdated)]
    public void CanExecute_WithShipmentTransactions_ShouldReturnTrue(TransactionType type)
    {
        // Arrange
        var transaction = new Transaction(type, "sender-key", "{}");
        var context = new ContractExecutionContext(transaction);

        // Act
        var result = _contract.CanExecute(context);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanExecute_WithDeliveryConfirmedTransaction_ShouldReturnFalse()
    {
        // Arrange
        var transaction = new Transaction(TransactionType.DeliveryConfirmed, "sender-key", "{}");
        var context = new ContractExecutionContext(transaction);

        // Act
        var result = _contract.CanExecute(context);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_WithShipmentCreated_ShouldInitializeShipment()
    {
        // Arrange
        var shipmentId = "shipment-123";
        var payload = new Dictionary<string, object>
        {
            { "ShipmentId", shipmentId },
            { "Origin", "New York" },
            { "Destination", "London" },
            { "RecipientId", "recipient-key" }
        };

        var transaction = new Transaction(TransactionType.ShipmentCreated, "coordinator-key",
            JsonSerializer.Serialize(payload));
        var context = new ContractExecutionContext(transaction);

        // Act
        var result = await _contract.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().ContainKey("shipmentId");
        result.Output["shipmentId"].Should().Be(shipmentId);
        result.Output.Should().ContainKey("status");
        result.Output["status"].Should().Be(ShipmentStatus.Created.ToString());
        result.Output.Should().ContainKey("initialized");
        result.Output["initialized"].Should().Be(true);
    }

    [Fact]
    public async Task ExecuteAsync_WithShipmentCreated_ShouldUpdateState()
    {
        // Arrange
        var shipmentId = "shipment-123";
        var coordinatorKey = "coordinator-key";
        var payload = new Dictionary<string, object>
        {
            { "ShipmentId", shipmentId },
            { "Origin", "New York" },
            { "Destination", "London" },
            { "RecipientId", "recipient-key" }
        };

        var transaction = new Transaction(TransactionType.ShipmentCreated, coordinatorKey,
            JsonSerializer.Serialize(payload));
        var context = new ContractExecutionContext(transaction);

        // Act
        var result = await _contract.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.StateChanges.Should().ContainKey($"shipment_{shipmentId}_status");
        result.StateChanges.Should().ContainKey($"shipment_{shipmentId}_createdBy");
        result.StateChanges.Should().ContainKey($"shipment_{shipmentId}_createdAt");
        result.StateChanges[$"shipment_{shipmentId}_createdBy"].Should().Be(coordinatorKey);
    }

    [Fact]
    public async Task ExecuteAsync_WithShipmentCreated_ShouldEmitEvent()
    {
        // Arrange
        var shipmentId = "shipment-123";
        var payload = new Dictionary<string, object>
        {
            { "ShipmentId", shipmentId },
            { "Origin", "New York" },
            { "Destination", "London" },
            { "RecipientId", "recipient-key" }
        };

        var transaction = new Transaction(TransactionType.ShipmentCreated, "coordinator-key",
            JsonSerializer.Serialize(payload));
        var context = new ContractExecutionContext(transaction);

        // Act
        var result = await _contract.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.Events.Should().Contain(e => e.Name == "ShipmentCreated");
        var createdEvent = result.Events.First(e => e.Name == "ShipmentCreated");
        createdEvent.Data.Should().ContainKey("shipmentId");
        createdEvent.Data.Should().ContainKey("origin");
        createdEvent.Data.Should().ContainKey("destination");
    }

    [Theory]
    [InlineData("Origin")]
    [InlineData("Destination")]
    [InlineData("RecipientId")]
    public async Task ExecuteAsync_WithMissingRequiredField_ShouldFail(string missingField)
    {
        // Arrange
        var payload = new Dictionary<string, object>
        {
            { "ShipmentId", "shipment-123" },
            { "Origin", "New York" },
            { "Destination", "London" },
            { "RecipientId", "recipient-key" }
        };
        payload.Remove(missingField);

        var transaction = new Transaction(TransactionType.ShipmentCreated, "coordinator-key",
            JsonSerializer.Serialize(payload));
        var context = new ContractExecutionContext(transaction);

        // Act
        var result = await _contract.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain(missingField);
    }

    [Fact]
    public async Task ExecuteAsync_WithShipmentCreatedWithItems_ShouldAutoValidate()
    {
        // Arrange
        var shipmentId = "shipment-123";
        var items = new[]
        {
            new { name = "Item 1", quantity = 10 },
            new { name = "Item 2", quantity = 5 }
        };
        var payload = new Dictionary<string, object>
        {
            { "ShipmentId", shipmentId },
            { "Origin", "New York" },
            { "Destination", "London" },
            { "RecipientId", "recipient-key" },
            { "Items", items }
        };

        var transaction = new Transaction(TransactionType.ShipmentCreated, "coordinator-key",
            JsonSerializer.Serialize(payload));
        var context = new ContractExecutionContext(transaction);

        // Act
        var result = await _contract.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.Output["status"].Should().Be(ShipmentStatus.Validated.ToString());
        result.Output.Should().ContainKey("autoValidated");
        result.Output["autoValidated"].Should().Be(true);
        result.Events.Should().Contain(e => e.Name == "ShipmentAutoValidated");
    }

    [Fact]
    public async Task ExecuteAsync_WithStatusUpdate_ShouldUpdateStatus()
    {
        // Arrange
        var shipmentId = "shipment-123";

        // First create the shipment
        var createPayload = new Dictionary<string, object>
        {
            { "ShipmentId", shipmentId },
            { "Origin", "New York" },
            { "Destination", "London" },
            { "RecipientId", "recipient-key" }
        };
        var createTransaction = new Transaction(TransactionType.ShipmentCreated, "coordinator-key",
            JsonSerializer.Serialize(createPayload));
        var createContext = new ContractExecutionContext(createTransaction);
        var createResult = await _contract.ExecuteAsync(createContext);

        // Apply state changes from creation
        _contract.UpdateState(createResult.StateChanges);

        // Now update the status
        var updatePayload = new Dictionary<string, object>
        {
            { "ShipmentId", shipmentId },
            { "NewStatus", ShipmentStatus.Validated.ToString() }
        };
        var updateTransaction = new Transaction(TransactionType.StatusUpdated, "coordinator-key",
            JsonSerializer.Serialize(updatePayload));
        var updateContext = new ContractExecutionContext(updateTransaction);

        // Act
        var result = await _contract.ExecuteAsync(updateContext);

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().ContainKey("newStatus");
        result.Output["newStatus"].Should().Be(ShipmentStatus.Validated.ToString());
        result.Output.Should().ContainKey("transitionValid");
        result.Output["transitionValid"].Should().Be(true);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidStatusTransition_ShouldFail()
    {
        // Arrange
        var shipmentId = "shipment-123";

        // First create the shipment
        var createPayload = new Dictionary<string, object>
        {
            { "ShipmentId", shipmentId },
            { "Origin", "New York" },
            { "Destination", "London" },
            { "RecipientId", "recipient-key" }
        };
        var createTransaction = new Transaction(TransactionType.ShipmentCreated, "coordinator-key",
            JsonSerializer.Serialize(createPayload));
        var createContext = new ContractExecutionContext(createTransaction);
        var createResult = await _contract.ExecuteAsync(createContext);

        // Apply state changes from creation
        _contract.UpdateState(createResult.StateChanges);

        // Try to update directly to Delivered (invalid transition from Created)
        var updatePayload = new Dictionary<string, object>
        {
            { "ShipmentId", shipmentId },
            { "NewStatus", ShipmentStatus.Delivered.ToString() }
        };
        var updateTransaction = new Transaction(TransactionType.StatusUpdated, "coordinator-key",
            JsonSerializer.Serialize(updatePayload));
        var updateContext = new ContractExecutionContext(updateTransaction);

        // Act
        var result = await _contract.ExecuteAsync(updateContext);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid state transition");
        result.Events.Should().Contain(e => e.Name == "InvalidStateTransition");
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentShipment_ShouldFail()
    {
        // Arrange
        var updatePayload = new Dictionary<string, object>
        {
            { "shipmentId", "non-existent-shipment" },
            { "newStatus", ShipmentStatus.Validated.ToString() }
        };
        var transaction = new Transaction(TransactionType.StatusUpdated, "coordinator-key",
            JsonSerializer.Serialize(updatePayload));
        var context = new ContractExecutionContext(transaction);

        // Act
        var result = await _contract.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task ExecuteAsync_WhenShipmentReachesDelivered_ShouldEmitEvent()
    {
        // Arrange
        var shipmentId = "shipment-123";

        // Create shipment
        var createPayload = new Dictionary<string, object>
        {
            { "ShipmentId", shipmentId },
            { "Origin", "New York" },
            { "Destination", "London" },
            { "RecipientId", "recipient-key" }
        };
        var createTransaction = new Transaction(TransactionType.ShipmentCreated, "coordinator-key",
            JsonSerializer.Serialize(createPayload));
        var createResult = await _contract.ExecuteAsync(new ContractExecutionContext(createTransaction));
        _contract.UpdateState(createResult.StateChanges);

        // Validated
        var validatedPayload = new Dictionary<string, object>
        {
            { "ShipmentId", shipmentId },
            { "NewStatus", ShipmentStatus.Validated.ToString() }
        };
        var validatedResult = await _contract.ExecuteAsync(new ContractExecutionContext(
            new Transaction(TransactionType.StatusUpdated, "coordinator-key",
                JsonSerializer.Serialize(validatedPayload))));
        _contract.UpdateState(validatedResult.StateChanges);

        // InTransit
        var inTransitPayload = new Dictionary<string, object>
        {
            { "ShipmentId", shipmentId },
            { "NewStatus", ShipmentStatus.InTransit.ToString() }
        };
        var inTransitResult = await _contract.ExecuteAsync(new ContractExecutionContext(
            new Transaction(TransactionType.StatusUpdated, "coordinator-key",
                JsonSerializer.Serialize(inTransitPayload))));
        _contract.UpdateState(inTransitResult.StateChanges);

        // Delivered
        var deliveredPayload = new Dictionary<string, object>
        {
            { "ShipmentId", shipmentId },
            { "NewStatus", ShipmentStatus.Delivered.ToString() }
        };
        var deliveredTransaction = new Transaction(TransactionType.StatusUpdated, "coordinator-key",
            JsonSerializer.Serialize(deliveredPayload));

        // Act
        var result = await _contract.ExecuteAsync(new ContractExecutionContext(deliveredTransaction));

        // Assert
        result.Success.Should().BeTrue();
        result.Events.Should().Contain(e => e.Name == "ShipmentReachedDestination");
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidPayload_ShouldFail()
    {
        // Arrange
        var transaction = new Transaction(TransactionType.ShipmentCreated, "coordinator-key", "invalid-json");
        var context = new ContractExecutionContext(transaction);

        // Act
        var result = await _contract.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingShipmentId_ShouldFail()
    {
        // Arrange
        var payload = new Dictionary<string, object>
        {
            { "Origin", "New York" },
            { "Destination", "London" }
        };
        var transaction = new Transaction(TransactionType.ShipmentCreated, "coordinator-key",
            JsonSerializer.Serialize(payload));
        var context = new ContractExecutionContext(transaction);

        // Act
        var result = await _contract.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Shipment ID not found");
    }

    [Fact]
    public async Task ExecuteAsync_CompleteLifecycle_ShouldSucceed()
    {
        // Arrange
        var shipmentId = "shipment-lifecycle-test";

        // Act & Assert - Created
        var createPayload = new Dictionary<string, object>
        {
            { "ShipmentId", shipmentId },
            { "Origin", "New York" },
            { "Destination", "London" },
            { "RecipientId", "recipient-key" }
        };
        var createResult = await _contract.ExecuteAsync(new ContractExecutionContext(
            new Transaction(TransactionType.ShipmentCreated, "coordinator-key",
                JsonSerializer.Serialize(createPayload))));
        createResult.Success.Should().BeTrue();
        _contract.UpdateState(createResult.StateChanges);

        // Validated
        var validatedPayload = new Dictionary<string, object>
        {
            { "ShipmentId", shipmentId },
            { "NewStatus", ShipmentStatus.Validated.ToString() }
        };
        var validatedResult = await _contract.ExecuteAsync(new ContractExecutionContext(
            new Transaction(TransactionType.StatusUpdated, "coordinator-key",
                JsonSerializer.Serialize(validatedPayload))));
        validatedResult.Success.Should().BeTrue();
        _contract.UpdateState(validatedResult.StateChanges);

        // InTransit
        var inTransitPayload = new Dictionary<string, object>
        {
            { "ShipmentId", shipmentId },
            { "NewStatus", ShipmentStatus.InTransit.ToString() }
        };
        var inTransitResult = await _contract.ExecuteAsync(new ContractExecutionContext(
            new Transaction(TransactionType.StatusUpdated, "coordinator-key",
                JsonSerializer.Serialize(inTransitPayload))));
        inTransitResult.Success.Should().BeTrue();
        _contract.UpdateState(inTransitResult.StateChanges);

        // Delivered
        var deliveredPayload = new Dictionary<string, object>
        {
            { "ShipmentId", shipmentId },
            { "NewStatus", ShipmentStatus.Delivered.ToString() }
        };
        var deliveredResult = await _contract.ExecuteAsync(new ContractExecutionContext(
            new Transaction(TransactionType.StatusUpdated, "coordinator-key",
                JsonSerializer.Serialize(deliveredPayload))));
        deliveredResult.Success.Should().BeTrue();
        _contract.UpdateState(deliveredResult.StateChanges);

        // Confirmed
        var confirmedPayload = new Dictionary<string, object>
        {
            { "ShipmentId", shipmentId },
            { "NewStatus", ShipmentStatus.Confirmed.ToString() }
        };
        var confirmedResult = await _contract.ExecuteAsync(new ContractExecutionContext(
            new Transaction(TransactionType.StatusUpdated, "coordinator-key",
                JsonSerializer.Serialize(confirmedPayload))));
        confirmedResult.Success.Should().BeTrue();
        _contract.UpdateState(confirmedResult.StateChanges);

        // Verify final state
        var state = _contract.GetState();
        state[$"shipment_{shipmentId}_status"].Should().Be(ShipmentStatus.Confirmed.ToString());
    }
}
