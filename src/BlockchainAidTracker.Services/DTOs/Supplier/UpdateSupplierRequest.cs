namespace BlockchainAidTracker.Services.DTOs.Supplier;

/// <summary>
/// Request DTO for updating a supplier
/// </summary>
public class UpdateSupplierRequest
{
    /// <summary>
    /// Contact email address (can be updated)
    /// </summary>
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Contact phone number (can be updated)
    /// </summary>
    public string? ContactPhone { get; set; }

    /// <summary>
    /// Bank account details (can be updated and will be encrypted)
    /// </summary>
    public string? BankDetails { get; set; }

    /// <summary>
    /// Minimum shipment value to trigger automatic payment (can be updated)
    /// </summary>
    public decimal? PaymentThreshold { get; set; }
}
