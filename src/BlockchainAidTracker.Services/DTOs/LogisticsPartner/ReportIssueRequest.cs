using System.ComponentModel.DataAnnotations;

namespace BlockchainAidTracker.Services.DTOs.LogisticsPartner;

/// <summary>
/// Enumeration of delivery issue types
/// </summary>
public enum IssueType
{
    Delay = 0,
    Damage = 1,
    Lost = 2,
    Other = 3
}

/// <summary>
/// Request DTO for reporting delivery issues
/// </summary>
public class ReportIssueRequest
{
    /// <summary>
    /// Type of issue
    /// </summary>
    [Required]
    public IssueType IssueType { get; set; }

    /// <summary>
    /// Detailed description of the issue
    /// </summary>
    [Required]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 2000 characters")]
    public string Description { get; set; }

    /// <summary>
    /// Priority level of the issue
    /// </summary>
    [Required]
    [Range(0, 2, ErrorMessage = "Priority must be Low (0), Medium (1), or High (2)")]
    public int Priority { get; set; } // 0 = Low, 1 = Medium, 2 = High
}
