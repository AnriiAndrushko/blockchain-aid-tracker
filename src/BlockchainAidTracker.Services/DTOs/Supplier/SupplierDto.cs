using BlockchainAidTracker.Core.Models;

namespace BlockchainAidTracker.Services.DTOs.Supplier;

/// <summary>
/// Data transfer object for supplier information (read)
/// </summary>
public class SupplierDto
{
    /// <summary>
    /// Unique identifier for the supplier
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// User ID of the customer/supplier account
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Legal company name of the supplier
    /// </summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// Company registration ID
    /// </summary>
    public string RegistrationId { get; set; } = string.Empty;

    /// <summary>
    /// Contact email address
    /// </summary>
    public string ContactEmail { get; set; } = string.Empty;

    /// <summary>
    /// Contact phone number
    /// </summary>
    public string ContactPhone { get; set; } = string.Empty;

    /// <summary>
    /// Business category/type
    /// </summary>
    public string BusinessCategory { get; set; } = string.Empty;

    /// <summary>
    /// Minimum shipment value to trigger automatic payment
    /// </summary>
    public decimal PaymentThreshold { get; set; }

    /// <summary>
    /// Verification status
    /// </summary>
    public string VerificationStatus { get; set; } = string.Empty;

    /// <summary>
    /// Flag indicating whether the supplier is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Timestamp when the supplier was created
    /// </summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>
    /// Timestamp when the supplier was last updated
    /// </summary>
    public DateTime UpdatedTimestamp { get; set; }

    /// <summary>
    /// Creates a SupplierDto from a Supplier model
    /// </summary>
    public static SupplierDto FromSupplier(Core.Models.Supplier supplier)
    {
        return new SupplierDto
        {
            Id = supplier.Id,
            UserId = supplier.UserId,
            CompanyName = supplier.CompanyName,
            RegistrationId = supplier.RegistrationId,
            ContactEmail = supplier.ContactEmail,
            ContactPhone = supplier.ContactPhone,
            BusinessCategory = supplier.BusinessCategory,
            PaymentThreshold = supplier.PaymentThreshold,
            VerificationStatus = supplier.VerificationStatus.ToString(),
            IsActive = supplier.IsActive,
            CreatedTimestamp = supplier.CreatedTimestamp,
            UpdatedTimestamp = supplier.UpdatedTimestamp
        };
    }
}
