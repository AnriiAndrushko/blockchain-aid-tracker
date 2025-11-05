using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.Services.DTOs.Validator;
using BlockchainAidTracker.Services.Exceptions;
using BlockchainAidTracker.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlockchainAidTracker.Api.Controllers;

/// <summary>
/// Controller for validator management operations
/// </summary>
[ApiController]
[Route("api/validators")]
[Produces("application/json")]
[Authorize]
public class ValidatorController : ControllerBase
{
    private readonly IValidatorService _validatorService;
    private readonly ILogger<ValidatorController> _logger;

    public ValidatorController(
        IValidatorService validatorService,
        ILogger<ValidatorController> logger)
    {
        _validatorService = validatorService ?? throw new ArgumentNullException(nameof(validatorService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers a new validator in the system (Admin only)
    /// </summary>
    /// <param name="request">Validator registration request</param>
    /// <returns>Newly created validator</returns>
    /// <response code="201">Validator registered successfully</response>
    /// <response code="400">Invalid request or business rule violation</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="403">Forbidden - requires Administrator role</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [ProducesResponseType(typeof(ValidatorDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ValidatorDto>> RegisterValidator([FromBody] CreateValidatorRequest request)
    {
        try
        {
            // Check if user is Administrator
            if (!IsAdministrator())
            {
                _logger.LogWarning("Unauthorized attempt to register validator by non-admin user");
                return Forbid();
            }

            _logger.LogInformation("Registering new validator: {ValidatorName}", request.Name);

            var validator = await _validatorService.RegisterValidatorAsync(request);

            _logger.LogInformation("Validator registered successfully: {ValidatorId}, {ValidatorName}",
                validator.Id, validator.Name);

            return CreatedAtAction(
                nameof(GetValidatorById),
                new { id = validator.Id },
                validator);
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning("Validator registration failed: {Message}", ex.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Validator Registration Failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering validator");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets all validators (Admin/Validator roles)
    /// </summary>
    /// <param name="activeOnly">If true, returns only active validators</param>
    /// <returns>List of validators</returns>
    /// <response code="200">Validators retrieved successfully</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="403">Forbidden - requires Administrator or Validator role</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<ValidatorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<ValidatorDto>>> GetAllValidators([FromQuery] bool activeOnly = false)
    {
        try
        {
            // Check if user is Administrator or Validator
            if (!IsAdministratorOrValidator())
            {
                _logger.LogWarning("Unauthorized attempt to list validators");
                return Forbid();
            }

            _logger.LogInformation("Retrieving validators (activeOnly: {ActiveOnly})", activeOnly);

            var validators = await _validatorService.GetAllValidatorsAsync(activeOnly);

            _logger.LogInformation("Retrieved {Count} validators", validators.Count);
            return Ok(validators);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving validators");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets a validator by ID (Admin/Validator roles)
    /// </summary>
    /// <param name="id">Validator ID</param>
    /// <returns>Validator details</returns>
    /// <response code="200">Validator retrieved successfully</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="403">Forbidden - requires Administrator or Validator role</response>
    /// <response code="404">Validator not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ValidatorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ValidatorDto>> GetValidatorById(string id)
    {
        try
        {
            // Check if user is Administrator or Validator
            if (!IsAdministratorOrValidator())
            {
                _logger.LogWarning("Unauthorized attempt to get validator");
                return Forbid();
            }

            _logger.LogInformation("Retrieving validator {ValidatorId}", id);

            var validator = await _validatorService.GetValidatorByIdAsync(id);

            if (validator == null)
            {
                _logger.LogWarning("Validator {ValidatorId} not found", id);
                return NotFound(new ProblemDetails
                {
                    Title = "Validator Not Found",
                    Detail = $"Validator with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            _logger.LogInformation("Validator {ValidatorId} retrieved successfully", id);
            return Ok(validator);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving validator {ValidatorId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Updates a validator (Admin only)
    /// </summary>
    /// <param name="id">Validator ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated validator</returns>
    /// <response code="200">Validator updated successfully</response>
    /// <response code="400">Invalid request or business rule violation</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="403">Forbidden - requires Administrator role</response>
    /// <response code="404">Validator not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ValidatorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ValidatorDto>> UpdateValidator(string id, [FromBody] UpdateValidatorRequest request)
    {
        try
        {
            // Check if user is Administrator
            if (!IsAdministrator())
            {
                _logger.LogWarning("Unauthorized attempt to update validator");
                return Forbid();
            }

            _logger.LogInformation("Updating validator {ValidatorId}", id);

            var validator = await _validatorService.UpdateValidatorAsync(id, request);

            _logger.LogInformation("Validator {ValidatorId} updated successfully", id);
            return Ok(validator);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Validator not found: {Message}", ex.Message);
            return NotFound(new ProblemDetails
            {
                Title = "Validator Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning("Validator update failed: {Message}", ex.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Validator Update Failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating validator {ValidatorId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Activates a validator (Admin only)
    /// </summary>
    /// <param name="id">Validator ID</param>
    /// <returns>Success status</returns>
    /// <response code="200">Validator activated successfully</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="403">Forbidden - requires Administrator role</response>
    /// <response code="404">Validator not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("{id}/activate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> ActivateValidator(string id)
    {
        try
        {
            // Check if user is Administrator
            if (!IsAdministrator())
            {
                _logger.LogWarning("Unauthorized attempt to activate validator");
                return Forbid();
            }

            _logger.LogInformation("Activating validator {ValidatorId}", id);

            await _validatorService.ActivateValidatorAsync(id);

            _logger.LogInformation("Validator {ValidatorId} activated successfully", id);
            return Ok(new { message = "Validator activated successfully" });
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Validator not found: {Message}", ex.Message);
            return NotFound(new ProblemDetails
            {
                Title = "Validator Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating validator {ValidatorId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Deactivates a validator (Admin only)
    /// </summary>
    /// <param name="id">Validator ID</param>
    /// <returns>Success status</returns>
    /// <response code="200">Validator deactivated successfully</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="403">Forbidden - requires Administrator role</response>
    /// <response code="404">Validator not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("{id}/deactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeactivateValidator(string id)
    {
        try
        {
            // Check if user is Administrator
            if (!IsAdministrator())
            {
                _logger.LogWarning("Unauthorized attempt to deactivate validator");
                return Forbid();
            }

            _logger.LogInformation("Deactivating validator {ValidatorId}", id);

            await _validatorService.DeactivateValidatorAsync(id);

            _logger.LogInformation("Validator {ValidatorId} deactivated successfully", id);
            return Ok(new { message = "Validator deactivated successfully" });
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Validator not found: {Message}", ex.Message);
            return NotFound(new ProblemDetails
            {
                Title = "Validator Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating validator {ValidatorId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets the next validator for block creation (Internal use)
    /// </summary>
    /// <returns>Next validator for block creation</returns>
    /// <response code="200">Next validator retrieved successfully</response>
    /// <response code="401">Unauthorized - requires authentication</response>
    /// <response code="404">No active validators available</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("next")]
    [ProducesResponseType(typeof(ValidatorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ValidatorDto>> GetNextValidatorForBlockCreation()
    {
        try
        {
            _logger.LogInformation("Retrieving next validator for block creation");

            var validator = await _validatorService.GetNextValidatorForBlockCreationAsync();

            if (validator == null)
            {
                _logger.LogWarning("No active validators available for block creation");
                return NotFound(new ProblemDetails
                {
                    Title = "No Active Validators",
                    Detail = "There are no active validators available for block creation",
                    Status = StatusCodes.Status404NotFound
                });
            }

            _logger.LogInformation("Next validator for block creation: {ValidatorId}, {ValidatorName}",
                validator.Id, validator.Name);
            return Ok(validator);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving next validator for block creation");
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
    /// Checks if the current user is an Administrator or Validator
    /// </summary>
    private bool IsAdministratorOrValidator()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        return role == UserRole.Administrator.ToString() || role == UserRole.Validator.ToString();
    }
}
