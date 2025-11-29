using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.Services;
using BlockchainAidTracker.Services.DTOs.LogisticsPartner;
using BlockchainAidTracker.Services.DTOs.Shipment;
using BlockchainAidTracker.Services.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlockchainAidTracker.Api.Controllers;

/// <summary>
/// API endpoints for logistics partner shipment tracking and delivery management
/// </summary>
[ApiController]
[Route("api/logistics-partner")]
[Authorize]
public class LogisticsPartnerController : ControllerBase
{
    private readonly ILogisticsPartnerService _logisticsPartnerService;
    private readonly ILogger<LogisticsPartnerController> _logger;

    public LogisticsPartnerController(
        ILogisticsPartnerService logisticsPartnerService,
        ILogger<LogisticsPartnerController> logger)
    {
        _logisticsPartnerService = logisticsPartnerService ?? throw new ArgumentNullException(nameof(logisticsPartnerService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets assigned shipments for the current logistics partner
    /// </summary>
    /// <param name="status">Optional shipment status filter</param>
    /// <returns>List of assigned shipments</returns>
    [HttpGet("shipments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<ShipmentDto>>> GetAssignedShipments([FromQuery] string? status = null)
    {
        var userId = GetUserIdFromClaims();

        try
        {
            var shipments = await _logisticsPartnerService.GetAssignedShipmentsAsync(userId, status);
            _logger.LogInformation("Retrieved {Count} assigned shipments for user {UserId}", shipments.Count, userId);
            return Ok(shipments);
        }
        catch (UnauthorizedException ex)
        {
            _logger.LogWarning("Unauthorized access attempt: {Message}", ex.Message);
            return Forbid();
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Resource not found: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving assigned shipments for user {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred retrieving shipments" });
        }
    }

    /// <summary>
    /// Gets the current location of a shipment
    /// </summary>
    /// <param name="shipmentId">The shipment ID</param>
    /// <returns>Current shipment location</returns>
    [HttpGet("shipments/{shipmentId}/location")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShipmentLocationDto>> GetShipmentLocation(string shipmentId)
    {
        if (string.IsNullOrWhiteSpace(shipmentId))
            return BadRequest(new { message = "Shipment ID is required" });

        try
        {
            var location = await _logisticsPartnerService.GetShipmentLocationAsync(shipmentId);
            if (location == null)
                return NotFound(new { message = $"No location found for shipment {shipmentId}" });

            return Ok(location);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Location not found: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving location for shipment {ShipmentId}", shipmentId);
            return StatusCode(500, new { message = "An error occurred retrieving location" });
        }
    }

    /// <summary>
    /// Updates the location of a shipment
    /// </summary>
    /// <param name="shipmentId">The shipment ID</param>
    /// <param name="request">Location update request</param>
    /// <returns>Updated location information</returns>
    [HttpPut("shipments/{shipmentId}/location")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShipmentLocationDto>> UpdateLocation(
        string shipmentId,
        [FromBody] UpdateLocationRequest request)
    {
        if (string.IsNullOrWhiteSpace(shipmentId))
            return BadRequest(new { message = "Shipment ID is required" });

        if (request == null)
            return BadRequest(new { message = "Location update request is required" });

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetUserIdFromClaims();

        try
        {
            var location = await _logisticsPartnerService.UpdateLocationAsync(shipmentId, userId, request);
            _logger.LogInformation("Updated location for shipment {ShipmentId}", shipmentId);
            return Ok(location);
        }
        catch (UnauthorizedException ex)
        {
            _logger.LogWarning("Unauthorized location update: {Message}", ex.Message);
            return Forbid();
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Resource not found: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning("Business logic error: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating location for shipment {ShipmentId}", shipmentId);
            return StatusCode(500, new { message = "An error occurred updating location" });
        }
    }

    /// <summary>
    /// Confirms that delivery has started for a shipment
    /// </summary>
    /// <param name="shipmentId">The shipment ID</param>
    /// <returns>Delivery event confirmation</returns>
    [HttpPost("shipments/{shipmentId}/delivery-started")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeliveryEventDto>> ConfirmDeliveryInitiation(string shipmentId)
    {
        if (string.IsNullOrWhiteSpace(shipmentId))
            return BadRequest(new { message = "Shipment ID is required" });

        var userId = GetUserIdFromClaims();

        try
        {
            var deliveryEvent = await _logisticsPartnerService.ConfirmDeliveryInitiationAsync(shipmentId, userId);
            _logger.LogInformation("Confirmed delivery initiation for shipment {ShipmentId}", shipmentId);
            return Ok(deliveryEvent);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Resource not found: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning("Business logic error: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming delivery initiation for shipment {ShipmentId}", shipmentId);
            return StatusCode(500, new { message = "An error occurred confirming delivery initiation" });
        }
    }

    /// <summary>
    /// Gets the delivery history for a shipment
    /// </summary>
    /// <param name="shipmentId">The shipment ID</param>
    /// <returns>List of delivery events</returns>
    [HttpGet("shipments/{shipmentId}/delivery-history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<DeliveryEventDto>>> GetDeliveryHistory(string shipmentId)
    {
        if (string.IsNullOrWhiteSpace(shipmentId))
            return BadRequest(new { message = "Shipment ID is required" });

        try
        {
            var history = await _logisticsPartnerService.GetDeliveryHistoryAsync(shipmentId);
            _logger.LogInformation("Retrieved {Count} delivery events for shipment {ShipmentId}", history.Count, shipmentId);
            return Ok(history);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Resource not found: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving delivery history for shipment {ShipmentId}", shipmentId);
            return StatusCode(500, new { message = "An error occurred retrieving delivery history" });
        }
    }

    /// <summary>
    /// Gets location history for a shipment
    /// </summary>
    /// <param name="shipmentId">The shipment ID</param>
    /// <param name="limit">Maximum number of recent locations (default: 10)</param>
    /// <returns>List of location records</returns>
    [HttpGet("shipments/{shipmentId}/location-history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ShipmentLocationDto>>> GetLocationHistory(
        string shipmentId,
        [FromQuery] int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(shipmentId))
            return BadRequest(new { message = "Shipment ID is required" });

        if (limit < 1 || limit > 100)
            return BadRequest(new { message = "Limit must be between 1 and 100" });

        try
        {
            var history = await _logisticsPartnerService.GetLocationHistoryAsync(shipmentId, limit);
            _logger.LogInformation("Retrieved {Count} location records for shipment {ShipmentId}", history.Count, shipmentId);
            return Ok(history);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Resource not found: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving location history for shipment {ShipmentId}", shipmentId);
            return StatusCode(500, new { message = "An error occurred retrieving location history" });
        }
    }

    /// <summary>
    /// Reports a delivery issue
    /// </summary>
    /// <param name="shipmentId">The shipment ID</param>
    /// <param name="request">Issue report request</param>
    /// <returns>Delivery event recording the issue</returns>
    [HttpPost("shipments/{shipmentId}/report-issue")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeliveryEventDto>> ReportDeliveryIssue(
        string shipmentId,
        [FromBody] ReportIssueRequest request)
    {
        if (string.IsNullOrWhiteSpace(shipmentId))
            return BadRequest(new { message = "Shipment ID is required" });

        if (request == null)
            return BadRequest(new { message = "Issue report request is required" });

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetUserIdFromClaims();

        try
        {
            var deliveryEvent = await _logisticsPartnerService.ReportDeliveryIssueAsync(shipmentId, userId, request);
            _logger.LogWarning("Issue reported for shipment {ShipmentId}: {IssueType}", shipmentId, request.IssueType);
            return Ok(deliveryEvent);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Resource not found: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting delivery issue for shipment {ShipmentId}", shipmentId);
            return StatusCode(500, new { message = "An error occurred reporting the delivery issue" });
        }
    }

    /// <summary>
    /// Confirms final receipt of a shipment
    /// </summary>
    /// <param name="shipmentId">The shipment ID</param>
    /// <returns>Delivery event confirmation</returns>
    [HttpPost("shipments/{shipmentId}/confirm-receipt")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeliveryEventDto>> ConfirmReceipt(string shipmentId)
    {
        if (string.IsNullOrWhiteSpace(shipmentId))
            return BadRequest(new { message = "Shipment ID is required" });

        var userId = GetUserIdFromClaims();

        try
        {
            var deliveryEvent = await _logisticsPartnerService.ConfirmReceiptAsync(shipmentId, userId);
            _logger.LogInformation("Confirmed final receipt for shipment {ShipmentId}", shipmentId);
            return Ok(deliveryEvent);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Resource not found: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming receipt for shipment {ShipmentId}", shipmentId);
            return StatusCode(500, new { message = "An error occurred confirming receipt" });
        }
    }

    private string GetUserIdFromClaims()
    {
        return User.FindFirst("sub")?.Value
            ?? User.FindFirst("userId")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedException("User ID not found in token");
    }
}
