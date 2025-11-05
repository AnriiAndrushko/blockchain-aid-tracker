using System.ComponentModel.DataAnnotations;

namespace BlockchainAidTracker.Services.DTOs.Validator;

/// <summary>
/// Request DTO for updating validator details
/// </summary>
public class UpdateValidatorRequest
{
    /// <summary>
    /// New priority (optional)
    /// </summary>
    [Range(0, 1000, ErrorMessage = "Priority must be between 0 and 1000")]
    public int? Priority { get; set; }

    /// <summary>
    /// New network address (optional)
    /// </summary>
    [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
    public string? Address { get; set; }

    /// <summary>
    /// New description (optional)
    /// </summary>
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }
}
