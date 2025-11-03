using BlockchainAidTracker.Services.DTOs.Authentication;
using BlockchainAidTracker.Services.Exceptions;
using BlockchainAidTracker.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlockchainAidTracker.Api.Controllers;

/// <summary>
/// Controller for authentication operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<AuthenticationController> _logger;

    public AuthenticationController(
        IAuthenticationService authenticationService,
        ILogger<AuthenticationController> logger)
    {
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers a new user
    /// </summary>
    /// <param name="request">Registration request containing user details</param>
    /// <returns>Authentication response with tokens</returns>
    /// <response code="200">User successfully registered</response>
    /// <response code="400">Invalid request or business rule violation</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AuthenticationResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            _logger.LogInformation("Registration attempt for username: {Username}", request.Username);
            var response = await _authenticationService.RegisterAsync(request);
            _logger.LogInformation("User {Username} registered successfully", request.Username);
            return Ok(response);
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning("Registration failed for username {Username}: {Message}", request.Username, ex.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Registration Failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for username: {Username}", request.Username);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Authenticates a user and returns JWT tokens
    /// </summary>
    /// <param name="request">Login request with credentials</param>
    /// <returns>Authentication response with tokens</returns>
    /// <response code="200">User successfully authenticated</response>
    /// <response code="401">Invalid credentials</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AuthenticationResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            _logger.LogInformation("Login attempt for user: {UsernameOrEmail}", request.UsernameOrEmail);
            var response = await _authenticationService.LoginAsync(request);
            _logger.LogInformation("User {UsernameOrEmail} logged in successfully", request.UsernameOrEmail);
            return Ok(response);
        }
        catch (UnauthorizedException ex)
        {
            _logger.LogWarning("Login failed for user {UsernameOrEmail}: {Message}", request.UsernameOrEmail, ex.Message);
            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication Failed",
                Detail = ex.Message,
                Status = StatusCodes.Status401Unauthorized
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user: {UsernameOrEmail}", request.UsernameOrEmail);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Refreshes an access token using a refresh token
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <returns>New authentication response with updated tokens</returns>
    /// <response code="200">Token successfully refreshed</response>
    /// <response code="401">Invalid or expired refresh token</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AuthenticationResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            _logger.LogInformation("Token refresh attempt");
            var response = await _authenticationService.RefreshTokenAsync(request);
            _logger.LogInformation("Token refreshed successfully");
            return Ok(response);
        }
        catch (UnauthorizedException ex)
        {
            _logger.LogWarning("Token refresh failed: {Message}", ex.Message);
            return Unauthorized(new ProblemDetails
            {
                Title = "Token Refresh Failed",
                Detail = ex.Message,
                Status = StatusCodes.Status401Unauthorized
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Logs out a user (client-side token invalidation)
    /// </summary>
    /// <returns>Success message</returns>
    /// <response code="200">User successfully logged out</response>
    /// <remarks>
    /// In a JWT-based system, logout is typically handled on the client-side by removing the tokens.
    /// This endpoint is provided for API completeness and can be extended with token blacklisting if needed.
    /// </remarks>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public ActionResult Logout()
    {
        var username = User.Identity?.Name;
        _logger.LogInformation("User {Username} logged out", username ?? "Unknown");
        return Ok(new { message = "Logged out successfully. Please remove tokens from client storage." });
    }

    /// <summary>
    /// Validates the current authentication token
    /// </summary>
    /// <returns>Token validation status</returns>
    /// <response code="200">Token is valid</response>
    /// <response code="401">Token is invalid or expired</response>
    [HttpGet("validate")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult ValidateToken()
    {
        var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value;
        var username = User.Identity?.Name;
        var role = User.FindFirst("role")?.Value;

        _logger.LogInformation("Token validation for user {Username}", username ?? "Unknown");

        return Ok(new
        {
            valid = true,
            userId,
            username,
            role,
            message = "Token is valid"
        });
    }
}
