namespace BlockchainAidTracker.Core.Models;

/// <summary>
/// Represents a humanitarian aid shipment in the supply chain
/// </summary>
public class Shipment
{
    /// <summary>
    /// Unique identifier for the shipment
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// List of items included in this shipment
    /// </summary>
    public List<ShipmentItem> Items { get; set; }

    /// <summary>
    /// Origin point/location of the shipment
    /// </summary>
    public string Origin { get; set; }

    /// <summary>
    /// Destination point/location of the shipment
    /// </summary>
    public string Destination { get; set; }

    /// <summary>
    /// Expected delivery timeframe (e.g., "2024-03-15 to 2024-03-20")
    /// </summary>
    public string ExpectedDeliveryTimeframe { get; set; }

    /// <summary>
    /// Public key or identifier of the assigned recipient
    /// </summary>
    public string AssignedRecipient { get; set; }

    /// <summary>
    /// Current status of the shipment
    /// </summary>
    public ShipmentStatus Status { get; set; }

    /// <summary>
    /// QR code data for tracking and verification
    /// </summary>
    public string QrCodeData { get; set; }

    /// <summary>
    /// Timestamp when the shipment was created (UTC)
    /// </summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>
    /// Timestamp when the shipment was last updated (UTC)
    /// </summary>
    public DateTime UpdatedTimestamp { get; set; }

    /// <summary>
    /// Actual delivery date when shipment was confirmed delivered (UTC)
    /// </summary>
    public DateTime? ActualDeliveryDate { get; set; }

    /// <summary>
    /// Public key of the coordinator who created the shipment
    /// </summary>
    public string CoordinatorPublicKey { get; set; }

    /// <summary>
    /// Public key of the donor who funded the shipment (optional)
    /// </summary>
    public string? DonorPublicKey { get; set; }

    /// <summary>
    /// Additional notes or comments about the shipment
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Total estimated value of the shipment in USD
    /// </summary>
    public decimal TotalEstimatedValue { get; set; }

    /// <summary>
    /// Default constructor - initializes a new shipment with default values
    /// </summary>
    public Shipment()
    {
        Id = Guid.NewGuid().ToString();
        Items = new List<ShipmentItem>();
        Origin = string.Empty;
        Destination = string.Empty;
        ExpectedDeliveryTimeframe = string.Empty;
        AssignedRecipient = string.Empty;
        Status = ShipmentStatus.Created;
        QrCodeData = string.Empty;
        CreatedTimestamp = DateTime.UtcNow;
        UpdatedTimestamp = DateTime.UtcNow;
        CoordinatorPublicKey = string.Empty;
        TotalEstimatedValue = 0;
    }

    /// <summary>
    /// Parameterized constructor for creating a shipment with specific details
    /// </summary>
    /// <param name="items">List of shipment items</param>
    /// <param name="origin">Origin location</param>
    /// <param name="destination">Destination location</param>
    /// <param name="expectedDeliveryTimeframe">Expected delivery timeframe</param>
    /// <param name="assignedRecipient">Recipient identifier</param>
    /// <param name="coordinatorPublicKey">Coordinator's public key</param>
    /// <param name="donorPublicKey">Optional donor's public key</param>
    /// <param name="notes">Optional notes</param>
    public Shipment(
        List<ShipmentItem> items,
        string origin,
        string destination,
        string expectedDeliveryTimeframe,
        string assignedRecipient,
        string coordinatorPublicKey,
        string? donorPublicKey = null,
        string? notes = null)
    {
        Id = Guid.NewGuid().ToString();
        Items = items ?? throw new ArgumentNullException(nameof(items));
        Origin = origin ?? throw new ArgumentNullException(nameof(origin));
        Destination = destination ?? throw new ArgumentNullException(nameof(destination));
        ExpectedDeliveryTimeframe = expectedDeliveryTimeframe ?? throw new ArgumentNullException(nameof(expectedDeliveryTimeframe));
        AssignedRecipient = assignedRecipient ?? throw new ArgumentNullException(nameof(assignedRecipient));
        CoordinatorPublicKey = coordinatorPublicKey ?? throw new ArgumentNullException(nameof(coordinatorPublicKey));
        DonorPublicKey = donorPublicKey;
        Notes = notes;

        Status = ShipmentStatus.Created;
        CreatedTimestamp = DateTime.UtcNow;
        UpdatedTimestamp = DateTime.UtcNow;
        QrCodeData = GenerateQrCodeData();
        TotalEstimatedValue = CalculateTotalValue();
    }

    /// <summary>
    /// Updates the status of the shipment and refreshes the timestamp
    /// </summary>
    /// <param name="newStatus">New shipment status</param>
    public void UpdateStatus(ShipmentStatus newStatus)
    {
        Status = newStatus;
        UpdatedTimestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds an item to the shipment and recalculates total value
    /// </summary>
    /// <param name="item">Shipment item to add</param>
    public void AddItem(ShipmentItem item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        Items.Add(item);
        TotalEstimatedValue = CalculateTotalValue();
        UpdatedTimestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Removes an item from the shipment and recalculates total value
    /// </summary>
    /// <param name="itemId">ID of the item to remove</param>
    /// <returns>True if item was removed, false if not found</returns>
    public bool RemoveItem(string itemId)
    {
        var item = Items.FirstOrDefault(i => i.Id == itemId);
        if (item == null)
            return false;

        Items.Remove(item);
        TotalEstimatedValue = CalculateTotalValue();
        UpdatedTimestamp = DateTime.UtcNow;
        return true;
    }

    /// <summary>
    /// Calculates the total estimated value of all items in the shipment
    /// </summary>
    /// <returns>Total estimated value in USD</returns>
    public decimal CalculateTotalValue()
    {
        return Items.Sum(item => item.EstimatedValue ?? 0);
    }

    /// <summary>
    /// Generates QR code data for the shipment (format: SHIPMENT-{ID}-{TIMESTAMP})
    /// </summary>
    /// <returns>QR code data string</returns>
    private string GenerateQrCodeData()
    {
        return $"SHIPMENT-{Id}-{CreatedTimestamp:yyyyMMddHHmmss}";
    }

    /// <summary>
    /// Gets a summary description of the shipment
    /// </summary>
    /// <returns>Shipment summary string</returns>
    public string GetSummary()
    {
        var itemCount = Items.Count;
        var itemsText = itemCount == 1 ? "1 item" : $"{itemCount} items";
        return $"Shipment {Id}: {itemsText} from {Origin} to {Destination} - Status: {Status}";
    }

    /// <summary>
    /// Validates that the shipment can transition to a new status
    /// </summary>
    /// <param name="newStatus">Target status</param>
    /// <returns>True if transition is valid, false otherwise</returns>
    public bool CanTransitionTo(ShipmentStatus newStatus)
    {
        // Define valid state transitions
        return Status switch
        {
            ShipmentStatus.Created => newStatus == ShipmentStatus.Validated,
            ShipmentStatus.Validated => newStatus == ShipmentStatus.InTransit,
            ShipmentStatus.InTransit => newStatus == ShipmentStatus.Delivered,
            ShipmentStatus.Delivered => newStatus == ShipmentStatus.Confirmed,
            ShipmentStatus.Confirmed => false, // Terminal state
            _ => false
        };
    }

    /// <summary>
    /// Confirms the delivery of the shipment and sets the actual delivery date
    /// </summary>
    public void ConfirmDelivery()
    {
        Status = ShipmentStatus.Confirmed;
        ActualDeliveryDate = DateTime.UtcNow;
        UpdatedTimestamp = DateTime.UtcNow;
    }
}
