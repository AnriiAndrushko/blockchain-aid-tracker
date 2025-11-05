using System.Text.Json;
using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.SmartContracts.Contracts;
using BlockchainAidTracker.SmartContracts.Models;
using FluentAssertions;

namespace BlockchainAidTracker.Tests.SmartContracts;

public class DeliveryVerificationContractTests
{
    private readonly DeliveryVerificationContract _contract;

    public DeliveryVerificationContractTests()
    {
        _contract = new DeliveryVerificationContract();
    }

    [Fact]
    public void Contract_ShouldHaveCorrectMetadata()
    {
        // Assert
        _contract.ContractId.Should().Be("delivery-verification-v1");
        _contract.Name.Should().Be("Delivery Verification Contract");
        _contract.Description.Should().NotBeNullOrEmpty();
        _contract.Version.Should().Be("1.0.0");
    }

    [Fact]
    public void CanExecute_WithDeliveryConfirmedTransaction_ShouldReturnTrue()
    {
        // Arrange
        var transaction = new Transaction(TransactionType.DeliveryConfirmed, "recipient-key", "{}");
        var context = new ContractExecutionContext(transaction);

        // Act
        var result = _contract.CanExecute(context);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(TransactionType.ShipmentCreated)]
    [InlineData(TransactionType.StatusUpdated)]
    public void CanExecute_WithOtherTransactionTypes_ShouldReturnFalse(TransactionType type)
    {
        // Arrange
        var transaction = new Transaction(type, "sender-key", "{}");
        var context = new ContractExecutionContext(transaction);

        // Act
        var result = _contract.CanExecute(context);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_WithValidDeliveryConfirmation_ShouldSucceed()
    {
        // Arrange
        var recipientKey = "recipient-public-key";
        var shipmentId = "shipment-123";
        var payload = new Dictionary<string, object>
        {
            { "shipmentId", shipmentId },
            { "assignedRecipient", recipientKey },
            { "qrCodeData", "QR-123" },
            { "expectedDeliveryTimeframe", "2025-01-01 to 2025-01-10" }
        };

        var transaction = new Transaction(TransactionType.DeliveryConfirmed, recipientKey,
            JsonSerializer.Serialize(payload));
        var context = new ContractExecutionContext(transaction);

        // Act
        var result = await _contract.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNullOrEmpty();
        result.Output.Should().ContainKey("verified");
        result.Output["verified"].Should().Be(true);
        result.Output.Should().ContainKey("shipmentId");
        result.Output["shipmentId"].Should().Be(shipmentId);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidPayload_ShouldFail()
    {
        // Arrange
        var transaction = new Transaction(TransactionType.DeliveryConfirmed, "recipient-key", "invalid-json");
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
            { "assignedRecipient", "recipient-key" }
        };

        var transaction = new Transaction(TransactionType.DeliveryConfirmed, "recipient-key",
            JsonSerializer.Serialize(payload));
        var context = new ContractExecutionContext(transaction);

        // Act
        var result = await _contract.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Shipment ID not found");
    }

    [Fact]
    public async Task ExecuteAsync_WithWrongRecipient_ShouldFail()
    {
        // Arrange
        var assignedRecipient = "correct-recipient-key";
        var wrongRecipient = "wrong-recipient-key";
        var payload = new Dictionary<string, object>
        {
            { "shipmentId", "shipment-123" },
            { "assignedRecipient", assignedRecipient }
        };

        var transaction = new Transaction(TransactionType.DeliveryConfirmed, wrongRecipient,
            JsonSerializer.Serialize(payload));
        var context = new ContractExecutionContext(transaction);

        // Act
        var result = await _contract.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("assigned recipient");
        result.Events.Should().Contain(e => e.Name == "DeliveryVerificationFailed");
    }

    [Fact]
    public async Task ExecuteAsync_WithMatchingQRCode_ShouldVerifySuccessfully()
    {
        // Arrange
        var recipientKey = "recipient-key";
        var qrCode = "SHIPMENT-123-QR";
        var payload = new Dictionary<string, object>
        {
            { "shipmentId", "shipment-123" },
            { "assignedRecipient", recipientKey },
            { "qrCodeData", qrCode },
            { "expectedDeliveryTimeframe", "2025-01-01 to 2025-01-10" }
        };

        var transaction = new Transaction(TransactionType.DeliveryConfirmed, recipientKey,
            JsonSerializer.Serialize(payload));
        var contextData = new Dictionary<string, object> { { "qrCodeData", qrCode } };
        var context = new ContractExecutionContext(transaction, null!, contextData);

        // Act
        var result = await _contract.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().ContainKey("qrCodeVerified");
        result.Output["qrCodeVerified"].Should().Be(true);
    }

    [Fact]
    public async Task ExecuteAsync_WithMismatchedQRCode_ShouldFail()
    {
        // Arrange
        var recipientKey = "recipient-key";
        var expectedQrCode = "SHIPMENT-123-QR";
        var providedQrCode = "WRONG-QR";
        var payload = new Dictionary<string, object>
        {
            { "shipmentId", "shipment-123" },
            { "assignedRecipient", recipientKey },
            { "qrCodeData", expectedQrCode }
        };

        var transaction = new Transaction(TransactionType.DeliveryConfirmed, recipientKey,
            JsonSerializer.Serialize(payload));
        var contextData = new Dictionary<string, object> { { "qrCodeData", providedQrCode } };
        var context = new ContractExecutionContext(transaction, null!, contextData);

        // Act
        var result = await _contract.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("QR code verification failed");
        result.Events.Should().Contain(e => e.Name == "QRCodeVerificationFailed");
    }

    [Fact]
    public async Task ExecuteAsync_WithOnTimeDelivery_ShouldMarkAsOnTime()
    {
        // Arrange
        var recipientKey = "recipient-key";
        var endDate = DateTime.UtcNow.AddDays(5).ToString("yyyy-MM-dd");
        var payload = new Dictionary<string, object>
        {
            { "shipmentId", "shipment-123" },
            { "assignedRecipient", recipientKey },
            { "expectedDeliveryTimeframe", $"2025-01-01 to {endDate}" }
        };

        var transaction = new Transaction(TransactionType.DeliveryConfirmed, recipientKey,
            JsonSerializer.Serialize(payload));
        var context = new ContractExecutionContext(transaction);

        // Act
        var result = await _contract.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().ContainKey("onTime");
        result.Output["onTime"].Should().Be(true);
        result.Events.Should().NotContain(e => e.Name == "DeliveryDelayed");
    }

    [Fact]
    public async Task ExecuteAsync_WithLateDelivery_ShouldMarkAsDelayed()
    {
        // Arrange
        var recipientKey = "recipient-key";
        var endDate = DateTime.UtcNow.AddDays(-5).ToString("yyyy-MM-dd");
        var payload = new Dictionary<string, object>
        {
            { "shipmentId", "shipment-123" },
            { "assignedRecipient", recipientKey },
            { "expectedDeliveryTimeframe", $"2025-01-01 to {endDate}" }
        };

        var transaction = new Transaction(TransactionType.DeliveryConfirmed, recipientKey,
            JsonSerializer.Serialize(payload));
        var context = new ContractExecutionContext(transaction);

        // Act
        var result = await _contract.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().ContainKey("onTime");
        result.Output["onTime"].Should().Be(false);
        result.Events.Should().Contain(e => e.Name == "DeliveryDelayed");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUpdateState()
    {
        // Arrange
        var recipientKey = "recipient-key";
        var shipmentId = "shipment-123";
        var payload = new Dictionary<string, object>
        {
            { "shipmentId", shipmentId },
            { "assignedRecipient", recipientKey },
            { "expectedDeliveryTimeframe", "2025-01-01 to 2025-01-10" }
        };

        var transaction = new Transaction(TransactionType.DeliveryConfirmed, recipientKey,
            JsonSerializer.Serialize(payload));
        var context = new ContractExecutionContext(transaction);

        // Act
        var result = await _contract.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.StateChanges.Should().ContainKey($"delivery_{shipmentId}_verified");
        result.StateChanges.Should().ContainKey($"delivery_{shipmentId}_timestamp");
        result.StateChanges.Should().ContainKey($"delivery_{shipmentId}_onTime");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldEmitDeliveryVerifiedEvent()
    {
        // Arrange
        var recipientKey = "recipient-key";
        var shipmentId = "shipment-123";
        var payload = new Dictionary<string, object>
        {
            { "shipmentId", shipmentId },
            { "assignedRecipient", recipientKey },
            { "expectedDeliveryTimeframe", "2025-01-01 to 2025-01-10" }
        };

        var transaction = new Transaction(TransactionType.DeliveryConfirmed, recipientKey,
            JsonSerializer.Serialize(payload));
        var context = new ContractExecutionContext(transaction);

        // Act
        var result = await _contract.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.Events.Should().Contain(e => e.Name == "DeliveryVerified");
        var verifiedEvent = result.Events.First(e => e.Name == "DeliveryVerified");
        verifiedEvent.Data.Should().ContainKey("shipmentId");
        verifiedEvent.Data.Should().ContainKey("recipient");
        verifiedEvent.Data.Should().ContainKey("deliveryTimestamp");
    }
}
