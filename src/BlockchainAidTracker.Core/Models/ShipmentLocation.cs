namespace BlockchainAidTracker.Core.Models;

/// <summary>
/// Represents the current or historical location of a shipment during delivery
/// </summary>
public class ShipmentLocation
{
    /// <summary>
    /// Unique identifier for the location record
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Foreign key to the Shipment entity
    /// </summary>
    public string ShipmentId { get; set; }

    /// <summary>
    /// Latitude coordinate (-90 to 90)
    /// </summary>
    public decimal Latitude { get; set; }

    /// <summary>
    /// Longitude coordinate (-180 to 180)
    /// </summary>
    public decimal Longitude { get; set; }

    /// <summary>
    /// Human-readable location name or address
    /// </summary>
    public string LocationName { get; set; }

    /// <summary>
    /// Timestamp when this location was recorded (UTC)
    /// </summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>
    /// GPS accuracy in meters (optional)
    /// </summary>
    public decimal? GpsAccuracy { get; set; }

    /// <summary>
    /// Foreign key to the User who updated this location
    /// </summary>
    public string UpdatedByUserId { get; set; }

    /// <summary>
    /// Default constructor
    /// </summary>
    public ShipmentLocation()
    {
        Id = Guid.NewGuid().ToString();
        LocationName = string.Empty;
        CreatedTimestamp = DateTime.UtcNow;
        UpdatedByUserId = string.Empty;
    }

    /// <summary>
    /// Parameterized constructor for creating a location record
    /// </summary>
    public ShipmentLocation(
        string shipmentId,
        decimal latitude,
        decimal longitude,
        string locationName,
        string updatedByUserId,
        decimal? gpsAccuracy = null)
    {
        Id = Guid.NewGuid().ToString();
        ShipmentId = shipmentId ?? throw new ArgumentNullException(nameof(shipmentId));
        Latitude = latitude;
        Longitude = longitude;
        LocationName = locationName ?? throw new ArgumentNullException(nameof(locationName));
        UpdatedByUserId = updatedByUserId ?? throw new ArgumentNullException(nameof(updatedByUserId));
        GpsAccuracy = gpsAccuracy;
        CreatedTimestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Validates that coordinates are within valid ranges
    /// </summary>
    /// <returns>True if coordinates are valid, false otherwise</returns>
    public bool IsValidCoordinates()
    {
        return Latitude >= -90 && Latitude <= 90 && Longitude >= -180 && Longitude <= 180;
    }
}
