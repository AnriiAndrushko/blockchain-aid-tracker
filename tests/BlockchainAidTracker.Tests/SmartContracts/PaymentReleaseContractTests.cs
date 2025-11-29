using System.Text.Json;
using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.SmartContracts.Contracts;
using BlockchainAidTracker.SmartContracts.Models;
using FluentAssertions;

namespace BlockchainAidTracker.Tests.SmartContracts;

public class PaymentReleaseContractTests
{
    [Fact]
    public void Contract_ShouldHaveCorrectMetadata()
    {
        // Arrange
        var contract = new PaymentReleaseContract();

        // Assert
        contract.ContractId.Should().Be("payment-release-v1");
        contract.Name.Should().Be("Payment Release Contract");
        contract.Description.Should().NotBeNullOrEmpty();
        contract.Version.Should().Be("1.0.0");
    }

    [Fact]
    public void CanExecute_WithStatusUpdatedTransaction_ShouldReturnTrue()
    {
        // Arrange
        var contract = new PaymentReleaseContract();
        var transaction = new Transaction(TransactionType.StatusUpdated, "coordinator-key", "{}");
        var context = new ContractExecutionContext(transaction);

        // Act
        var result = contract.CanExecute(context);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanExecute_WithPaymentInitiatedTransaction_ShouldReturnTrue()
    {
        // Arrange
        var contract = new PaymentReleaseContract();
        var transaction = new Transaction(TransactionType.PaymentInitiated, "admin-key", "{}");
        var context = new ContractExecutionContext(transaction);

        // Act
        var result = contract.CanExecute(context);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(TransactionType.ShipmentCreated)]
    [InlineData(TransactionType.DeliveryConfirmed)]
    [InlineData(TransactionType.SupplierRegistered)]
    public void CanExecute_WithOtherTransactionTypes_ShouldReturnFalse(TransactionType type)
    {
        // Arrange
        var contract = new PaymentReleaseContract();
        var transaction = new Transaction(type, "sender-key", "{}");
        var context = new ContractExecutionContext(transaction);

        // Act
        var result = contract.CanExecute(context);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidPayload_ShouldFail()
    {
        // Arrange
        var contract = new PaymentReleaseContract();
        var transaction = new Transaction(TransactionType.StatusUpdated, "sender-key", "invalid-json");
        var context = new ContractExecutionContext(transaction);

        // Act
        var result = await contract.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingShipmentId_ShouldFail()
    {
        // Arrange
        var contract = new PaymentReleaseContract();
        var payload = new Dictionary<string, object>
        {
            { "NewStatus", ShipmentStatus.Confirmed.ToString() }
        };

        var transaction = new Transaction(TransactionType.StatusUpdated, "coordinator-key",
            JsonSerializer.Serialize(payload));
        var context = new ContractExecutionContext(transaction);

        // Act
        var result = await contract.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Shipment ID");
    }

    [Fact]
    public async Task ExecuteAsync_WithNonConfirmedStatus_ShouldSkipPaymentRelease()
    {
        // Arrange
        var contract = new PaymentReleaseContract();
        var shipmentId = "shipment-123";
        var payload = new Dictionary<string, object>
        {
            { "ShipmentId", shipmentId },
            { "NewStatus", ShipmentStatus.InTransit.ToString() }
        };

        var transaction = new Transaction(TransactionType.StatusUpdated, "coordinator-key",
            JsonSerializer.Serialize(payload));
        var context = new ContractExecutionContext(transaction);

        // Act
        var result = await contract.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().ContainKey("processed");
        result.Output["processed"].Should().Be(false);
        result.Output["reason"]?.ToString().Should().Contain("not in Confirmed status");
    }

    [Fact]
    public async Task ExecuteAsync_WithConfirmedStatusAndNoSuppliers_ShouldSkip()
    {
        // Arrange
        var contract = new PaymentReleaseContract();
        var shipmentId = "shipment-123";
        var payload = new Dictionary<string, object>
        {
            { "ShipmentId", shipmentId },
            { "NewStatus", ShipmentStatus.Confirmed.ToString() },
            { "Suppliers", new List<object>() }
        };

        var transaction = new Transaction(TransactionType.StatusUpdated, "coordinator-key",
            JsonSerializer.Serialize(payload));
        var context = new ContractExecutionContext(transaction);

        // Act
        var result = await contract.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().ContainKey("processed");
        result.Output["processed"].Should().Be(false);
    }

    [Fact]
    public async Task ExecuteAsync_WithConfirmedStatusAndUnverifiedSupplier_ShouldSkip()
    {
        // Arrange
        var contract = new PaymentReleaseContract();
        var shipmentId = "shipment-123";
        var supplierId = "supplier-123";

        var payload = new
        {
            ShipmentId = shipmentId,
            NewStatus = ShipmentStatus.Confirmed.ToString(),
            Suppliers = new[]
            {
                new { SupplierId = supplierId, Amount = 1000, Currency = "USD" }
            }
        };

        var transaction = new Transaction(TransactionType.StatusUpdated, "coordinator-key",
            JsonSerializer.Serialize(payload));
        var context = new ContractExecutionContext(transaction);

        // Act
        var result = await contract.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().ContainKey("processed");
        result.Output["processed"].Should().Be(false);
        // Should have supplier not verified event
        result.Events.Any(e => e.Name == "SupplierNotVerified").Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithConfirmedStatusAndVerifiedSupplier_ShouldReleasePayment()
    {
        // Arrange
        var contract = new PaymentReleaseContract();
        var shipmentId = "shipment-123";
        var supplierId = "supplier-123";

        var payload = new
        {
            ShipmentId = shipmentId,
            NewStatus = ShipmentStatus.Confirmed.ToString(),
            Suppliers = new[]
            {
                new { SupplierId = supplierId, Amount = 1000.00, Currency = "USD" }
            }
        };

        // Setup supplier as verified in contract state
        contract.UpdateState(new Dictionary<string, object>
        {
            { $"supplier_{supplierId}_verification_status", "Verified" },
            { $"supplier_{supplierId}_payment_threshold", "500.00" }
        });

        var transaction = new Transaction(TransactionType.StatusUpdated, "coordinator-key",
            JsonSerializer.Serialize(payload));
        var context = new ContractExecutionContext(transaction);

        // Act
        var result = await contract.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().ContainKey("processed");
        result.Output["processed"].Should().Be(true);
        result.Output.Should().ContainKey("paymentStatus");
        result.Output["paymentStatus"].Should().Be("Released");
        result.Events.Any(e => e.Name == "PaymentInitiated").Should().BeTrue();
        result.Events.Any(e => e.Name == "PaymentReleased").Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_DirectPaymentInitiation_ShouldSucceed()
    {
        // Arrange
        var contract = new PaymentReleaseContract();
        var shipmentId = "shipment-123";
        var supplierId = "supplier-123";
        var payload = new Dictionary<string, object>
        {
            { "ShipmentId", shipmentId },
            { "SupplierId", supplierId },
            { "Amount", 1000.00 },  // Use decimal/double instead of string
            { "Currency", "USD" }
        };

        // Setup supplier as verified
        contract.UpdateState(new Dictionary<string, object>
        {
            { $"supplier_{supplierId}_verification_status", "Verified" }
        });

        var transaction = new Transaction(TransactionType.PaymentInitiated, "admin-key",
            JsonSerializer.Serialize(payload));
        var context = new ContractExecutionContext(transaction);

        // Act
        var result = await contract.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue($"Error: {result.ErrorMessage}");
        result.Output.Should().ContainKey("paymentId");
        result.Output.Should().ContainKey("status");
        result.Output["status"].Should().Be("Initiated");
        result.Events.Any(e => e.Name == "PaymentInitiated").Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_DirectPaymentInitiation_WithUnverifiedSupplier_ShouldFail()
    {
        // Arrange
        var contract = new PaymentReleaseContract();
        var shipmentId = "shipment-123";
        var supplierId = "supplier-123";
        var payload = new Dictionary<string, object>
        {
            { "ShipmentId", shipmentId },
            { "SupplierId", supplierId },
            { "Amount", 1000.00 },  // Use decimal/double
            { "Currency", "USD" }
        };

        // Supplier not set as verified

        var transaction = new Transaction(TransactionType.PaymentInitiated, "admin-key",
            JsonSerializer.Serialize(payload));
        var context = new ContractExecutionContext(transaction);

        // Act
        var result = await contract.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not verified");
    }

    [Fact]
    public async Task ExecuteAsync_DirectPaymentInitiation_WithMissingSuppliedId_ShouldFail()
    {
        // Arrange
        var contract = new PaymentReleaseContract();
        var shipmentId = "shipment-123";
        var payload = new Dictionary<string, object>
        {
            { "ShipmentId", shipmentId },
            { "Amount", "1000.00" },
            { "Currency", "USD" }
        };

        var transaction = new Transaction(TransactionType.PaymentInitiated, "admin-key",
            JsonSerializer.Serialize(payload));
        var context = new ContractExecutionContext(transaction);

        // Act
        var result = await contract.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Supplier ID");
    }

    [Fact]
    public async Task ExecuteAsync_DirectPaymentInitiation_WithInvalidAmount_ShouldFail()
    {
        // Arrange
        var contract = new PaymentReleaseContract();
        var shipmentId = "shipment-123";
        var supplierId = "supplier-123";
        var payload = new Dictionary<string, object>
        {
            { "ShipmentId", shipmentId },
            { "SupplierId", supplierId },
            { "Amount", -100 },  // Use negative number
            { "Currency", "USD" }
        };

        // Setup supplier as verified
        contract.UpdateState(new Dictionary<string, object>
        {
            { $"supplier_{supplierId}_verification_status", "Verified" }
        });

        var transaction = new Transaction(TransactionType.PaymentInitiated, "admin-key",
            JsonSerializer.Serialize(payload));
        var context = new ContractExecutionContext(transaction);

        // Act
        var result = await contract.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid payment amount");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldEmitPaymentReleasedEvent()
    {
        // Arrange
        var contract = new PaymentReleaseContract();
        var shipmentId = "shipment-123";
        var supplierId1 = "supplier-123";
        var supplierId2 = "supplier-456";

        var payload = new
        {
            ShipmentId = shipmentId,
            NewStatus = ShipmentStatus.Confirmed.ToString(),
            Suppliers = new[]
            {
                new { SupplierId = supplierId1, Amount = 1000.00, Currency = "USD" },
                new { SupplierId = supplierId2, Amount = 500.00, Currency = "EUR" }
            }
        };

        // Setup both suppliers as verified
        contract.UpdateState(new Dictionary<string, object>
        {
            { $"supplier_{supplierId1}_verification_status", "Verified" },
            { $"supplier_{supplierId1}_payment_threshold", "500.00" },
            { $"supplier_{supplierId2}_verification_status", "Verified" },
            { $"supplier_{supplierId2}_payment_threshold", "400.00" }
        });

        var transaction = new Transaction(TransactionType.StatusUpdated, "coordinator-key",
            JsonSerializer.Serialize(payload));
        var context = new ContractExecutionContext(transaction);

        // Act
        var result = await contract.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.Events.Any(e =>
            e.Name == "PaymentReleased" &&
            e.Data?.ContainsKey("suppliersCount") == true &&
            (int)e.Data["suppliersCount"] == 2 &&
            e.Data?.ContainsKey("shipmentId") == true &&
            e.Data["shipmentId"].ToString() == shipmentId
        ).Should().BeTrue();
    }
}
