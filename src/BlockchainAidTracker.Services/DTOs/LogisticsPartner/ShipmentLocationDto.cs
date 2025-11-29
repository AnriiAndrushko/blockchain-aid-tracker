namespace BlockchainAidTracker.Services.DTOs.LogisticsPartner;

/// <summary>
/// DTO for shipment location information
/// </summary>
public class ShipmentLocationDto
{
    /// <summary>
    /// Location record ID
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Shipment ID
    /// </summary>
    public string ShipmentId { get; set; }

    /// <summary>
    /// Latitude coordinate
    /// </summary>
    public decimal Latitude { get; set; }

    /// <summary>
    /// Longitude coordinate
    /// </summary>
    public decimal Longitude { get; set; }

    /// <summary>
    /// Human-readable location name
    /// </summary>
    public string LocationName { get; set; }

    /// <summary>
    /// Timestamp when recorded
    /// </summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>
    /// GPS accuracy in meters
    /// </summary>
    public decimal? GpsAccuracy { get; set; }

    /// <summary>
    /// User ID who updated this location
    /// </summary>
    public string UpdatedByUserId { get; set; }
}
