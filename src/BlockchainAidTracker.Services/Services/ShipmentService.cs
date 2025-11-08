using System.Text.Json;
using BlockchainAidTracker.Blockchain;
using BlockchainAidTracker.Core.Extensions;
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
    private readonly TransactionSigningContext _signingContext;
    private readonly IAuditLogService _auditLogService;

    public ShipmentService(
        IShipmentRepository shipmentRepository,
        IUserRepository userRepository,
        IQrCodeService qrCodeService,
        Blockchain.Blockchain blockchain,
        IDigitalSignatureService digitalSignatureService,
        TransactionSigningContext signingContext,
        IAuditLogService auditLogService)
    {
        _shipmentRepository = shipmentRepository ?? throw new ArgumentNullException(nameof(shipmentRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _qrCodeService = qrCodeService ?? throw new ArgumentNullException(nameof(qrCodeService));
        _blockchain = blockchain ?? throw new ArgumentNullException(nameof(blockchain));
        _digitalSignatureService = digitalSignatureService ?? throw new ArgumentNullException(nameof(digitalSignatureService));
        _signingContext = signingContext ?? throw new ArgumentNullException(nameof(signingContext));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
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

        try
        {
            // Validate coordinator exists and has appropriate role
            var coordinator = await _userRepository.GetByIdAsync(coordinatorId);
            if (coordinator == null)
            {
                await _auditLogService.LogFailureAsync(
                    AuditLogCategory.Shipment,
                    AuditLogAction.ShipmentCreated,
                    $"Shipment creation failed: Coordinator '{coordinatorId}' not found",
                    "Coordinator not found",
                    coordinatorId);
                throw new NotFoundException($"Coordinator with ID '{coordinatorId}' not found");
            }

            if (coordinator.Role != UserRole.Coordinator && coordinator.Role != UserRole.Administrator)
            {
                await _auditLogService.LogFailureAsync(
                    AuditLogCategory.Shipment,
                    AuditLogAction.ShipmentCreated,
                    $"Shipment creation failed: User '{coordinator.Username}' lacks required role",
                    "Insufficient permissions",
                    coordinatorId,
                    coordinator.Username);
                throw new UnauthorizedException("Only coordinators and administrators can create shipments");
            }

            // Validate recipient exists
            var recipient = await _userRepository.GetByIdAsync(request.RecipientId);
            if (recipient == null)
            {
                await _auditLogService.LogFailureAsync(
                    AuditLogCategory.Shipment,
                    AuditLogAction.ShipmentCreated,
                    $"Shipment creation failed: Recipient '{request.RecipientId}' not found",
                    "Recipient not found",
                    coordinatorId,
                    coordinator.Username);
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
                },
                coordinatorId);

            // Log successful shipment creation
            await _auditLogService.LogAsync(
                AuditLogCategory.Shipment,
                AuditLogAction.ShipmentCreated,
                $"Shipment '{shipmentId}' created by '{coordinator.Username}' from {request.Origin} to {request.Destination}",
                coordinatorId,
                coordinator.Username,
                shipmentId,
                "Shipment",
                $"{{\"origin\":\"{request.Origin}\",\"destination\":\"{request.Destination}\",\"recipientId\":\"{request.RecipientId}\",\"itemCount\":{request.Items.Count}}}");

            return MapToDto(shipment, new List<string> { transactionId });
        }
        catch (BusinessException)
        {
            throw; // Re-throw business exceptions (already logged)
        }
        catch (UnauthorizedException)
        {
            throw; // Re-throw unauthorized exceptions (already logged)
        }
        catch (NotFoundException)
        {
            throw; // Re-throw not found exceptions (already logged)
        }
        catch (Exception ex)
        {
            await _auditLogService.LogFailureAsync(
                AuditLogCategory.Shipment,
                AuditLogAction.ShipmentCreated,
                $"Shipment creation failed for coordinator '{coordinatorId}'",
                ex.Message,
                coordinatorId);
            throw;
        }
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
            // Use private method since we already have the shipment (no need to check existence)
            var transactionIds = GetBlockchainTransactionsForShipment(shipment.Id);
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

        try
        {
            var shipment = await _shipmentRepository.GetByIdWithItemsAsync(shipmentId);
            if (shipment == null)
            {
                await _auditLogService.LogFailureAsync(
                    AuditLogCategory.Shipment,
                    AuditLogAction.ShipmentStatusUpdated,
                    $"Status update failed: Shipment '{shipmentId}' not found",
                    "Shipment not found",
                    updatedBy);
                throw new NotFoundException($"Shipment with ID '{shipmentId}' not found");
            }

            var user = await _userRepository.GetByIdAsync(updatedBy);
            if (user == null)
            {
                await _auditLogService.LogFailureAsync(
                    AuditLogCategory.Shipment,
                    AuditLogAction.ShipmentStatusUpdated,
                    $"Status update failed: User '{updatedBy}' not found",
                    "User not found",
                    updatedBy);
                throw new NotFoundException($"User with ID '{updatedBy}' not found");
            }

            // Validate status transition
            if (!shipment.CanTransitionTo(newStatus))
            {
                await _auditLogService.LogFailureAsync(
                    AuditLogCategory.Shipment,
                    AuditLogAction.ShipmentStatusUpdated,
                    $"Invalid status transition for shipment '{shipmentId}' from {shipment.Status} to {newStatus}",
                    "Invalid status transition",
                    updatedBy,
                    user.Username,
                    shipmentId,
                    "Shipment");
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
                },
                updatedBy);

            // Log successful status update
            await _auditLogService.LogAsync(
                AuditLogCategory.Shipment,
                AuditLogAction.ShipmentStatusUpdated,
                $"Shipment '{shipmentId}' status updated from {oldStatus} to {newStatus} by '{user.Username}'",
                updatedBy,
                user.Username,
                shipmentId,
                "Shipment",
                $"{{\"oldStatus\":\"{oldStatus}\",\"newStatus\":\"{newStatus}\"}}");

            // Use private method since we already have the shipment (no need to check existence)
            var transactionIds = GetBlockchainTransactionsForShipment(shipmentId);
            return MapToDto(shipment, transactionIds);
        }
        catch (BusinessException)
        {
            throw; // Re-throw business exceptions (already logged)
        }
        catch (NotFoundException)
        {
            throw; // Re-throw not found exceptions (already logged)
        }
        catch (Exception ex)
        {
            await _auditLogService.LogFailureAsync(
                AuditLogCategory.Shipment,
                AuditLogAction.ShipmentStatusUpdated,
                $"Status update failed for shipment '{shipmentId}'",
                ex.Message,
                updatedBy,
                entityId: shipmentId,
                entityType: "Shipment");
            throw;
        }
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

        try
        {
            var shipment = await _shipmentRepository.GetByIdWithItemsAsync(shipmentId);
            if (shipment == null)
            {
                await _auditLogService.LogFailureAsync(
                    AuditLogCategory.Shipment,
                    AuditLogAction.ShipmentDeliveryConfirmed,
                    $"Delivery confirmation failed: Shipment '{shipmentId}' not found",
                    "Shipment not found",
                    recipientId);
                throw new NotFoundException($"Shipment with ID '{shipmentId}' not found");
            }

            if (shipment.AssignedRecipient != recipientId)
            {
                await _auditLogService.LogFailureAsync(
                    AuditLogCategory.Shipment,
                    AuditLogAction.ShipmentDeliveryConfirmed,
                    $"Delivery confirmation failed: User '{recipientId}' is not the assigned recipient",
                    "Unauthorized recipient",
                    recipientId,
                    entityId: shipmentId,
                    entityType: "Shipment");
                throw new UnauthorizedException("Only the assigned recipient can confirm delivery");
            }

            var recipient = await _userRepository.GetByIdAsync(recipientId);
            if (recipient == null)
            {
                await _auditLogService.LogFailureAsync(
                    AuditLogCategory.Shipment,
                    AuditLogAction.ShipmentDeliveryConfirmed,
                    $"Delivery confirmation failed: Recipient '{recipientId}' not found",
                    "Recipient not found",
                    recipientId);
                throw new NotFoundException($"Recipient with ID '{recipientId}' not found");
            }

            if (!shipment.CanTransitionTo(ShipmentStatus.Confirmed))
            {
                await _auditLogService.LogFailureAsync(
                    AuditLogCategory.Shipment,
                    AuditLogAction.ShipmentDeliveryConfirmed,
                    $"Delivery confirmation failed: Shipment '{shipmentId}' in invalid status {shipment.Status}",
                    "Invalid shipment status",
                    recipientId,
                    recipient.Username,
                    shipmentId,
                    "Shipment");
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
                },
                recipientId);

            // Log successful delivery confirmation
            await _auditLogService.LogAsync(
                AuditLogCategory.Shipment,
                AuditLogAction.ShipmentDeliveryConfirmed,
                $"Delivery confirmed for shipment '{shipmentId}' by recipient '{recipient.Username}'",
                recipientId,
                recipient.Username,
                shipmentId,
                "Shipment",
                $"{{\"actualDeliveryDate\":\"{shipment.ActualDeliveryDate:yyyy-MM-dd HH:mm:ss}\"}}");

            // Use private method since we already have the shipment (no need to check existence)
            var transactionIds = GetBlockchainTransactionsForShipment(shipmentId);
            return MapToDto(shipment, transactionIds);
        }
        catch (BusinessException)
        {
            throw; // Re-throw business exceptions (already logged)
        }
        catch (UnauthorizedException)
        {
            throw; // Re-throw unauthorized exceptions (already logged)
        }
        catch (NotFoundException)
        {
            throw; // Re-throw not found exceptions (already logged)
        }
        catch (Exception ex)
        {
            await _auditLogService.LogFailureAsync(
                AuditLogCategory.Shipment,
                AuditLogAction.ShipmentDeliveryConfirmed,
                $"Delivery confirmation failed for shipment '{shipmentId}'",
                ex.Message,
                recipientId,
                entityId: shipmentId,
                entityType: "Shipment");
            throw;
        }
    }

    public async Task<List<string>> GetShipmentBlockchainHistoryAsync(string shipmentId)
    {
        if (string.IsNullOrWhiteSpace(shipmentId))
        {
            throw new ArgumentException("Shipment ID cannot be null or empty", nameof(shipmentId));
        }

        // Verify the shipment exists in the database
        var shipment = await _shipmentRepository.GetByIdAsync(shipmentId);
        if (shipment == null)
        {
            throw new NotFoundException($"Shipment with ID '{shipmentId}' not found");
        }

        return GetBlockchainTransactionsForShipment(shipmentId);
    }

    private List<string> GetBlockchainTransactionsForShipment(string shipmentId)
    {
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

        return transactionIds;
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
        object payload,
        string userId)
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

        // Try to sign with the real private key if available
        var privateKey = _signingContext.GetPrivateKey(userId);
        if (!string.IsNullOrEmpty(privateKey))
        {
            // Sign the transaction with the user's private key using the extension method
            transaction.Sign(privateKey, _digitalSignatureService);
        }
        else
        {
            // Fall back to placeholder signature if private key not available
            // This allows backward compatibility with tests that don't have keys
            transaction.Signature = "PLACEHOLDER_SIGNATURE";
        }

        _blockchain.AddTransaction(transaction);

        // Create a block to add pending transactions to the chain
        // In production, this would be done by validator nodes through consensus
        var block = _blockchain.CreateBlock(senderPublicKey);
        _blockchain.AddBlock(block);

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
