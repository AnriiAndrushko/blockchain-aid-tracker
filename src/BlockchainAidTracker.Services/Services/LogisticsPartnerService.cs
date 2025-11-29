using System.Text.Json;
using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.DataAccess.Repositories;
using BlockchainAidTracker.Services.DTOs.LogisticsPartner;
using BlockchainAidTracker.Services.DTOs.Shipment;
using BlockchainAidTracker.Services.Exceptions;
using Microsoft.Extensions.Logging;

namespace BlockchainAidTracker.Services.Services;

/// <summary>
/// Service implementation for LogisticsPartner operations including location tracking and delivery events
/// </summary>
public class LogisticsPartnerService : ILogisticsPartnerService
{
    private readonly IShipmentRepository _shipmentRepository;
    private readonly IShipmentLocationRepository _locationRepository;
    private readonly IDeliveryEventRepository _eventRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<LogisticsPartnerService> _logger;

    public LogisticsPartnerService(
        IShipmentRepository shipmentRepository,
        IShipmentLocationRepository locationRepository,
        IDeliveryEventRepository eventRepository,
        IUserRepository userRepository,
        ILogger<LogisticsPartnerService> logger)
    {
        _shipmentRepository = shipmentRepository ?? throw new ArgumentNullException(nameof(shipmentRepository));
        _locationRepository = locationRepository ?? throw new ArgumentNullException(nameof(locationRepository));
        _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<ShipmentDto>> GetAssignedShipmentsAsync(string userId, string? status = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        // Verify user is a LogisticsPartner
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new NotFoundException($"User with ID '{userId}' not found");

        if (user.Role != UserRole.LogisticsPartner && user.Role != UserRole.Administrator)
            throw new UnauthorizedException("Only logistics partners and administrators can view assigned shipments");

        // Get shipments in transit (assigned to logistics partners)
        var shipments = await _shipmentRepository.GetByStatusAsync(ShipmentStatus.InTransit);

        var dtos = shipments.Select(s => new ShipmentDto
        {
            Id = s.Id,
            Origin = s.Origin,
            Destination = s.Destination,
            Status = s.Status,
            CreatedAt = s.CreatedTimestamp,
            UpdatedAt = s.UpdatedTimestamp
        }).ToList();

        _logger.LogInformation("Retrieved {Count} assigned shipments for user {UserId}", dtos.Count, userId);
        return dtos;
    }

    public async Task<ShipmentLocationDto?> GetShipmentLocationAsync(string shipmentId)
    {
        if (string.IsNullOrWhiteSpace(shipmentId))
            throw new ArgumentException("Shipment ID cannot be null or empty", nameof(shipmentId));

        var location = await _locationRepository.GetLatestAsync(shipmentId);
        if (location == null)
        {
            _logger.LogInformation("No location found for shipment {ShipmentId}", shipmentId);
            return null;
        }

        return MapToDto(location);
    }

    public async Task<ShipmentLocationDto> UpdateLocationAsync(
        string shipmentId,
        string userId,
        UpdateLocationRequest request)
    {
        if (string.IsNullOrWhiteSpace(shipmentId))
            throw new ArgumentException("Shipment ID cannot be null or empty", nameof(shipmentId));

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // Verify shipment exists
        var shipment = await _shipmentRepository.GetByIdAsync(shipmentId);
        if (shipment == null)
            throw new NotFoundException($"Shipment with ID '{shipmentId}' not found");

        // Verify user is a logistics partner
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new NotFoundException($"User with ID '{userId}' not found");

        if (user.Role != UserRole.LogisticsPartner && user.Role != UserRole.Administrator)
            throw new UnauthorizedException("Only logistics partners can update shipment locations");

        // Create location record
        var location = new ShipmentLocation(
            shipmentId,
            request.Latitude,
            request.Longitude,
            request.LocationName,
            userId,
            request.GpsAccuracy);

        if (!location.IsValidCoordinates())
            throw new BusinessException("Invalid coordinates provided");

        await _locationRepository.AddAsync(location);

        // Create delivery event for location update
        var eventMetadata = new { latitude = location.Latitude, longitude = location.Longitude };
        var deliveryEvent = new DeliveryEvent(
            shipmentId,
            DeliveryEventType.LocationUpdated,
            $"Location updated to {request.LocationName}",
            userId,
            JsonSerializer.Serialize(eventMetadata));

        await _eventRepository.AddAsync(deliveryEvent);

        _logger.LogInformation(
            "Updated location for shipment {ShipmentId} to {LocationName} by user {UserId}",
            shipmentId,
            request.LocationName,
            userId);

        return MapToDto(location);
    }

    public async Task<DeliveryEventDto> ConfirmDeliveryInitiationAsync(string shipmentId, string userId)
    {
        if (string.IsNullOrWhiteSpace(shipmentId))
            throw new ArgumentException("Shipment ID cannot be null or empty", nameof(shipmentId));

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        // Verify shipment exists and is in correct status
        var shipment = await _shipmentRepository.GetByIdAsync(shipmentId);
        if (shipment == null)
            throw new NotFoundException($"Shipment with ID '{shipmentId}' not found");

        if (shipment.Status != ShipmentStatus.InTransit)
            throw new BusinessException($"Shipment must be in InTransit status, current status is {shipment.Status}");

        // Create delivery event
        var deliveryEvent = new DeliveryEvent(
            shipmentId,
            DeliveryEventType.DeliveryStarted,
            "Delivery has been initiated",
            userId);

        await _eventRepository.AddAsync(deliveryEvent);

        _logger.LogInformation(
            "Confirmed delivery initiation for shipment {ShipmentId} by user {UserId}",
            shipmentId,
            userId);

        return MapToDto(deliveryEvent);
    }

    public async Task<List<DeliveryEventDto>> GetDeliveryHistoryAsync(string shipmentId)
    {
        if (string.IsNullOrWhiteSpace(shipmentId))
            throw new ArgumentException("Shipment ID cannot be null or empty", nameof(shipmentId));

        // Verify shipment exists
        var shipment = await _shipmentRepository.GetByIdAsync(shipmentId);
        if (shipment == null)
            throw new NotFoundException($"Shipment with ID '{shipmentId}' not found");

        var events = await _eventRepository.GetByShipmentAsync(shipmentId);
        _logger.LogInformation("Retrieved {Count} delivery events for shipment {ShipmentId}", events.Count, shipmentId);

        return events.Select(MapToDto).ToList();
    }

    public async Task<List<ShipmentLocationDto>> GetLocationHistoryAsync(string shipmentId, int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(shipmentId))
            throw new ArgumentException("Shipment ID cannot be null or empty", nameof(shipmentId));

        if (limit < 1)
            throw new ArgumentException("Limit must be 1 or greater", nameof(limit));

        // Verify shipment exists
        var shipment = await _shipmentRepository.GetByIdAsync(shipmentId);
        if (shipment == null)
            throw new NotFoundException($"Shipment with ID '{shipmentId}' not found");

        var locations = await _locationRepository.GetPaginatedAsync(shipmentId, 1, limit);
        _logger.LogInformation(
            "Retrieved {Count} location history records for shipment {ShipmentId}",
            locations.Count,
            shipmentId);

        return locations.Select(MapToDto).ToList();
    }

    public async Task<DeliveryEventDto> ReportDeliveryIssueAsync(
        string shipmentId,
        string userId,
        ReportIssueRequest request)
    {
        if (string.IsNullOrWhiteSpace(shipmentId))
            throw new ArgumentException("Shipment ID cannot be null or empty", nameof(shipmentId));

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // Verify shipment exists
        var shipment = await _shipmentRepository.GetByIdAsync(shipmentId);
        if (shipment == null)
            throw new NotFoundException($"Shipment with ID '{shipmentId}' not found");

        // Create delivery event with issue details
        var metadata = new
        {
            issueType = request.IssueType.ToString(),
            priority = request.Priority,
            timestamp = DateTime.UtcNow
        };

        var deliveryEvent = new DeliveryEvent(
            shipmentId,
            DeliveryEventType.IssueReported,
            request.Description,
            userId,
            JsonSerializer.Serialize(metadata));

        await _eventRepository.AddAsync(deliveryEvent);

        _logger.LogWarning(
            "Issue reported for shipment {ShipmentId}: {IssueType} - {Description}",
            shipmentId,
            request.IssueType,
            request.Description);

        return MapToDto(deliveryEvent);
    }

    public async Task<DeliveryEventDto> ConfirmReceiptAsync(string shipmentId, string userId)
    {
        if (string.IsNullOrWhiteSpace(shipmentId))
            throw new ArgumentException("Shipment ID cannot be null or empty", nameof(shipmentId));

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        // Verify shipment exists
        var shipment = await _shipmentRepository.GetByIdAsync(shipmentId);
        if (shipment == null)
            throw new NotFoundException($"Shipment with ID '{shipmentId}' not found");

        // Create delivery event for final receipt
        var deliveryEvent = new DeliveryEvent(
            shipmentId,
            DeliveryEventType.ReceiptConfirmed,
            "Final receipt confirmed by logistics partner",
            userId);

        await _eventRepository.AddAsync(deliveryEvent);

        _logger.LogInformation(
            "Confirmed final receipt for shipment {ShipmentId} by user {UserId}",
            shipmentId,
            userId);

        return MapToDto(deliveryEvent);
    }

    // Helper methods
    private ShipmentLocationDto MapToDto(ShipmentLocation location)
    {
        return new ShipmentLocationDto
        {
            Id = location.Id,
            ShipmentId = location.ShipmentId,
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            LocationName = location.LocationName,
            CreatedTimestamp = location.CreatedTimestamp,
            GpsAccuracy = location.GpsAccuracy,
            UpdatedByUserId = location.UpdatedByUserId
        };
    }

    private DeliveryEventDto MapToDto(DeliveryEvent deliveryEvent)
    {
        return new DeliveryEventDto
        {
            Id = deliveryEvent.Id,
            ShipmentId = deliveryEvent.ShipmentId,
            EventType = deliveryEvent.EventType,
            Description = deliveryEvent.Description,
            CreatedTimestamp = deliveryEvent.CreatedTimestamp,
            CreatedByUserId = deliveryEvent.CreatedByUserId,
            Metadata = deliveryEvent.Metadata
        };
    }
}
