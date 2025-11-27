namespace BlockchainAidTracker.Services.DTOs.Supplier;

/// <summary>
/// Request DTO for creating a new supplier
/// </summary>
public class CreateSupplierRequest
{
    /// <summary>
    /// Legal company name of the supplier
    /// </summary>
    public required string CompanyName { get; set; }

    /// <summary>
    /// Company registration ID
    /// </summary>
    public required string RegistrationId { get; set; }

    /// <summary>
    /// Contact email address
    /// </summary>
    public required string ContactEmail { get; set; }

    /// <summary>
    /// Contact phone number
    /// </summary>
    public required string ContactPhone { get; set; }

    /// <summary>
    /// Business category/type (Food, Medicine, Supplies, etc.)
    /// </summary>
    public required string BusinessCategory { get; set; }

    /// <summary>
    /// Bank account details (IBAN/Swift code) - will be encrypted before storage
    /// </summary>
    public required string BankDetails { get; set; }

    /// <summary>
    /// Minimum shipment value (in USD) to trigger automatic payment
    /// </summary>
    public decimal PaymentThreshold { get; set; } = 1000m;

    /// <summary>
    /// Tax ID for the supplier
    /// </summary>
    public required string TaxId { get; set; }
}
