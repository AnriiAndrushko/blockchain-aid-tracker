using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.Services.DTOs.Shipment;
using BlockchainAidTracker.Services.Exceptions;
using BlockchainAidTracker.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlockchainAidTracker.Api.Controllers;

/// <summary>
/// Controller for shipment operations with blockchain integration
/// </summary>
[ApiController]
[Route("api/shipments")]
[Produces("application/json")]
[Authorize]
public class ShipmentController : ControllerBase
{
    private readonly IShipmentService _shipmentService;
    private readonly IQrCodeService _qrCodeService;
    private readonly ILogger<ShipmentController> _logger;

    public ShipmentController(
        IShipmentService shipmentService,
        IQrCodeService qrCodeService,
        ILogger<ShipmentController> logger)
    {
        _shipmentService = shipmentService ?? throw new ArgumentNullException(nameof(shipmentService));
        _qrCodeService = qrCodeService ?? throw new ArgumentNullException(nameof(qrCodeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new shipment and records it on the blockchain
    /// </summary>
    /// <param name="request">Shipment creation request</param>
    /// <returns>Created shipment details</returns>
    /// <response code="201">Shipment successfully created</response>
    /// <response code="400">Invalid request or business rule violation</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="403">Forbidden - insufficient permissions</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [ProducesResponseType(typeof(ShipmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ShipmentDto>> CreateShipment([FromBody] CreateShipmentRequest request)
    {
        try
        {
            var userId = GetUserIdFromClaims();
            _logger.LogInformation("Creating shipment from {Origin} to {Destination} by user {UserId}",
                request.Origin, request.Destination, userId);

            var shipment = await _shipmentService.CreateShipmentAsync(request, userId);

            _logger.LogInformation("Shipment {ShipmentId} created successfully", shipment.Id);
            return CreatedAtAction(nameof(GetShipmentById), new { id = shipment.Id }, shipment);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Shipment creation failed: {Message}", ex.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (UnauthorizedException ex)
        {
            _logger.LogWarning("Unauthorized shipment creation attempt: {Message}", ex.Message);
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Forbidden",
                Detail = ex.Message,
                Status = StatusCodes.Status403Forbidden
            });
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning("Shipment creation failed: {Message}", ex.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Shipment Creation Failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating shipment");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets all shipments with optional filtering
    /// </summary>
    /// <param name="status">Optional status filter</param>
    /// <param name="recipientId">Optional recipient ID filter</param>
    /// <returns>List of shipments</returns>
    /// <response code="200">Shipments retrieved successfully</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<ShipmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<ShipmentDto>>> GetShipments(
        [FromQuery] ShipmentStatus? status = null,
        [FromQuery] string? recipientId = null)
    {
        try
        {
            _logger.LogInformation("Retrieving shipments with status: {Status}, recipientId: {RecipientId}",
                status, recipientId);

            var shipments = await _shipmentService.GetShipmentsAsync(status, recipientId);

            _logger.LogInformation("Retrieved {Count} shipments", shipments.Count);
            return Ok(shipments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shipments");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets a specific shipment by ID
    /// </summary>
    /// <param name="id">Shipment ID</param>
    /// <returns>Shipment details</returns>
    /// <response code="200">Shipment retrieved successfully</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="404">Shipment not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ShipmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ShipmentDto>> GetShipmentById(string id)
    {
        try
        {
            _logger.LogInformation("Retrieving shipment {ShipmentId}", id);

            var shipment = await _shipmentService.GetShipmentByIdAsync(id);

            if (shipment == null)
            {
                _logger.LogWarning("Shipment {ShipmentId} not found", id);
                return NotFound(new ProblemDetails
                {
                    Title = "Shipment Not Found",
                    Detail = $"Shipment with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            _logger.LogInformation("Shipment {ShipmentId} retrieved successfully", id);
            return Ok(shipment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shipment {ShipmentId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Updates the status of a shipment and records the change on the blockchain
    /// </summary>
    /// <param name="id">Shipment ID</param>
    /// <param name="request">Status update request</param>
    /// <returns>Updated shipment details</returns>
    /// <response code="200">Status updated successfully</response>
    /// <response code="400">Invalid request or business rule violation</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="403">Forbidden - insufficient permissions</response>
    /// <response code="404">Shipment not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("{id}/status")]
    [ProducesResponseType(typeof(ShipmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ShipmentDto>> UpdateShipmentStatus(string id, [FromBody] UpdateShipmentStatusRequest request)
    {
        try
        {
            var userId = GetUserIdFromClaims();
            _logger.LogInformation("Updating shipment {ShipmentId} status to {NewStatus} by user {UserId}",
                id, request.NewStatus, userId);

            var shipment = await _shipmentService.UpdateShipmentStatusAsync(id, request.NewStatus, userId);

            _logger.LogInformation("Shipment {ShipmentId} status updated successfully", id);
            return Ok(shipment);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Shipment {ShipmentId} not found: {Message}", id, ex.Message);
            return NotFound(new ProblemDetails
            {
                Title = "Shipment Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (UnauthorizedException ex)
        {
            _logger.LogWarning("Unauthorized status update attempt for shipment {ShipmentId}: {Message}", id, ex.Message);
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Forbidden",
                Detail = ex.Message,
                Status = StatusCodes.Status403Forbidden
            });
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning("Status update failed for shipment {ShipmentId}: {Message}", id, ex.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Status Update Failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating shipment {ShipmentId} status", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Confirms delivery of a shipment by the recipient
    /// </summary>
    /// <param name="id">Shipment ID</param>
    /// <returns>Updated shipment details</returns>
    /// <response code="200">Delivery confirmed successfully</response>
    /// <response code="400">Invalid request or business rule violation</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="403">Forbidden - insufficient permissions</response>
    /// <response code="404">Shipment not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("{id}/confirm-delivery")]
    [ProducesResponseType(typeof(ShipmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ShipmentDto>> ConfirmDelivery(string id)
    {
        try
        {
            var userId = GetUserIdFromClaims();
            _logger.LogInformation("Confirming delivery for shipment {ShipmentId} by user {UserId}", id, userId);

            var shipment = await _shipmentService.ConfirmDeliveryAsync(id, userId);

            _logger.LogInformation("Delivery confirmed for shipment {ShipmentId}", id);
            return Ok(shipment);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Shipment {ShipmentId} not found: {Message}", id, ex.Message);
            return NotFound(new ProblemDetails
            {
                Title = "Shipment Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (UnauthorizedException ex)
        {
            _logger.LogWarning("Unauthorized delivery confirmation attempt for shipment {ShipmentId}: {Message}", id, ex.Message);
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Forbidden",
                Detail = ex.Message,
                Status = StatusCodes.Status403Forbidden
            });
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning("Delivery confirmation failed for shipment {ShipmentId}: {Message}", id, ex.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Delivery Confirmation Failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming delivery for shipment {ShipmentId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets the blockchain transaction history for a shipment
    /// </summary>
    /// <param name="id">Shipment ID</param>
    /// <returns>List of transaction IDs</returns>
    /// <response code="200">History retrieved successfully</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="404">Shipment not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id}/history")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<string>>> GetShipmentHistory(string id)
    {
        try
        {
            _logger.LogInformation("Retrieving blockchain history for shipment {ShipmentId}", id);

            var history = await _shipmentService.GetShipmentBlockchainHistoryAsync(id);

            _logger.LogInformation("Retrieved {Count} blockchain transactions for shipment {ShipmentId}",
                history.Count, id);
            return Ok(history);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Shipment {ShipmentId} not found: {Message}", id, ex.Message);
            return NotFound(new ProblemDetails
            {
                Title = "Shipment Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving history for shipment {ShipmentId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets the QR code image for a shipment
    /// </summary>
    /// <param name="id">Shipment ID</param>
    /// <returns>QR code image as PNG</returns>
    /// <response code="200">QR code retrieved successfully</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="404">Shipment not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id}/qrcode")]
    [Produces("image/png")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetShipmentQrCode(string id)
    {
        try
        {
            _logger.LogInformation("Generating QR code for shipment {ShipmentId}", id);

            var shipment = await _shipmentService.GetShipmentByIdAsync(id);

            if (shipment == null)
            {
                _logger.LogWarning("Shipment {ShipmentId} not found", id);
                return NotFound(new ProblemDetails
                {
                    Title = "Shipment Not Found",
                    Detail = $"Shipment with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            var qrCodeBytes = _qrCodeService.GenerateQrCodeAsPng(shipment.QrCode);

            _logger.LogInformation("QR code generated for shipment {ShipmentId}", id);
            return File(qrCodeBytes, "image/png", $"shipment-{id}-qrcode.png");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating QR code for shipment {ShipmentId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Helper method to extract user ID from JWT claims
    /// </summary>
    private string GetUserIdFromClaims()
    {
        return User.FindFirst("sub")?.Value
            ?? User.FindFirst("userId")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedException("User ID not found in token");
    }
}
