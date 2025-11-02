namespace BlockchainAidTracker.Core.Models;

/// <summary>
/// Represents an individual item within a shipment
/// </summary>
public class ShipmentItem
{
    /// <summary>
    /// Unique identifier for the shipment item
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Description of the item (e.g., "Medical Supplies - Bandages")
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Quantity of this item in the shipment
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Unit of measurement for the quantity (e.g., "boxes", "kg", "units")
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// Category of the item (e.g., "Medical", "Food", "Shelter", "Water")
    /// </summary>
    public string Category { get; set; }

    /// <summary>
    /// Estimated value in USD (optional, for transparency)
    /// </summary>
    public decimal? EstimatedValue { get; set; }

    /// <summary>
    /// Default constructor - initializes a new shipment item with a unique ID
    /// </summary>
    public ShipmentItem()
    {
        Id = Guid.NewGuid().ToString();
        Description = string.Empty;
        Unit = string.Empty;
        Category = string.Empty;
    }

    /// <summary>
    /// Parameterized constructor for creating a shipment item with specific details
    /// </summary>
    /// <param name="description">Item description</param>
    /// <param name="quantity">Quantity of items</param>
    /// <param name="unit">Unit of measurement</param>
    /// <param name="category">Item category</param>
    /// <param name="estimatedValue">Optional estimated value</param>
    public ShipmentItem(string description, int quantity, string unit, string category, decimal? estimatedValue = null)
    {
        Id = Guid.NewGuid().ToString();
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Quantity = quantity > 0 ? quantity : throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));
        Unit = unit ?? throw new ArgumentNullException(nameof(unit));
        Category = category ?? throw new ArgumentNullException(nameof(category));
        EstimatedValue = estimatedValue;
    }
}
