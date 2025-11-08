using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.Services.DTOs.AuditLog;
using BlockchainAidTracker.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlockchainAidTracker.Api.Controllers;

/// <summary>
/// Controller for audit log operations
/// </summary>
[ApiController]
[Route("api/audit-logs")]
[Produces("application/json")]
[Authorize(Roles = "Administrator")]
public class AuditLogController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuditLogController> _logger;

    public AuditLogController(
        IAuditLogService auditLogService,
        ILogger<AuditLogController> logger)
    {
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets recent audit logs with pagination
    /// </summary>
    /// <param name="pageSize">Number of logs per page (default: 50, max: 100)</param>
    /// <param name="pageNumber">Page number (1-indexed, default: 1)</param>
    /// <returns>List of recent audit logs</returns>
    /// <response code="200">Audit logs retrieved successfully</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="403">Forbidden - Administrator role required</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<AuditLogDto>>> GetRecentLogs(
        [FromQuery] int pageSize = 50,
        [FromQuery] int pageNumber = 1)
    {
        try
        {
            _logger.LogInformation("Retrieving recent audit logs (page {PageNumber}, size {PageSize})", pageNumber, pageSize);

            // Validate pagination parameters
            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Page Size",
                    Detail = "Page size must be between 1 and 100",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            if (pageNumber < 1)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Page Number",
                    Detail = "Page number must be greater than 0",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var logs = await _auditLogService.GetRecentLogsAsync(pageSize, pageNumber);

            _logger.LogInformation("Retrieved {Count} audit logs", logs.Count);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent audit logs");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while retrieving audit logs",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets filtered audit logs
    /// </summary>
    /// <param name="filter">Filter criteria</param>
    /// <returns>List of filtered audit logs</returns>
    /// <response code="200">Audit logs retrieved successfully</response>
    /// <response code="400">Invalid filter parameters</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="403">Forbidden - Administrator role required</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("filter")]
    [ProducesResponseType(typeof(List<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<AuditLogDto>>> GetFilteredLogs([FromBody] AuditLogFilterRequest filter)
    {
        try
        {
            _logger.LogInformation("Retrieving filtered audit logs");

            // Validate pagination parameters
            if (filter.PageSize < 1 || filter.PageSize > 100)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Page Size",
                    Detail = "Page size must be between 1 and 100",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            if (filter.PageNumber < 1)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Page Number",
                    Detail = "Page number must be greater than 0",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var logs = await _auditLogService.GetLogsAsync(filter);

            _logger.LogInformation("Retrieved {Count} filtered audit logs", logs.Count);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving filtered audit logs");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while retrieving audit logs",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets audit logs by category
    /// </summary>
    /// <param name="category">Audit log category</param>
    /// <returns>List of audit logs in the specified category</returns>
    /// <response code="200">Audit logs retrieved successfully</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="403">Forbidden - Administrator role required</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("category/{category}")]
    [ProducesResponseType(typeof(List<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<AuditLogDto>>> GetLogsByCategory(AuditLogCategory category)
    {
        try
        {
            _logger.LogInformation("Retrieving audit logs for category {Category}", category);

            var logs = await _auditLogService.GetLogsByCategoryAsync(category);

            _logger.LogInformation("Retrieved {Count} audit logs for category {Category}", logs.Count, category);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs for category {Category}", category);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while retrieving audit logs",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets audit logs for a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of audit logs for the specified user</returns>
    /// <response code="200">Audit logs retrieved successfully</response>
    /// <response code="400">Invalid user ID</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="403">Forbidden - Administrator role required</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(List<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<AuditLogDto>>> GetLogsByUser(string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid User ID",
                    Detail = "User ID cannot be empty",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            _logger.LogInformation("Retrieving audit logs for user {UserId}", userId);

            var logs = await _auditLogService.GetLogsByUserIdAsync(userId);

            _logger.LogInformation("Retrieved {Count} audit logs for user {UserId}", logs.Count, userId);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while retrieving audit logs",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets audit logs for a specific entity
    /// </summary>
    /// <param name="entityId">Entity ID</param>
    /// <returns>List of audit logs for the specified entity</returns>
    /// <response code="200">Audit logs retrieved successfully</response>
    /// <response code="400">Invalid entity ID</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="403">Forbidden - Administrator role required</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("entity/{entityId}")]
    [ProducesResponseType(typeof(List<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<AuditLogDto>>> GetLogsByEntity(string entityId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(entityId))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Entity ID",
                    Detail = "Entity ID cannot be empty",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            _logger.LogInformation("Retrieving audit logs for entity {EntityId}", entityId);

            var logs = await _auditLogService.GetLogsByEntityIdAsync(entityId);

            _logger.LogInformation("Retrieved {Count} audit logs for entity {EntityId}", logs.Count, entityId);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs for entity {EntityId}", entityId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while retrieving audit logs",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets failed audit logs
    /// </summary>
    /// <returns>List of failed audit logs</returns>
    /// <response code="200">Audit logs retrieved successfully</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="403">Forbidden - Administrator role required</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("failed")]
    [ProducesResponseType(typeof(List<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<AuditLogDto>>> GetFailedLogs()
    {
        try
        {
            _logger.LogInformation("Retrieving failed audit logs");

            var logs = await _auditLogService.GetFailedLogsAsync();

            _logger.LogInformation("Retrieved {Count} failed audit logs", logs.Count);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving failed audit logs");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while retrieving audit logs",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets count of audit logs by category
    /// </summary>
    /// <param name="category">Audit log category</param>
    /// <returns>Count of audit logs in the specified category</returns>
    /// <response code="200">Count retrieved successfully</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="403">Forbidden - Administrator role required</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("category/{category}/count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<int>> GetCountByCategory(AuditLogCategory category)
    {
        try
        {
            _logger.LogInformation("Retrieving count of audit logs for category {Category}", category);

            var count = await _auditLogService.GetCountByCategoryAsync(category);

            _logger.LogInformation("Found {Count} audit logs for category {Category}", count, category);
            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving count for category {Category}", category);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while retrieving audit log count",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }
}
