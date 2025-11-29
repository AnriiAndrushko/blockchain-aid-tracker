using System.ComponentModel.DataAnnotations;

namespace BlockchainAidTracker.Services.DTOs.LogisticsPartner;

/// <summary>
/// Request DTO for updating shipment location
/// </summary>
public class UpdateLocationRequest
{
    /// <summary>
    /// Latitude coordinate (-90 to 90)
    /// </summary>
    [Required]
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
    public decimal Latitude { get; set; }

    /// <summary>
    /// Longitude coordinate (-180 to 180)
    /// </summary>
    [Required]
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
    public decimal Longitude { get; set; }

    /// <summary>
    /// Location name or address
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "Location name must be between 1 and 500 characters")]
    public string LocationName { get; set; }

    /// <summary>
    /// GPS accuracy in meters (optional)
    /// </summary>
    [Range(0, 10000, ErrorMessage = "GPS accuracy must be between 0 and 10000")]
    public decimal? GpsAccuracy { get; set; }
}
