using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.Services.DTOs.Supplier;
using BlockchainAidTracker.Services.Exceptions;
using BlockchainAidTracker.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlockchainAidTracker.Api.Controllers;

/// <summary>
/// Controller for supplier/customer management operations
/// </summary>
[ApiController]
[Route("api/suppliers")]
[Produces("application/json")]
[Authorize]
public class SupplierController : ControllerBase
{
    private readonly ISupplierService _supplierService;
    private readonly IKeyManagementService _keyManagementService;
    private readonly ILogger<SupplierController> _logger;

    public SupplierController(
        ISupplierService supplierService,
        IKeyManagementService keyManagementService,
        ILogger<SupplierController> logger)
    {
        _supplierService = supplierService ?? throw new ArgumentNullException(nameof(supplierService));
        _keyManagementService = keyManagementService ?? throw new ArgumentNullException(nameof(keyManagementService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers a new supplier in the system (Customer role)
    /// </summary>
    /// <param name="request">Supplier registration request</param>
    /// <returns>Newly registered supplier</returns>
    /// <response code="201">Supplier registered successfully</response>
    /// <response code="400">Invalid request or business rule violation</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="403">Forbidden - requires Customer role</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SupplierDto>> RegisterSupplier([FromBody] CreateSupplierRequest request)
    {
        try
        {
            // Check if user is Customer role
            if (!IsCustomer())
            {
                _logger.LogWarning("Unauthorized attempt to register supplier by non-customer user");
                return Forbid();
            }

            var userId = GetUserIdFromClaims();
            _logger.LogInformation("Registering new supplier for user {UserId}: {CompanyName}", userId, request.CompanyName);

            var supplier = await _supplierService.RegisterSupplierAsync(userId, request, _keyManagementService);

            _logger.LogInformation("Supplier registered successfully: {SupplierId}, {CompanyName}",
                supplier.Id, supplier.CompanyName);

            return CreatedAtAction(
                nameof(GetSupplierById),
                new { id = supplier.Id },
                supplier);
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning("Supplier registration failed: {Message}", ex.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Supplier Registration Failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering supplier");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets supplier details by ID
    /// </summary>
    /// <param name="id">Supplier ID</param>
    /// <returns>Supplier details</returns>
    /// <response code="200">Supplier retrieved successfully</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="403">Forbidden - only admin or supplier owner can view</response>
    /// <response code="404">Supplier not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SupplierDto>> GetSupplierById(string id)
    {
        try
        {
            _logger.LogInformation("Retrieving supplier {SupplierId}", id);

            var supplier = await _supplierService.GetSupplierByIdAsync(id);

            if (supplier == null)
            {
                _logger.LogWarning("Supplier {SupplierId} not found", id);
                return NotFound(new ProblemDetails
                {
                    Title = "Supplier Not Found",
                    Detail = $"Supplier with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // Check access control: Admin or supplier owner
            var userId = GetUserIdFromClaims();
            if (!IsAdministrator() && supplier.UserId != userId)
            {
                _logger.LogWarning("Unauthorized access attempt to supplier {SupplierId} by user {UserId}", id, userId);
                return Forbid();
            }

            _logger.LogInformation("Supplier retrieved successfully: {SupplierId}", id);
            return Ok(supplier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving supplier {SupplierId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets all suppliers with optional filtering (Admin only)
    /// </summary>
    /// <param name="verificationStatus">Filter by verification status (Pending/Verified/Rejected)</param>
    /// <param name="activeOnly">If true, returns only active suppliers</param>
    /// <returns>List of suppliers</returns>
    /// <response code="200">Suppliers retrieved successfully</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="403">Forbidden - requires Administrator role</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<SupplierDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<SupplierDto>>> GetAllSuppliers([FromQuery] string? verificationStatus = null, [FromQuery] bool activeOnly = false)
    {
        try
        {
            // Check if user is Administrator
            if (!IsAdministrator())
            {
                _logger.LogWarning("Unauthorized attempt to list all suppliers");
                return Forbid();
            }

            _logger.LogInformation("Retrieving suppliers (status: {Status}, activeOnly: {ActiveOnly})", verificationStatus ?? "all", activeOnly);

            // Parse verification status if provided
            SupplierVerificationStatus? statusFilter = null;
            if (!string.IsNullOrEmpty(verificationStatus))
            {
                if (Enum.TryParse<SupplierVerificationStatus>(verificationStatus, out var parsed))
                {
                    statusFilter = parsed;
                }
            }

            var suppliers = await _supplierService.GetAllSuppliersAsync(statusFilter);

            if (activeOnly)
            {
                suppliers = suppliers.Where(s => s.IsActive).ToList();
            }

            _logger.LogInformation("Retrieved {Count} suppliers", suppliers.Count);
            return Ok(suppliers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving suppliers");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Updates supplier information
    /// </summary>
    /// <param name="id">Supplier ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated supplier</returns>
    /// <response code="200">Supplier updated successfully</response>
    /// <response code="400">Invalid request or business rule violation</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="403">Forbidden - only owner or admin can update</response>
    /// <response code="404">Supplier not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SupplierDto>> UpdateSupplier(string id, [FromBody] UpdateSupplierRequest request)
    {
        try
        {
            _logger.LogInformation("Updating supplier {SupplierId}", id);

            var supplier = await _supplierService.GetSupplierByIdAsync(id);
            if (supplier == null)
            {
                _logger.LogWarning("Supplier {SupplierId} not found", id);
                return NotFound(new ProblemDetails
                {
                    Title = "Supplier Not Found",
                    Detail = $"Supplier with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // Check access control: Admin or supplier owner
            var userId = GetUserIdFromClaims();
            if (!IsAdministrator() && supplier.UserId != userId)
            {
                _logger.LogWarning("Unauthorized update attempt to supplier {SupplierId} by user {UserId}", id, userId);
                return Forbid();
            }

            var updatedSupplier = await _supplierService.UpdateSupplierAsync(id, request, _keyManagementService);

            _logger.LogInformation("Supplier {SupplierId} updated successfully", id);
            return Ok(updatedSupplier);
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning("Supplier update failed: {Message}", ex.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Supplier Update Failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Supplier not found: {Message}", ex.Message);
            return NotFound(new ProblemDetails
            {
                Title = "Supplier Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating supplier {SupplierId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Verifies or rejects a supplier (Admin only)
    /// </summary>
    /// <param name="id">Supplier ID</param>
    /// <param name="status">New verification status (Verified/Rejected)</param>
    /// <returns>Success message</returns>
    /// <response code="200">Supplier verified/rejected successfully</response>
    /// <response code="400">Invalid status or business rule violation</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="403">Forbidden - requires Administrator role</response>
    /// <response code="404">Supplier not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("{id}/verify")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> VerifySupplier(string id, [FromQuery] string status)
    {
        try
        {
            // Check if user is Administrator
            if (!IsAdministrator())
            {
                _logger.LogWarning("Unauthorized attempt to verify supplier");
                return Forbid();
            }

            _logger.LogInformation("Verifying supplier {SupplierId} with status {Status}", id, status);

            if (status == "Verified")
            {
                await _supplierService.VerifySupplierAsync(id);
                _logger.LogInformation("Supplier {SupplierId} verified successfully", id);
            }
            else if (status == "Rejected")
            {
                await _supplierService.RejectSupplierAsync(id);
                _logger.LogInformation("Supplier {SupplierId} rejected successfully", id);
            }
            else
            {
                _logger.LogWarning("Invalid verification status: {Status}", status);
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Status",
                    Detail = "Status must be 'Verified' or 'Rejected'",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            return Ok(new { message = $"Supplier {status.ToLower()} successfully" });
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Supplier not found: {Message}", ex.Message);
            return NotFound(new ProblemDetails
            {
                Title = "Supplier Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying supplier {SupplierId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Activates a supplier (Admin only)
    /// </summary>
    /// <param name="id">Supplier ID</param>
    /// <returns>Success message</returns>
    /// <response code="200">Supplier activated successfully</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="403">Forbidden - requires Administrator role</response>
    /// <response code="404">Supplier not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("{id}/activate")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> ActivateSupplier(string id)
    {
        try
        {
            // Check if user is Administrator
            if (!IsAdministrator())
            {
                _logger.LogWarning("Unauthorized attempt to activate supplier");
                return Forbid();
            }

            _logger.LogInformation("Activating supplier {SupplierId}", id);

            await _supplierService.ActivateSupplierAsync(id);

            _logger.LogInformation("Supplier {SupplierId} activated successfully", id);
            return Ok(new { message = "Supplier activated successfully" });
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Supplier not found: {Message}", ex.Message);
            return NotFound(new ProblemDetails
            {
                Title = "Supplier Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating supplier {SupplierId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Deactivates a supplier (Admin only)
    /// </summary>
    /// <param name="id">Supplier ID</param>
    /// <returns>Success message</returns>
    /// <response code="200">Supplier deactivated successfully</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="403">Forbidden - requires Administrator role</response>
    /// <response code="404">Supplier not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("{id}/deactivate")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeactivateSupplier(string id)
    {
        try
        {
            // Check if user is Administrator
            if (!IsAdministrator())
            {
                _logger.LogWarning("Unauthorized attempt to deactivate supplier");
                return Forbid();
            }

            _logger.LogInformation("Deactivating supplier {SupplierId}", id);

            await _supplierService.DeactivateSupplierAsync(id);

            _logger.LogInformation("Supplier {SupplierId} deactivated successfully", id);
            return Ok(new { message = "Supplier deactivated successfully" });
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Supplier not found: {Message}", ex.Message);
            return NotFound(new ProblemDetails
            {
                Title = "Supplier Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating supplier {SupplierId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets payment history for a supplier
    /// </summary>
    /// <param name="id">Supplier ID</param>
    /// <returns>Supplier's payment history</returns>
    /// <response code="200">Payment history retrieved successfully</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="403">Forbidden - only owner or admin can view</response>
    /// <response code="404">Supplier not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id}/payments")]
    [ProducesResponseType(typeof(List<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetSupplierPayments(string id)
    {
        try
        {
            _logger.LogInformation("Retrieving payment history for supplier {SupplierId}", id);

            var supplier = await _supplierService.GetSupplierByIdAsync(id);
            if (supplier == null)
            {
                _logger.LogWarning("Supplier {SupplierId} not found", id);
                return NotFound(new ProblemDetails
                {
                    Title = "Supplier Not Found",
                    Detail = $"Supplier with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // Check access control: Admin or supplier owner
            var userId = GetUserIdFromClaims();
            if (!IsAdministrator() && supplier.UserId != userId)
            {
                _logger.LogWarning("Unauthorized access attempt to supplier payments {SupplierId} by user {UserId}", id, userId);
                return Forbid();
            }

            var paymentHistory = await _supplierService.GetSupplierPaymentHistoryAsync(id);

            _logger.LogInformation("Payment history retrieved for supplier {SupplierId}", id);
            return Ok(paymentHistory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment history for supplier {SupplierId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Checks if the current user is an Administrator
    /// </summary>
    private bool IsAdministrator()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        return role == UserRole.Administrator.ToString();
    }

    /// <summary>
    /// Checks if the current user is a Customer
    /// </summary>
    private bool IsCustomer()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        return role == UserRole.Customer.ToString();
    }

    /// <summary>
    /// Gets the user ID from JWT claims
    /// </summary>
    private string GetUserIdFromClaims()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }
}
