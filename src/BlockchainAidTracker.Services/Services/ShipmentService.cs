using System.Text.Json;
using BlockchainAidTracker.Blockchain;
using BlockchainAidTracker.Core.Interfaces;
using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.DataAccess.Repositories;
using BlockchainAidTracker.Services.DTOs.Shipment;
using BlockchainAidTracker.Services.Exceptions;
using BlockchainAidTracker.Services.Interfaces;

namespace BlockchainAidTracker.Services.Services;

/// <summary>
/// Implementation of shipment service with blockchain integration
/// </summary>
public class ShipmentService : IShipmentService
{
    private readonly IShipmentRepository _shipmentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IQrCodeService _qrCodeService;
    private readonly Blockchain.Blockchain _blockchain;
    private readonly IDigitalSignatureService _digitalSignatureService;

    public ShipmentService(
        IShipmentRepository shipmentRepository,
        IUserRepository userRepository,
        IQrCodeService qrCodeService,
        Blockchain.Blockchain blockchain,
        IDigitalSignatureService digitalSignatureService)
    {
        _shipmentRepository = shipmentRepository ?? throw new ArgumentNullException(nameof(shipmentRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _qrCodeService = qrCodeService ?? throw new ArgumentNullException(nameof(qrCodeService));
        _blockchain = blockchain ?? throw new ArgumentNullException(nameof(blockchain));
        _digitalSignatureService = digitalSignatureService ?? throw new ArgumentNullException(nameof(digitalSignatureService));
    }

    public async Task<ShipmentDto> CreateShipmentAsync(CreateShipmentRequest request, string coordinatorId)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(coordinatorId))
        {
            throw new ArgumentException("Coordinator ID cannot be null or empty", nameof(coordinatorId));
        }

        // Validate coordinator exists and has appropriate role
        var coordinator = await _userRepository.GetByIdAsync(coordinatorId);
        if (coordinator == null)
        {
            throw new NotFoundException($"Coordinator with ID '{coordinatorId}' not found");
        }

        if (coordinator.Role != UserRole.Coordinator && coordinator.Role != UserRole.Administrator)
        {
            throw new UnauthorizedException("Only coordinators and administrators can create shipments");
        }

        // Validate recipient exists
        var recipient = await _userRepository.GetByIdAsync(request.RecipientId);
        if (recipient == null)
        {
            throw new NotFoundException($"Recipient with ID '{request.RecipientId}' not found");
        }

        // Create shipment entity
        var shipmentId = Guid.NewGuid().ToString();
        var qrCode = _qrCodeService.GenerateQrCode(shipmentId);

        // Format expected delivery as a timeframe string
        var expectedTimeframe = $"Expected by {request.ExpectedDeliveryDate:yyyy-MM-dd}";

        var shipment = new Shipment
        {
            Id = shipmentId,
            Origin = request.Origin,
            Destination = request.Destination,
            AssignedRecipient = request.RecipientId,
            ExpectedDeliveryTimeframe = expectedTimeframe,
            Status = ShipmentStatus.Created,
            QrCodeData = qrCode,
            CoordinatorPublicKey = coordinator.PublicKey,
            CreatedTimestamp = DateTime.UtcNow,
            UpdatedTimestamp = DateTime.UtcNow,
            Items = request.Items.Select(i => new ShipmentItem
            {
                Id = Guid.NewGuid().ToString(),
                Description = i.Description,
                Quantity = i.Quantity,
                Unit = i.Unit,
                Category = "General" // Default category
            }).ToList()
        };

        // Save to database (AddAsync saves automatically)
        await _shipmentRepository.AddAsync(shipment);

        // Create blockchain transaction
        var transactionId = await CreateBlockchainTransactionAsync(
            TransactionType.ShipmentCreated,
            coordinator.PublicKey,
            new
            {
                ShipmentId = shipmentId,
                Origin = request.Origin,
                Destination = request.Destination,
                RecipientId = request.RecipientId,
                Items = request.Items,
                CreatedBy = coordinatorId,
                CreatedAt = shipment.CreatedTimestamp
            });

        return MapToDto(shipment, new List<string> { transactionId });
    }

    public async Task<ShipmentDto?> GetShipmentByIdAsync(string shipmentId)
    {
        if (string.IsNullOrWhiteSpace(shipmentId))
        {
            throw new ArgumentException("Shipment ID cannot be null or empty", nameof(shipmentId));
        }

        var shipment = await _shipmentRepository.GetByIdWithItemsAsync(shipmentId);
        if (shipment == null)
        {
            return null;
        }

        var transactionIds = await GetShipmentBlockchainHistoryAsync(shipmentId);
        return MapToDto(shipment, transactionIds);
    }

    public async Task<List<ShipmentDto>> GetShipmentsAsync(ShipmentStatus? status = null, string? recipientId = null)
    {
        List<Shipment> shipments;

        if (status.HasValue && !string.IsNullOrWhiteSpace(recipientId))
        {
            // Get by recipient then filter by status
            var allRecipientShipments = await _shipmentRepository.GetByRecipientAsync(recipientId);
            shipments = allRecipientShipments.Where(s => s.Status == status.Value).ToList();
        }
        else if (status.HasValue)
        {
            shipments = await _shipmentRepository.GetByStatusAsync(status.Value);
        }
        else if (!string.IsNullOrWhiteSpace(recipientId))
        {
            shipments = await _shipmentRepository.GetByRecipientAsync(recipientId);
        }
        else
        {
            shipments = await _shipmentRepository.GetAllWithItemsAsync();
        }

        var shipmentDtos = new List<ShipmentDto>();
        foreach (var shipment in shipments)
        {
            var transactionIds = await GetShipmentBlockchainHistoryAsync(shipment.Id);
            shipmentDtos.Add(MapToDto(shipment, transactionIds));
        }

        return shipmentDtos;
    }

    public async Task<ShipmentDto> UpdateShipmentStatusAsync(string shipmentId, ShipmentStatus newStatus, string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(shipmentId))
        {
            throw new ArgumentException("Shipment ID cannot be null or empty", nameof(shipmentId));
        }

        if (string.IsNullOrWhiteSpace(updatedBy))
        {
            throw new ArgumentException("Updated by user ID cannot be null or empty", nameof(updatedBy));
        }

        var shipment = await _shipmentRepository.GetByIdWithItemsAsync(shipmentId);
        if (shipment == null)
        {
            throw new NotFoundException($"Shipment with ID '{shipmentId}' not found");
        }

        var user = await _userRepository.GetByIdAsync(updatedBy);
        if (user == null)
        {
            throw new NotFoundException($"User with ID '{updatedBy}' not found");
        }

        // Validate status transition
        if (!shipment.CanTransitionTo(newStatus))
        {
            throw new BusinessException($"Cannot transition shipment from {shipment.Status} to {newStatus}");
        }

        var oldStatus = shipment.Status;
        shipment.UpdateStatus(newStatus);

        _shipmentRepository.Update(shipment);

        // Create blockchain transaction
        var transactionId = await CreateBlockchainTransactionAsync(
            TransactionType.StatusUpdated,
            user.PublicKey,
            new
            {
                ShipmentId = shipmentId,
                OldStatus = oldStatus.ToString(),
                NewStatus = newStatus.ToString(),
                UpdatedBy = updatedBy,
                UpdatedAt = shipment.UpdatedTimestamp
            });

        var transactionIds = await GetShipmentBlockchainHistoryAsync(shipmentId);
        return MapToDto(shipment, transactionIds);
    }

    public async Task<ShipmentDto> ConfirmDeliveryAsync(string shipmentId, string recipientId)
    {
        if (string.IsNullOrWhiteSpace(shipmentId))
        {
            throw new ArgumentException("Shipment ID cannot be null or empty", nameof(shipmentId));
        }

        if (string.IsNullOrWhiteSpace(recipientId))
        {
            throw new ArgumentException("Recipient ID cannot be null or empty", nameof(recipientId));
        }

        var shipment = await _shipmentRepository.GetByIdWithItemsAsync(shipmentId);
        if (shipment == null)
        {
            throw new NotFoundException($"Shipment with ID '{shipmentId}' not found");
        }

        if (shipment.AssignedRecipient != recipientId)
        {
            throw new UnauthorizedException("Only the assigned recipient can confirm delivery");
        }

        var recipient = await _userRepository.GetByIdAsync(recipientId);
        if (recipient == null)
        {
            throw new NotFoundException($"Recipient with ID '{recipientId}' not found");
        }

        if (!shipment.CanTransitionTo(ShipmentStatus.Confirmed))
        {
            throw new BusinessException($"Cannot confirm delivery for shipment in {shipment.Status} status");
        }

        shipment.ConfirmDelivery(); // Uses the method from Core model

        _shipmentRepository.Update(shipment);

        // Create blockchain transaction
        var transactionId = await CreateBlockchainTransactionAsync(
            TransactionType.DeliveryConfirmed,
            recipient.PublicKey,
            new
            {
                ShipmentId = shipmentId,
                RecipientId = recipientId,
                ConfirmedAt = shipment.UpdatedTimestamp,
                ActualDeliveryDate = shipment.ActualDeliveryDate
            });

        var transactionIds = await GetShipmentBlockchainHistoryAsync(shipmentId);
        return MapToDto(shipment, transactionIds);
    }

    public Task<List<string>> GetShipmentBlockchainHistoryAsync(string shipmentId)
    {
        if (string.IsNullOrWhiteSpace(shipmentId))
        {
            throw new ArgumentException("Shipment ID cannot be null or empty", nameof(shipmentId));
        }

        var transactionIds = new List<string>();

        // Search through blockchain for transactions related to this shipment
        for (int i = 0; i < _blockchain.GetChainLength(); i++)
        {
            var block = _blockchain.GetBlockByIndex(i);
            if (block != null)
            {
                foreach (var transaction in block.Transactions)
                {
                    // Check if transaction payload contains the shipment ID
                    if (transaction.PayloadData.Contains(shipmentId))
                    {
                        transactionIds.Add(transaction.Id);
                    }
                }
            }
        }

        return Task.FromResult(transactionIds);
    }

    public async Task<bool> VerifyShipmentOnBlockchainAsync(string shipmentId)
    {
        if (string.IsNullOrWhiteSpace(shipmentId))
        {
            throw new ArgumentException("Shipment ID cannot be null or empty", nameof(shipmentId));
        }

        var transactionIds = await GetShipmentBlockchainHistoryAsync(shipmentId);
        return transactionIds.Any();
    }

    private async Task<string> CreateBlockchainTransactionAsync(
        TransactionType type,
        string senderPublicKey,
        object payload)
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Type = type,
            Timestamp = DateTime.UtcNow,
            SenderPublicKey = senderPublicKey,
            PayloadData = JsonSerializer.Serialize(payload),
            Signature = string.Empty // Will be set by signing
        };

        // Note: In production, transactions should be signed with the user's private key
        // For now, we'll use a placeholder signature
        transaction.Signature = "PLACEHOLDER_SIGNATURE";

        _blockchain.AddTransaction(transaction);

        return await Task.FromResult(transaction.Id);
    }

    private static ShipmentDto MapToDto(Shipment shipment, List<string> transactionIds)
    {
        // Try to parse the expected delivery date from the timeframe string
        DateTime? expectedDate = null;
        if (!string.IsNullOrWhiteSpace(shipment.ExpectedDeliveryTimeframe))
        {
            // Try to extract date if it's in our format
            var match = System.Text.RegularExpressions.Regex.Match(
                shipment.ExpectedDeliveryTimeframe,
                @"(\d{4}-\d{2}-\d{2})");
            if (match.Success && DateTime.TryParse(match.Value, out var parsedDate))
            {
                expectedDate = parsedDate;
            }
        }

        return new ShipmentDto
        {
            Id = shipment.Id,
            Origin = shipment.Origin,
            Destination = shipment.Destination,
            RecipientId = shipment.AssignedRecipient,
            ExpectedDeliveryDate = expectedDate ?? DateTime.UtcNow.AddDays(7), // Default if not parseable
            ActualDeliveryDate = shipment.ActualDeliveryDate,
            Status = shipment.Status,
            QrCode = shipment.QrCodeData,
            CreatedAt = shipment.CreatedTimestamp,
            UpdatedAt = shipment.UpdatedTimestamp,
            Items = shipment.Items.Select(i => new ShipmentItemDto
            {
                Description = i.Description,
                Quantity = i.Quantity,
                Unit = i.Unit
            }).ToList(),
            TransactionIds = transactionIds
        };
    }
}
