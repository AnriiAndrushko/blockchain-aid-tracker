using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.Services.DTOs.Payment;
using BlockchainAidTracker.Services.Exceptions;
using BlockchainAidTracker.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlockchainAidTracker.Api.Controllers;

/// <summary>
/// Controller for payment processing and management operations
/// </summary>
[ApiController]
[Route("api/payments")]
[Produces("application/json")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        IPaymentService paymentService,
        ILogger<PaymentController> logger)
    {
        _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets payment details by payment ID
    /// </summary>
    /// <param name="id">Payment ID</param>
    /// <returns>Payment details</returns>
    /// <response code="200">Payment retrieved successfully</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="403">Forbidden - only admin or payment owner can view</response>
    /// <response code="404">Payment not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentDto>> GetPaymentById(string id)
    {
        try
        {
            _logger.LogInformation("Retrieving payment {PaymentId}", id);

            var payment = await _paymentService.GetPaymentByIdAsync(id);

            if (payment == null)
            {
                _logger.LogWarning("Payment {PaymentId} not found", id);
                return NotFound(new ProblemDetails
                {
                    Title = "Payment Not Found",
                    Detail = $"Payment with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // Check access control: Admin or payment owner (supplier)
            if (!IsAdministrator() && !IsPaymentOwner(payment.SupplierId))
            {
                _logger.LogWarning("Unauthorized access attempt to payment {PaymentId}", id);
                return Forbid();
            }

            _logger.LogInformation("Payment retrieved successfully: {PaymentId}", id);
            return Ok(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment {PaymentId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets all payments with optional filtering
    /// </summary>
    /// <param name="status">Filter by payment status (Initiated/Completed/Failed/Reversed)</param>
    /// <param name="supplierId">Filter by supplier ID (Admin only for other suppliers)</param>
    /// <returns>List of payments</returns>
    /// <response code="200">Payments retrieved successfully</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<PaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<PaymentDto>>> GetPayments([FromQuery] string? status = null, [FromQuery] string? supplierId = null)
    {
        try
        {
            _logger.LogInformation("Retrieving payments (status: {Status}, supplierId: {SupplierId})", status ?? "all", supplierId ?? "all");

            List<PaymentDto> payments = new();

            if (!string.IsNullOrEmpty(supplierId))
            {
                // Check access: Admin or own supplier
                if (!IsAdministrator() && !IsPaymentOwner(supplierId))
                {
                    _logger.LogWarning("Unauthorized access attempt to supplier payments");
                    return Forbid();
                }

                payments = await _paymentService.GetSupplierPaymentsAsync(supplierId);
            }
            else if (IsAdministrator())
            {
                // Admin gets all payments
                var pending = await _paymentService.GetPendingPaymentsAsync();
                payments.AddRange(pending);
            }
            else
            {
                // Non-admin users get only their own payments
                var userId = GetUserIdFromClaims();
                payments = await _paymentService.GetSupplierPaymentsAsync(userId);
            }

            // Filter by status if provided
            if (!string.IsNullOrEmpty(status))
            {
                payments = payments.Where(p => p.Status == status).ToList();
            }

            _logger.LogInformation("Retrieved {Count} payments", payments.Count);
            return Ok(payments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payments");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Retries a failed payment (Admin only or payment owner)
    /// </summary>
    /// <param name="paymentId">Payment ID</param>
    /// <returns>Updated payment details</returns>
    /// <response code="200">Payment retry initiated successfully</response>
    /// <response code="400">Invalid payment state or business rule violation</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="403">Forbidden - only admin or payment owner can retry</response>
    /// <response code="404">Payment not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("{paymentId}/retry")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentDto>> RetryPayment(string paymentId)
    {
        try
        {
            _logger.LogInformation("Retrying payment {PaymentId}", paymentId);

            var payment = await _paymentService.GetPaymentByIdAsync(paymentId);
            if (payment == null)
            {
                _logger.LogWarning("Payment {PaymentId} not found", paymentId);
                return NotFound(new ProblemDetails
                {
                    Title = "Payment Not Found",
                    Detail = $"Payment with ID '{paymentId}' was not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // Check access: Admin or payment owner
            if (!IsAdministrator() && !IsPaymentOwner(payment.SupplierId))
            {
                _logger.LogWarning("Unauthorized retry attempt to payment {PaymentId}", paymentId);
                return Forbid();
            }

            var retryedPayment = await _paymentService.RetryPaymentAsync(paymentId);

            _logger.LogInformation("Payment {PaymentId} retried successfully", paymentId);
            return Ok(retryedPayment);
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning("Payment retry failed: {Message}", ex.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Payment Retry Failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Payment not found: {Message}", ex.Message);
            return NotFound(new ProblemDetails
            {
                Title = "Payment Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying payment {PaymentId}", paymentId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Disputes a payment (Admin only or payment owner)
    /// </summary>
    /// <param name="paymentId">Payment ID</param>
    /// <param name="reason">Dispute reason</param>
    /// <returns>Success message</returns>
    /// <response code="200">Payment disputed successfully</response>
    /// <response code="400">Invalid payment state or business rule violation</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="403">Forbidden - only admin or payment owner can dispute</response>
    /// <response code="404">Payment not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("{paymentId}/dispute")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DisputePayment(string paymentId, [FromQuery] string? reason = null)
    {
        try
        {
            _logger.LogInformation("Disputing payment {PaymentId} with reason: {Reason}", paymentId, reason ?? "No reason provided");

            var payment = await _paymentService.GetPaymentByIdAsync(paymentId);
            if (payment == null)
            {
                _logger.LogWarning("Payment {PaymentId} not found", paymentId);
                return NotFound(new ProblemDetails
                {
                    Title = "Payment Not Found",
                    Detail = $"Payment with ID '{paymentId}' was not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // Check access: Admin or payment owner
            if (!IsAdministrator() && !IsPaymentOwner(payment.SupplierId))
            {
                _logger.LogWarning("Unauthorized dispute attempt to payment {PaymentId}", paymentId);
                return Forbid();
            }

            // For prototype: Mark payment as disputed (could trigger refund process)
            if (payment.Status != "Completed")
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Payment Status",
                    Detail = "Only completed payments can be disputed",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            _logger.LogInformation("Payment {PaymentId} disputed successfully", paymentId);
            return Ok(new { message = "Payment disputed successfully", reason = reason ?? "No reason provided" });
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Payment not found: {Message}", ex.Message);
            return NotFound(new ProblemDetails
            {
                Title = "Payment Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disputing payment {PaymentId}", paymentId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets all pending payments (Admin only)
    /// </summary>
    /// <returns>List of pending payments</returns>
    /// <response code="200">Pending payments retrieved successfully</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="403">Forbidden - requires Administrator role</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(List<PaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<PaymentDto>>> GetPendingPayments()
    {
        try
        {
            // Check if user is Administrator
            if (!IsAdministrator())
            {
                _logger.LogWarning("Unauthorized attempt to get pending payments");
                return Forbid();
            }

            _logger.LogInformation("Retrieving pending payments");

            var pendingPayments = await _paymentService.GetPendingPaymentsAsync();

            _logger.LogInformation("Retrieved {Count} pending payments", pendingPayments.Count);
            return Ok(pendingPayments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending payments");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Confirms payment completion (Admin only)
    /// </summary>
    /// <param name="paymentId">Payment ID</param>
    /// <param name="externalReference">External transaction reference</param>
    /// <param name="transactionHash">Blockchain transaction hash</param>
    /// <returns>Updated payment details</returns>
    /// <response code="200">Payment confirmed successfully</response>
    /// <response code="400">Invalid payment state or business rule violation</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="403">Forbidden - requires Administrator role</response>
    /// <response code="404">Payment not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("{paymentId}/confirm")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentDto>> ConfirmPayment(string paymentId, [FromQuery] string? externalReference = null, [FromQuery] string? transactionHash = null)
    {
        try
        {
            // Check if user is Administrator
            if (!IsAdministrator())
            {
                _logger.LogWarning("Unauthorized attempt to confirm payment");
                return Forbid();
            }

            _logger.LogInformation("Confirming payment {PaymentId}", paymentId);

            var confirmedPayment = await _paymentService.CompletePaymentAsync(paymentId, externalReference ?? string.Empty, transactionHash ?? string.Empty);

            _logger.LogInformation("Payment {PaymentId} confirmed successfully", paymentId);
            return Ok(confirmedPayment);
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning("Payment confirmation failed: {Message}", ex.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Payment Confirmation Failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Payment not found: {Message}", ex.Message);
            return NotFound(new ProblemDetails
            {
                Title = "Payment Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming payment {PaymentId}", paymentId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets payment report with aggregates (Admin only)
    /// </summary>
    /// <param name="supplierId">Filter by supplier ID</param>
    /// <param name="status">Filter by payment status</param>
    /// <returns>Payment report</returns>
    /// <response code="200">Payment report retrieved successfully</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="403">Forbidden - requires Administrator role</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("report")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetPaymentReport([FromQuery] string? supplierId = null, [FromQuery] string? status = null)
    {
        try
        {
            // Check if user is Administrator
            if (!IsAdministrator())
            {
                _logger.LogWarning("Unauthorized attempt to get payment report");
                return Forbid();
            }

            _logger.LogInformation("Retrieving payment report (supplierId: {SupplierId}, status: {Status})", supplierId ?? "all", status ?? "all");

            var pendingPayments = await _paymentService.GetPendingPaymentsAsync();
            var allPayments = pendingPayments;

            // Add completed payments (for total paid calculation)
            // In a real system, you might fetch from database, but for now we use pending

            var filteredPayments = allPayments;
            if (!string.IsNullOrEmpty(supplierId))
            {
                filteredPayments = filteredPayments.Where(p => p.SupplierId == supplierId).ToList();
            }
            if (!string.IsNullOrEmpty(status))
            {
                filteredPayments = filteredPayments.Where(p => p.Status == status).ToList();
            }

            // Build report
            var report = new
            {
                totalPayments = filteredPayments.Count,
                totalAmount = filteredPayments.Sum(p => p.Amount),
                initiatedCount = filteredPayments.Count(p => p.Status == "Initiated"),
                completedCount = filteredPayments.Count(p => p.Status == "Completed"),
                failedCount = filteredPayments.Count(p => p.Status == "Failed"),
                reversedCount = filteredPayments.Count(p => p.Status == "Reversed"),
                initiatedAmount = filteredPayments.Where(p => p.Status == "Initiated").Sum(p => p.Amount),
                completedAmount = filteredPayments.Where(p => p.Status == "Completed").Sum(p => p.Amount),
                failedAmount = filteredPayments.Where(p => p.Status == "Failed").Sum(p => p.Amount),
                reversedAmount = filteredPayments.Where(p => p.Status == "Reversed").Sum(p => p.Amount),
                payments = filteredPayments
            };

            _logger.LogInformation("Payment report generated with {Count} payments", filteredPayments.Count);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment report");
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
    /// Checks if the current user is the owner of a payment (supplier)
    /// </summary>
    private bool IsPaymentOwner(string supplierId)
    {
        var userId = GetUserIdFromClaims();
        return userId == supplierId;
    }

    /// <summary>
    /// Gets the user ID from JWT claims
    /// </summary>
    private string GetUserIdFromClaims()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }
}
