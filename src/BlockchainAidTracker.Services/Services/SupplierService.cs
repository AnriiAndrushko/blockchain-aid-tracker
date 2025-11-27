using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.DataAccess.Repositories;
using BlockchainAidTracker.Services.DTOs.Payment;
using BlockchainAidTracker.Services.DTOs.Supplier;
using BlockchainAidTracker.Services.Exceptions;
using BlockchainAidTracker.Services.Interfaces;

namespace BlockchainAidTracker.Services.Services;

/// <summary>
/// Implementation of supplier management service
/// </summary>
public class SupplierService : ISupplierService
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly ISupplierShipmentRepository _supplierShipmentRepository;
    private readonly IPaymentRepository _paymentRepository;

    /// <summary>
    /// Constructor
    /// </summary>
    public SupplierService(
        ISupplierRepository supplierRepository,
        ISupplierShipmentRepository supplierShipmentRepository,
        IPaymentRepository paymentRepository)
    {
        _supplierRepository = supplierRepository ?? throw new ArgumentNullException(nameof(supplierRepository));
        _supplierShipmentRepository = supplierShipmentRepository ?? throw new ArgumentNullException(nameof(supplierShipmentRepository));
        _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
    }

    /// <inheritdoc />
    public async Task<SupplierDto> RegisterSupplierAsync(
        string userId,
        CreateSupplierRequest request,
        IKeyManagementService keyManagementService)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // Validate request
        ValidateCreateSupplierRequest(request);

        // Check if supplier already exists for this user
        var existingSupplier = await _supplierRepository.GetByUserIdAsync(userId);
        if (existingSupplier != null)
            throw new BusinessException($"User already has a supplier profile");

        // Check if company name is unique
        if (await _supplierRepository.CompanyNameExistsAsync(request.CompanyName))
            throw new BusinessException($"Company name '{request.CompanyName}' is already registered");

        // Check if tax ID is unique
        if (await _supplierRepository.TaxIdExistsAsync(request.TaxId))
            throw new BusinessException($"Tax ID '{request.TaxId}' is already registered");

        // Encrypt bank details using Base64 encoding as simple encryption
        var encryptedBankDetails = EncryptBankDetails(request.BankDetails);

        // Create supplier
        var supplier = new Supplier(
            userId,
            request.CompanyName,
            request.RegistrationId,
            request.ContactEmail,
            request.ContactPhone,
            request.BusinessCategory,
            encryptedBankDetails,
            request.PaymentThreshold,
            request.TaxId);

        await _supplierRepository.AddAsync(supplier);

        return SupplierDto.FromSupplier(supplier);
    }

    /// <inheritdoc />
    public async Task<SupplierDto?> GetSupplierByIdAsync(string supplierId)
    {
        if (string.IsNullOrWhiteSpace(supplierId))
            throw new ArgumentException("Supplier ID cannot be null or empty", nameof(supplierId));

        var supplier = await _supplierRepository.GetByIdAsync(supplierId);
        return supplier != null ? SupplierDto.FromSupplier(supplier) : null;
    }

    /// <inheritdoc />
    public async Task<SupplierDto?> GetSupplierByUserIdAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        var supplier = await _supplierRepository.GetByUserIdAsync(userId);
        return supplier != null ? SupplierDto.FromSupplier(supplier) : null;
    }

    /// <inheritdoc />
    public async Task<List<SupplierDto>> GetAllSuppliersAsync(SupplierVerificationStatus? status = null)
    {
        List<Supplier> suppliers;

        if (status.HasValue)
        {
            suppliers = await _supplierRepository.GetByVerificationStatusAsync(status.Value);
        }
        else
        {
            suppliers = await _supplierRepository.GetAllAsync();
        }

        return suppliers.Select(SupplierDto.FromSupplier).ToList();
    }

    /// <inheritdoc />
    public async Task<List<SupplierDto>> GetVerifiedSuppliersAsync()
    {
        var suppliers = await _supplierRepository.GetVerifiedAsync();
        return suppliers.Select(SupplierDto.FromSupplier).ToList();
    }

    /// <inheritdoc />
    public async Task<SupplierDto> UpdateSupplierAsync(
        string supplierId,
        UpdateSupplierRequest request,
        IKeyManagementService keyManagementService)
    {
        if (string.IsNullOrWhiteSpace(supplierId))
            throw new ArgumentException("Supplier ID cannot be null or empty", nameof(supplierId));

        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var supplier = await _supplierRepository.GetByIdAsync(supplierId);
        if (supplier == null)
            throw new NotFoundException($"Supplier with ID '{supplierId}' not found");

        // Update fields if provided
        if (!string.IsNullOrWhiteSpace(request.ContactEmail))
            supplier.ContactEmail = request.ContactEmail.Trim();

        if (!string.IsNullOrWhiteSpace(request.ContactPhone))
            supplier.ContactPhone = request.ContactPhone.Trim();

        if (!string.IsNullOrWhiteSpace(request.BankDetails))
            supplier.EncryptedBankDetails = EncryptBankDetails(request.BankDetails);

        if (request.PaymentThreshold.HasValue && request.PaymentThreshold.Value > 0)
            supplier.PaymentThreshold = request.PaymentThreshold.Value;

        supplier.UpdatedTimestamp = DateTime.UtcNow;

        _supplierRepository.Update(supplier);

        return SupplierDto.FromSupplier(supplier);
    }

    /// <inheritdoc />
    public async Task<SupplierDto> VerifySupplierAsync(string supplierId)
    {
        if (string.IsNullOrWhiteSpace(supplierId))
            throw new ArgumentException("Supplier ID cannot be null or empty", nameof(supplierId));

        var supplier = await _supplierRepository.GetByIdAsync(supplierId);
        if (supplier == null)
            throw new NotFoundException($"Supplier with ID '{supplierId}' not found");

        supplier.Verify();
        _supplierRepository.Update(supplier);

        return SupplierDto.FromSupplier(supplier);
    }

    /// <inheritdoc />
    public async Task<SupplierDto> RejectSupplierAsync(string supplierId)
    {
        if (string.IsNullOrWhiteSpace(supplierId))
            throw new ArgumentException("Supplier ID cannot be null or empty", nameof(supplierId));

        var supplier = await _supplierRepository.GetByIdAsync(supplierId);
        if (supplier == null)
            throw new NotFoundException($"Supplier with ID '{supplierId}' not found");

        supplier.Reject();
        _supplierRepository.Update(supplier);

        return SupplierDto.FromSupplier(supplier);
    }

    /// <inheritdoc />
    public async Task<SupplierDto> ActivateSupplierAsync(string supplierId)
    {
        if (string.IsNullOrWhiteSpace(supplierId))
            throw new ArgumentException("Supplier ID cannot be null or empty", nameof(supplierId));

        var supplier = await _supplierRepository.GetByIdAsync(supplierId);
        if (supplier == null)
            throw new NotFoundException($"Supplier with ID '{supplierId}' not found");

        supplier.Activate();
        _supplierRepository.Update(supplier);

        return SupplierDto.FromSupplier(supplier);
    }

    /// <inheritdoc />
    public async Task<SupplierDto> DeactivateSupplierAsync(string supplierId)
    {
        if (string.IsNullOrWhiteSpace(supplierId))
            throw new ArgumentException("Supplier ID cannot be null or empty", nameof(supplierId));

        var supplier = await _supplierRepository.GetByIdAsync(supplierId);
        if (supplier == null)
            throw new NotFoundException($"Supplier with ID '{supplierId}' not found");

        supplier.Deactivate();
        _supplierRepository.Update(supplier);

        return SupplierDto.FromSupplier(supplier);
    }

    /// <inheritdoc />
    public async Task<List<SupplierShipmentDto>> GetSupplierShipmentsAsync(string supplierId)
    {
        if (string.IsNullOrWhiteSpace(supplierId))
            throw new ArgumentException("Supplier ID cannot be null or empty", nameof(supplierId));

        // Verify supplier exists
        var supplier = await _supplierRepository.GetByIdAsync(supplierId);
        if (supplier == null)
            throw new NotFoundException($"Supplier with ID '{supplierId}' not found");

        var shipments = await _supplierShipmentRepository.GetBySupplierIdAsync(supplierId);
        return shipments.Select(SupplierShipmentDto.FromSupplierShipment).ToList();
    }

    /// <inheritdoc />
    public async Task<List<PaymentHistoryDto>> GetSupplierPaymentHistoryAsync(string supplierId)
    {
        if (string.IsNullOrWhiteSpace(supplierId))
            throw new ArgumentException("Supplier ID cannot be null or empty", nameof(supplierId));

        // Verify supplier exists
        var supplier = await _supplierRepository.GetByIdAsync(supplierId);
        if (supplier == null)
            throw new NotFoundException($"Supplier with ID '{supplierId}' not found");

        var payments = await _paymentRepository.GetBySupplierIdAsync(supplierId);
        return payments.Select(PaymentHistoryDto.FromPaymentRecord).ToList();
    }

    /// <summary>
    /// Validates create supplier request
    /// </summary>
    private static void ValidateCreateSupplierRequest(CreateSupplierRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyName))
            throw new ArgumentException("Company name is required", nameof(request.CompanyName));

        if (string.IsNullOrWhiteSpace(request.RegistrationId))
            throw new ArgumentException("Registration ID is required", nameof(request.RegistrationId));

        if (string.IsNullOrWhiteSpace(request.ContactEmail))
            throw new ArgumentException("Contact email is required", nameof(request.ContactEmail));

        if (!request.ContactEmail.Contains("@"))
            throw new ArgumentException("Invalid email format", nameof(request.ContactEmail));

        if (string.IsNullOrWhiteSpace(request.ContactPhone))
            throw new ArgumentException("Contact phone is required", nameof(request.ContactPhone));

        if (string.IsNullOrWhiteSpace(request.BusinessCategory))
            throw new ArgumentException("Business category is required", nameof(request.BusinessCategory));

        if (string.IsNullOrWhiteSpace(request.BankDetails))
            throw new ArgumentException("Bank details are required", nameof(request.BankDetails));

        if (request.PaymentThreshold <= 0)
            throw new ArgumentException("Payment threshold must be greater than zero", nameof(request.PaymentThreshold));

        if (string.IsNullOrWhiteSpace(request.TaxId))
            throw new ArgumentException("Tax ID is required", nameof(request.TaxId));
    }

    /// <summary>
    /// Encrypts bank details using Base64 encoding (simple obfuscation for prototype)
    /// Production should use proper AES-256 encryption
    /// </summary>
    private static string EncryptBankDetails(string bankDetails)
    {
        if (string.IsNullOrEmpty(bankDetails))
            throw new ArgumentException("Bank details cannot be null or empty");

        var bytes = System.Text.Encoding.UTF8.GetBytes(bankDetails);
        return Convert.ToBase64String(bytes);
    }
}
