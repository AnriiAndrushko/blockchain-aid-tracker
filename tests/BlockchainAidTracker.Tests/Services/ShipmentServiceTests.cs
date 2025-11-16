using BlockchainAidTracker.Core.Interfaces;
using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.DataAccess.Repositories;
using BlockchainAidTracker.Services.DTOs.Shipment;
using BlockchainAidTracker.Services.Exceptions;
using BlockchainAidTracker.Services.Interfaces;
using BlockchainAidTracker.Services.Services;
using Moq;
using Xunit;

namespace BlockchainAidTracker.Tests.Services;

/// <summary>
/// Unit tests for ShipmentService
/// </summary>
public class ShipmentServiceTests
{
    private readonly Mock<IShipmentRepository> _shipmentRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IQrCodeService> _qrCodeServiceMock;
    private readonly BlockchainAidTracker.Blockchain.Blockchain _blockchain;
    private readonly Mock<IDigitalSignatureService> _digitalSignatureServiceMock;
    private readonly TransactionSigningContext _signingContext;
    private readonly ShipmentService _shipmentService;

    public ShipmentServiceTests()
    {
        _shipmentRepositoryMock = new Mock<IShipmentRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _qrCodeServiceMock = new Mock<IQrCodeService>();
        _signingContext = new TransactionSigningContext();

        // Create a real blockchain instance for testing (can't mock non-virtual methods)
        var hashServiceMock = new Mock<IHashService>();
        hashServiceMock.Setup(x => x.ComputeSha256Hash(It.IsAny<string>())).Returns("mockedHash");
        _digitalSignatureServiceMock = new Mock<IDigitalSignatureService>();
        _blockchain = new BlockchainAidTracker.Blockchain.Blockchain(hashServiceMock.Object, _digitalSignatureServiceMock.Object)
        {
            // Disable signature validation for unit tests (ShipmentService uses placeholder signatures)
            ValidateTransactionSignatures = false
        };

        _shipmentService = new ShipmentService(
            _shipmentRepositoryMock.Object,
            _userRepositoryMock.Object,
            _qrCodeServiceMock.Object,
            _blockchain,
            _digitalSignatureServiceMock.Object,
            _signingContext
        );
    }

    #region CreateShipmentAsync Tests

    [Fact]
    public async Task CreateShipmentAsync_ValidRequest_CreatesShipmentAndReturnsDto()
    {
        // Arrange
        var coordinatorId = "coord123";
        var recipientId = "recipient123";

        var coordinator = new User
        {
            Id = coordinatorId,
            Role = UserRole.Coordinator,
            PublicKey = "coordinatorPublicKey"
        };

        var recipient = new User
        {
            Id = recipientId,
            Role = UserRole.Recipient
        };

        var request = new CreateShipmentRequest
        {
            Origin = "New York",
            Destination = "Los Angeles",
            RecipientId = recipientId,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(7),
            Items = new List<ShipmentItemDto>
            {
                new ShipmentItemDto { Description = "Medical Supplies", Quantity = 100, Unit = "boxes" }
            }
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(coordinatorId, default))
            .ReturnsAsync(coordinator);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(recipientId, default))
            .ReturnsAsync(recipient);
        _qrCodeServiceMock.Setup(x => x.GenerateQrCode(It.IsAny<string>()))
            .Returns("base64QrCode");
        _shipmentRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Shipment>(), default))
            .ReturnsAsync((Shipment s, CancellationToken ct) => s);

        // Act
        var result = await _shipmentService.CreateShipmentAsync(request, coordinatorId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.Origin, result.Origin);
        Assert.Equal(request.Destination, result.Destination);
        Assert.Equal(recipientId, result.RecipientId);
        Assert.Equal("base64QrCode", result.QrCode);
        Assert.Single(result.Items);
        _shipmentRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Shipment>(), default), Times.Once);
        // Verify transaction was added to blockchain
        Assert.Single(result.TransactionIds);
    }

    [Fact]
    public async Task CreateShipmentAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _shipmentService.CreateShipmentAsync(null!, "coord123"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateShipmentAsync_InvalidCoordinatorId_ThrowsArgumentException(string? coordinatorId)
    {
        // Arrange
        var request = new CreateShipmentRequest
        {
            Origin = "New York",
            Destination = "LA",
            RecipientId = "recipient123",
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(7)
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _shipmentService.CreateShipmentAsync(request, coordinatorId!));
    }

    [Fact]
    public async Task CreateShipmentAsync_CoordinatorNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var request = new CreateShipmentRequest
        {
            Origin = "New York",
            Destination = "LA",
            RecipientId = "recipient123",
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(7)
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync("nonexistent", default))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _shipmentService.CreateShipmentAsync(request, "nonexistent"));
    }

    [Fact]
    public async Task CreateShipmentAsync_UserNotCoordinatorOrAdmin_ThrowsUnauthorizedException()
    {
        // Arrange
        var userId = "user123";
        var user = new User
        {
            Id = userId,
            Role = UserRole.Recipient // Not a coordinator or admin
        };

        var request = new CreateShipmentRequest
        {
            Origin = "New York",
            Destination = "LA",
            RecipientId = "recipient123",
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(7)
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _shipmentService.CreateShipmentAsync(request, userId));
    }

    [Fact]
    public async Task CreateShipmentAsync_RecipientNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var coordinator = new User
        {
            Id = "coord123",
            Role = UserRole.Coordinator,
            PublicKey = "publicKey"
        };

        var request = new CreateShipmentRequest
        {
            Origin = "New York",
            Destination = "LA",
            RecipientId = "nonexistent",
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(7)
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync("coord123", default))
            .ReturnsAsync(coordinator);
        _userRepositoryMock.Setup(x => x.GetByIdAsync("nonexistent", default))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _shipmentService.CreateShipmentAsync(request, "coord123"));
    }

    #endregion

    #region GetShipmentByIdAsync Tests

    [Fact]
    public async Task GetShipmentByIdAsync_ShipmentExists_ReturnsDto()
    {
        // Arrange
        var shipmentId = "shipment123";
        var shipment = new Shipment
        {
            Id = shipmentId,
            Origin = "New York",
            Destination = "LA",
            AssignedRecipient = "recipient123",
            Status = ShipmentStatus.Created,
            QrCodeData = "qrCode",
            CreatedTimestamp = DateTime.UtcNow,
            UpdatedTimestamp = DateTime.UtcNow,
            ExpectedDeliveryTimeframe = "Expected by 2025-12-31",
            Items = new List<ShipmentItem>
            {
                new ShipmentItem { Description = "Item", Quantity = 10, Unit = "boxes" }
            }
        };

        _shipmentRepositoryMock.Setup(x => x.GetByIdWithItemsAsync(shipmentId, default))
            .ReturnsAsync(shipment);
        _shipmentRepositoryMock.Setup(x => x.GetByIdAsync(shipmentId, default))
            .ReturnsAsync(shipment);

        // Act
        var result = await _shipmentService.GetShipmentByIdAsync(shipmentId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(shipmentId, result.Id);
        Assert.Equal(shipment.Origin, result.Origin);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetShipmentByIdAsync_ShipmentNotFound_ReturnsNull()
    {
        // Arrange
        _shipmentRepositoryMock.Setup(x => x.GetByIdWithItemsAsync("nonexistent", default))
            .ReturnsAsync((Shipment?)null);

        // Act
        var result = await _shipmentService.GetShipmentByIdAsync("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetShipmentByIdAsync_InvalidShipmentId_ThrowsArgumentException(string? shipmentId)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _shipmentService.GetShipmentByIdAsync(shipmentId!));
    }

    #endregion

    #region GetShipmentsAsync Tests

    [Fact]
    public async Task GetShipmentsAsync_NoFilters_ReturnsAllShipments()
    {
        // Arrange
        var shipments = new List<Shipment>
        {
            new Shipment { Id = "1", Origin = "NY", Destination = "LA", CreatedTimestamp = DateTime.UtcNow, UpdatedTimestamp = DateTime.UtcNow, ExpectedDeliveryTimeframe = "test" },
            new Shipment { Id = "2", Origin = "SF", Destination = "Seattle", CreatedTimestamp = DateTime.UtcNow, UpdatedTimestamp = DateTime.UtcNow, ExpectedDeliveryTimeframe = "test" }
        };

        _shipmentRepositoryMock.Setup(x => x.GetAllWithItemsAsync(default))
            .ReturnsAsync(shipments);

        // Act
        var result = await _shipmentService.GetShipmentsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetShipmentsAsync_WithStatusFilter_ReturnsFilteredShipments()
    {
        // Arrange
        var inTransitShipments = new List<Shipment>
        {
            new Shipment { Id = "1", Status = ShipmentStatus.InTransit, CreatedTimestamp = DateTime.UtcNow, UpdatedTimestamp = DateTime.UtcNow, ExpectedDeliveryTimeframe = "test" }
        };

        _shipmentRepositoryMock.Setup(x => x.GetByStatusAsync(ShipmentStatus.InTransit, default))
            .ReturnsAsync(inTransitShipments);

        // Act
        var result = await _shipmentService.GetShipmentsAsync(ShipmentStatus.InTransit);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(ShipmentStatus.InTransit, result[0].Status);
    }

    [Fact]
    public async Task GetShipmentsAsync_WithRecipientFilter_ReturnsFilteredShipments()
    {
        // Arrange
        var recipientId = "recipient123";
        var recipientShipments = new List<Shipment>
        {
            new Shipment { Id = "1", AssignedRecipient = recipientId, CreatedTimestamp = DateTime.UtcNow, UpdatedTimestamp = DateTime.UtcNow, ExpectedDeliveryTimeframe = "test" }
        };

        _shipmentRepositoryMock.Setup(x => x.GetByRecipientAsync(recipientId, default))
            .ReturnsAsync(recipientShipments);

        // Act
        var result = await _shipmentService.GetShipmentsAsync(null, recipientId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(recipientId, result[0].RecipientId);
    }

    #endregion

    #region UpdateShipmentStatusAsync Tests

    [Fact]
    public async Task UpdateShipmentStatusAsync_ValidTransition_UpdatesStatusAndReturnsDto()
    {
        // Arrange
        var shipmentId = "shipment123";
        var userId = "user123";

        var shipment = new Shipment
        {
            Id = shipmentId,
            Status = ShipmentStatus.Created,
            CreatedTimestamp = DateTime.UtcNow,
            UpdatedTimestamp = DateTime.UtcNow,
            ExpectedDeliveryTimeframe = "test"
        };

        var user = new User
        {
            Id = userId,
            PublicKey = "userPublicKey"
        };

        _shipmentRepositoryMock.Setup(x => x.GetByIdWithItemsAsync(shipmentId, default))
            .ReturnsAsync(shipment);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        // Act
        var result = await _shipmentService.UpdateShipmentStatusAsync(shipmentId, ShipmentStatus.Validated, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ShipmentStatus.Validated, result.Status);
        _shipmentRepositoryMock.Verify(x => x.Update(It.IsAny<Shipment>()), Times.Once);
    }

    [Fact]
    public async Task UpdateShipmentStatusAsync_InvalidTransition_ThrowsBusinessException()
    {
        // Arrange
        var shipmentId = "shipment123";
        var shipment = new Shipment
        {
            Id = shipmentId,
            Status = ShipmentStatus.Created,
            CreatedTimestamp = DateTime.UtcNow,
            UpdatedTimestamp = DateTime.UtcNow
        };

        var user = new User { Id = "user123", PublicKey = "key" };

        _shipmentRepositoryMock.Setup(x => x.GetByIdWithItemsAsync(shipmentId, default))
            .ReturnsAsync(shipment);
        _userRepositoryMock.Setup(x => x.GetByIdAsync("user123", default))
            .ReturnsAsync(user);

        // Act & Assert - Cannot go from Created directly to Delivered
        await Assert.ThrowsAsync<BusinessException>(() =>
            _shipmentService.UpdateShipmentStatusAsync(shipmentId, ShipmentStatus.Delivered, "user123"));
    }

    [Fact]
    public async Task UpdateShipmentStatusAsync_ShipmentNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _shipmentRepositoryMock.Setup(x => x.GetByIdWithItemsAsync("nonexistent", default))
            .ReturnsAsync((Shipment?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _shipmentService.UpdateShipmentStatusAsync("nonexistent", ShipmentStatus.Validated, "user123"));
    }

    [Fact]
    public async Task UpdateShipmentStatusAsync_LogisticsPartnerUpdatesToInTransit_Success()
    {
        // Arrange
        var shipmentId = "shipment123";
        var userId = "logistics123";

        var shipment = new Shipment
        {
            Id = shipmentId,
            Status = ShipmentStatus.Validated
        };

        var user = new User
        {
            Id = userId,
            Role = UserRole.LogisticsPartner,
            PublicKey = "logisticsPublicKey"
        };

        _shipmentRepositoryMock.Setup(x => x.GetByIdWithItemsAsync(shipmentId, default))
            .ReturnsAsync(shipment);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        // Act
        var result = await _shipmentService.UpdateShipmentStatusAsync(shipmentId, ShipmentStatus.InTransit, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ShipmentStatus.InTransit, result.Status);
        _shipmentRepositoryMock.Verify(x => x.Update(It.IsAny<Shipment>()), Times.Once);
    }

    [Fact]
    public async Task UpdateShipmentStatusAsync_LogisticsPartnerUpdatesToDelivered_Success()
    {
        // Arrange
        var shipmentId = "shipment123";
        var userId = "logistics123";

        var shipment = new Shipment
        {
            Id = shipmentId,
            Status = ShipmentStatus.InTransit
        };

        var user = new User
        {
            Id = userId,
            Role = UserRole.LogisticsPartner,
            PublicKey = "logisticsPublicKey"
        };

        _shipmentRepositoryMock.Setup(x => x.GetByIdWithItemsAsync(shipmentId, default))
            .ReturnsAsync(shipment);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        // Act
        var result = await _shipmentService.UpdateShipmentStatusAsync(shipmentId, ShipmentStatus.Delivered, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ShipmentStatus.Delivered, result.Status);
        _shipmentRepositoryMock.Verify(x => x.Update(It.IsAny<Shipment>()), Times.Once);
    }

    [Fact]
    public async Task UpdateShipmentStatusAsync_LogisticsPartnerUpdatesToValidated_ThrowsUnauthorizedException()
    {
        // Arrange
        var shipmentId = "shipment123";
        var userId = "logistics123";

        var shipment = new Shipment
        {
            Id = shipmentId,
            Status = ShipmentStatus.Created
        };

        var user = new User
        {
            Id = userId,
            Role = UserRole.LogisticsPartner,
            PublicKey = "logisticsPublicKey"
        };

        _shipmentRepositoryMock.Setup(x => x.GetByIdWithItemsAsync(shipmentId, default))
            .ReturnsAsync(shipment);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        // Act & Assert - LogisticsPartner cannot update to Validated
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _shipmentService.UpdateShipmentStatusAsync(shipmentId, ShipmentStatus.Validated, userId));
    }

    [Fact]
    public async Task UpdateShipmentStatusAsync_LogisticsPartnerUpdatesToConfirmed_ThrowsUnauthorizedException()
    {
        // Arrange
        var shipmentId = "shipment123";
        var userId = "logistics123";

        var shipment = new Shipment
        {
            Id = shipmentId,
            Status = ShipmentStatus.Delivered
        };

        var user = new User
        {
            Id = userId,
            Role = UserRole.LogisticsPartner,
            PublicKey = "logisticsPublicKey"
        };

        _shipmentRepositoryMock.Setup(x => x.GetByIdWithItemsAsync(shipmentId, default))
            .ReturnsAsync(shipment);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        // Act & Assert - LogisticsPartner cannot update to Confirmed
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _shipmentService.UpdateShipmentStatusAsync(shipmentId, ShipmentStatus.Confirmed, userId));
    }

    [Fact]
    public async Task UpdateShipmentStatusAsync_DonorRole_ThrowsUnauthorizedException()
    {
        // Arrange
        var shipmentId = "shipment123";
        var userId = "donor123";

        var shipment = new Shipment
        {
            Id = shipmentId,
            Status = ShipmentStatus.Created
        };

        var user = new User
        {
            Id = userId,
            Role = UserRole.Donor,
            PublicKey = "donorPublicKey"
        };

        _shipmentRepositoryMock.Setup(x => x.GetByIdWithItemsAsync(shipmentId, default))
            .ReturnsAsync(shipment);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        // Act & Assert - Donor cannot update status
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _shipmentService.UpdateShipmentStatusAsync(shipmentId, ShipmentStatus.Validated, userId));
    }

    [Fact]
    public async Task UpdateShipmentStatusAsync_ValidatorRole_ThrowsUnauthorizedException()
    {
        // Arrange
        var shipmentId = "shipment123";
        var userId = "validator123";

        var shipment = new Shipment
        {
            Id = shipmentId,
            Status = ShipmentStatus.Created
        };

        var user = new User
        {
            Id = userId,
            Role = UserRole.Validator,
            PublicKey = "validatorPublicKey"
        };

        _shipmentRepositoryMock.Setup(x => x.GetByIdWithItemsAsync(shipmentId, default))
            .ReturnsAsync(shipment);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        // Act & Assert - Validator cannot update status
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _shipmentService.UpdateShipmentStatusAsync(shipmentId, ShipmentStatus.Validated, userId));
    }

    [Fact]
    public async Task UpdateShipmentStatusAsync_RecipientRole_ThrowsUnauthorizedException()
    {
        // Arrange
        var shipmentId = "shipment123";
        var userId = "recipient123";

        var shipment = new Shipment
        {
            Id = shipmentId,
            Status = ShipmentStatus.Created
        };

        var user = new User
        {
            Id = userId,
            Role = UserRole.Recipient,
            PublicKey = "recipientPublicKey"
        };

        _shipmentRepositoryMock.Setup(x => x.GetByIdWithItemsAsync(shipmentId, default))
            .ReturnsAsync(shipment);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        // Act & Assert - Recipient should use ConfirmDelivery endpoint
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _shipmentService.UpdateShipmentStatusAsync(shipmentId, ShipmentStatus.Delivered, userId));
    }

    [Fact]
    public async Task UpdateShipmentStatusAsync_AdministratorRole_CanUpdateToAnyStatus()
    {
        // Arrange
        var shipmentId = "shipment123";
        var userId = "admin123";

        var shipment = new Shipment
        {
            Id = shipmentId,
            Status = ShipmentStatus.Created
        };

        var user = new User
        {
            Id = userId,
            Role = UserRole.Administrator,
            PublicKey = "adminPublicKey"
        };

        _shipmentRepositoryMock.Setup(x => x.GetByIdWithItemsAsync(shipmentId, default))
            .ReturnsAsync(shipment);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        // Act
        var result = await _shipmentService.UpdateShipmentStatusAsync(shipmentId, ShipmentStatus.Validated, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ShipmentStatus.Validated, result.Status);
        _shipmentRepositoryMock.Verify(x => x.Update(It.IsAny<Shipment>()), Times.Once);
    }

    [Fact]
    public async Task UpdateShipmentStatusAsync_CoordinatorRole_CanUpdateToAnyStatus()
    {
        // Arrange
        var shipmentId = "shipment123";
        var userId = "coordinator123";

        var shipment = new Shipment
        {
            Id = shipmentId,
            Status = ShipmentStatus.Validated
        };

        var user = new User
        {
            Id = userId,
            Role = UserRole.Coordinator,
            PublicKey = "coordinatorPublicKey"
        };

        _shipmentRepositoryMock.Setup(x => x.GetByIdWithItemsAsync(shipmentId, default))
            .ReturnsAsync(shipment);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        // Act
        var result = await _shipmentService.UpdateShipmentStatusAsync(shipmentId, ShipmentStatus.InTransit, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ShipmentStatus.InTransit, result.Status);
        _shipmentRepositoryMock.Verify(x => x.Update(It.IsAny<Shipment>()), Times.Once);
    }

    #endregion

    #region ConfirmDeliveryAsync Tests

    [Fact]
    public async Task ConfirmDeliveryAsync_ValidRequest_ConfirmsDeliveryAndReturnsDto()
    {
        // Arrange
        var shipmentId = "shipment123";
        var recipientId = "recipient123";

        var shipment = new Shipment
        {
            Id = shipmentId,
            AssignedRecipient = recipientId,
            Status = ShipmentStatus.Delivered, // Must be delivered before confirming
            CreatedTimestamp = DateTime.UtcNow,
            UpdatedTimestamp = DateTime.UtcNow,
            ExpectedDeliveryTimeframe = "test"
        };

        var recipient = new User
        {
            Id = recipientId,
            PublicKey = "recipientPublicKey"
        };

        _shipmentRepositoryMock.Setup(x => x.GetByIdWithItemsAsync(shipmentId, default))
            .ReturnsAsync(shipment);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(recipientId, default))
            .ReturnsAsync(recipient);

        // Act
        var result = await _shipmentService.ConfirmDeliveryAsync(shipmentId, recipientId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ShipmentStatus.Confirmed, result.Status);
        Assert.NotNull(result.ActualDeliveryDate);
        _shipmentRepositoryMock.Verify(x => x.Update(It.IsAny<Shipment>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmDeliveryAsync_WrongRecipient_ThrowsUnauthorizedException()
    {
        // Arrange
        var shipmentId = "shipment123";
        var shipment = new Shipment
        {
            Id = shipmentId,
            AssignedRecipient = "recipient123",
            Status = ShipmentStatus.Delivered,
            CreatedTimestamp = DateTime.UtcNow,
            UpdatedTimestamp = DateTime.UtcNow
        };

        _shipmentRepositoryMock.Setup(x => x.GetByIdWithItemsAsync(shipmentId, default))
            .ReturnsAsync(shipment);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _shipmentService.ConfirmDeliveryAsync(shipmentId, "wrongRecipient"));
    }

    [Fact]
    public async Task ConfirmDeliveryAsync_InvalidStatus_ThrowsBusinessException()
    {
        // Arrange
        var shipmentId = "shipment123";
        var recipientId = "recipient123";

        var shipment = new Shipment
        {
            Id = shipmentId,
            AssignedRecipient = recipientId,
            Status = ShipmentStatus.Created, // Wrong status for confirmation
            CreatedTimestamp = DateTime.UtcNow,
            UpdatedTimestamp = DateTime.UtcNow
        };

        var recipient = new User
        {
            Id = recipientId,
            PublicKey = "key"
        };

        _shipmentRepositoryMock.Setup(x => x.GetByIdWithItemsAsync(shipmentId, default))
            .ReturnsAsync(shipment);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(recipientId, default))
            .ReturnsAsync(recipient);

        // Act & Assert
        await Assert.ThrowsAsync<BusinessException>(() =>
            _shipmentService.ConfirmDeliveryAsync(shipmentId, recipientId));
    }

    #endregion

    #region GetShipmentBlockchainHistoryAsync and VerifyShipmentOnBlockchainAsync Tests

    [Fact]
    public async Task GetShipmentBlockchainHistoryAsync_ShipmentWithNoTransactions_ReturnsEmptyList()
    {
        // Arrange
        var shipmentId = "shipment123";
        var shipment = new Shipment { Id = shipmentId };
        _shipmentRepositoryMock.Setup(x => x.GetByIdAsync(shipmentId, default))
            .ReturnsAsync(shipment);

        // Act
        var result = await _shipmentService.GetShipmentBlockchainHistoryAsync(shipmentId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result); // No transactions added yet for this shipment
    }

    [Fact]
    public async Task VerifyShipmentOnBlockchainAsync_ShipmentHasNoTransactions_ReturnsFalse()
    {
        // Arrange
        var shipmentId = "nonexistent";
        var shipment = new Shipment { Id = shipmentId };
        _shipmentRepositoryMock.Setup(x => x.GetByIdAsync(shipmentId, default))
            .ReturnsAsync(shipment);

        // Act
        var result = await _shipmentService.VerifyShipmentOnBlockchainAsync(shipmentId);

        // Assert
        Assert.False(result);
    }

    #endregion
}
