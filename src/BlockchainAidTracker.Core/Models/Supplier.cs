namespace BlockchainAidTracker.Core.Models;

/// <summary>
/// Enum for supplier verification status
/// </summary>
public enum SupplierVerificationStatus
{
    /// <summary>
    /// Supplier registration is pending admin verification
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Supplier has been verified by admin and can participate in payments
    /// </summary>
    Verified = 1,

    /// <summary>
    /// Supplier registration was rejected
    /// </summary>
    Rejected = 2
}

/// <summary>
/// Represents a supplier/vendor who provides goods for shipments
/// and receives automatic payment via smart contract
/// </summary>
public class Supplier
{
    /// <summary>
    /// Unique identifier for the supplier
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// User ID of the customer/supplier account
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// Legal company name of the supplier
    /// </summary>
    public string CompanyName { get; set; }

    /// <summary>
    /// Company registration ID
    /// </summary>
    public string RegistrationId { get; set; }

    /// <summary>
    /// Contact email address
    /// </summary>
    public string ContactEmail { get; set; }

    /// <summary>
    /// Contact phone number
    /// </summary>
    public string ContactPhone { get; set; }

    /// <summary>
    /// Business category/type (Food, Medicine, Supplies, etc.)
    /// </summary>
    public string BusinessCategory { get; set; }

    /// <summary>
    /// Encrypted bank account details (IBAN/Swift code for payment settlement)
    /// </summary>
    public string EncryptedBankDetails { get; set; }

    /// <summary>
    /// Minimum shipment value (in USD) to trigger automatic payment
    /// </summary>
    public decimal PaymentThreshold { get; set; }

    /// <summary>
    /// Tax ID for the supplier
    /// </summary>
    public string TaxId { get; set; }

    /// <summary>
    /// Verification status (Pending, Verified, Rejected)
    /// </summary>
    public SupplierVerificationStatus VerificationStatus { get; set; }

    /// <summary>
    /// Timestamp when the supplier was created (UTC)
    /// </summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>
    /// Timestamp when the supplier was last updated (UTC)
    /// </summary>
    public DateTime UpdatedTimestamp { get; set; }

    /// <summary>
    /// Flag indicating whether the supplier is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Navigation property for related user
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Navigation property for related supplier shipments
    /// </summary>
    public List<SupplierShipment> SupplierShipments { get; set; }

    /// <summary>
    /// Navigation property for payment records
    /// </summary>
    public List<PaymentRecord> PaymentRecords { get; set; }

    /// <summary>
    /// Default constructor
    /// </summary>
    public Supplier()
    {
        Id = Guid.NewGuid().ToString();
        SupplierShipments = new List<SupplierShipment>();
        PaymentRecords = new List<PaymentRecord>();
        CreatedTimestamp = DateTime.UtcNow;
        UpdatedTimestamp = DateTime.UtcNow;
        VerificationStatus = SupplierVerificationStatus.Pending;
        IsActive = true;
    }

    /// <summary>
    /// Parameterized constructor
    /// </summary>
    public Supplier(
        string userId,
        string companyName,
        string registrationId,
        string contactEmail,
        string contactPhone,
        string businessCategory,
        string encryptedBankDetails,
        decimal paymentThreshold,
        string taxId) : this()
    {
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        CompanyName = companyName ?? throw new ArgumentNullException(nameof(companyName));
        RegistrationId = registrationId ?? throw new ArgumentNullException(nameof(registrationId));
        ContactEmail = contactEmail ?? throw new ArgumentNullException(nameof(contactEmail));
        ContactPhone = contactPhone ?? throw new ArgumentNullException(nameof(contactPhone));
        BusinessCategory = businessCategory ?? throw new ArgumentNullException(nameof(businessCategory));
        EncryptedBankDetails = encryptedBankDetails ?? throw new ArgumentNullException(nameof(encryptedBankDetails));
        PaymentThreshold = paymentThreshold;
        TaxId = taxId ?? throw new ArgumentNullException(nameof(taxId));
    }

    /// <summary>
    /// Verifies the supplier
    /// </summary>
    public void Verify()
    {
        VerificationStatus = SupplierVerificationStatus.Verified;
        UpdatedTimestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Rejects the supplier verification
    /// </summary>
    public void Reject()
    {
        VerificationStatus = SupplierVerificationStatus.Rejected;
        UpdatedTimestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the supplier
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedTimestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the supplier
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedTimestamp = DateTime.UtcNow;
    }
}
