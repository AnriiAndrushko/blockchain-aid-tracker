using System.ComponentModel.DataAnnotations;

namespace BlockchainAidTracker.Services.DTOs.Validator;

/// <summary>
/// Request DTO for creating/registering a new validator
/// </summary>
public class CreateValidatorRequest
{
    /// <summary>
    /// Validator name (must be unique)
    /// </summary>
    [Required(ErrorMessage = "Validator name is required")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Validator name must be between 3 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Password for encrypting the validator's private key
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Priority in validator set (lower number = higher priority)
    /// </summary>
    [Range(0, 1000, ErrorMessage = "Priority must be between 0 and 1000")]
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Network address/endpoint (optional)
    /// </summary>
    [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
    public string? Address { get; set; }

    /// <summary>
    /// Description (optional)
    /// </summary>
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }
}
