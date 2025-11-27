using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.DataAccess.Repositories;
using BlockchainAidTracker.Services.DTOs.Payment;
using BlockchainAidTracker.Services.Exceptions;
using BlockchainAidTracker.Services.Interfaces;

namespace BlockchainAidTracker.Services.Services;

/// <summary>
/// Implementation of payment processing service
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly ISupplierShipmentRepository _supplierShipmentRepository;

    /// <summary>
    /// Constructor
    /// </summary>
    public PaymentService(
        IPaymentRepository paymentRepository,
        ISupplierRepository supplierRepository,
        ISupplierShipmentRepository supplierShipmentRepository)
    {
        _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        _supplierRepository = supplierRepository ?? throw new ArgumentNullException(nameof(supplierRepository));
        _supplierShipmentRepository = supplierShipmentRepository ?? throw new ArgumentNullException(nameof(supplierShipmentRepository));
    }

    /// <inheritdoc />
    public async Task<decimal> CalculatePaymentAmountAsync(string shipmentId, string supplierId)
    {
        if (string.IsNullOrWhiteSpace(shipmentId))
            throw new ArgumentException("Shipment ID cannot be null or empty", nameof(shipmentId));

        if (string.IsNullOrWhiteSpace(supplierId))
            throw new ArgumentException("Supplier ID cannot be null or empty", nameof(supplierId));

        // Get all pending supplier shipments for this supplier on this shipment
        var supplierShipments = await _supplierShipmentRepository.GetByShipmentIdAsync(shipmentId);
        var supplierShipment = supplierShipments.FirstOrDefault(ss => ss.SupplierId == supplierId && ss.PaymentStatus == SupplierShipmentPaymentStatus.Pending);

        if (supplierShipment == null)
            return 0m;

        return supplierShipment.Value;
    }

    /// <inheritdoc />
    public async Task<PaymentDto> InitiatePaymentAsync(string shipmentId, string supplierId, PaymentMethod paymentMethod)
    {
        if (string.IsNullOrWhiteSpace(shipmentId))
            throw new ArgumentException("Shipment ID cannot be null or empty", nameof(shipmentId));

        if (string.IsNullOrWhiteSpace(supplierId))
            throw new ArgumentException("Supplier ID cannot be null or empty", nameof(supplierId));

        // Verify supplier exists and is verified
        var supplier = await _supplierRepository.GetByIdAsync(supplierId);
        if (supplier == null)
            throw new NotFoundException($"Supplier with ID '{supplierId}' not found");

        if (supplier.VerificationStatus != SupplierVerificationStatus.Verified)
            throw new BusinessException($"Supplier must be verified before payment can be initiated");

        // Calculate payment amount
        var amount = await CalculatePaymentAmountAsync(shipmentId, supplierId);
        if (amount <= 0)
            throw new BusinessException($"No pending payments found for supplier on this shipment");

        // Check payment threshold
        if (amount < supplier.PaymentThreshold)
            throw new BusinessException($"Payment amount {amount} is below threshold {supplier.PaymentThreshold}");

        // Get supplier shipment to determine currency
        var supplierShipments = await _supplierShipmentRepository.GetByShipmentIdAsync(shipmentId);
        var supplierShipment = supplierShipments.FirstOrDefault(ss => ss.SupplierId == supplierId);
        if (supplierShipment == null)
            throw new NotFoundException("Supplier shipment not found");

        // Create payment record
        var payment = new PaymentRecord(supplierId, shipmentId, amount, supplierShipment.Currency, paymentMethod);
        await _paymentRepository.AddAsync(payment);

        return PaymentDto.FromPaymentRecord(payment);
    }

    /// <inheritdoc />
    public async Task<PaymentDto> CompletePaymentAsync(string paymentId, string? externalReference = null, string? transactionHash = null)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
            throw new ArgumentException("Payment ID cannot be null or empty", nameof(paymentId));

        var payment = await _paymentRepository.GetByIdAsync(paymentId);
        if (payment == null)
            throw new NotFoundException($"Payment with ID '{paymentId}' not found");

        payment.Complete(externalReference, transactionHash);
        _paymentRepository.Update(payment);

        // Update supplier shipment payment status
        var shipmentSuppliers = await _supplierShipmentRepository.GetByShipmentIdAsync(payment.ShipmentId);
        var shipmentSupplier = shipmentSuppliers.FirstOrDefault(ss => ss.SupplierId == payment.SupplierId);
        if (shipmentSupplier != null && !shipmentSupplier.PaymentReleased)
        {
            shipmentSupplier.ReleasePayment(transactionHash ?? "");
            _supplierShipmentRepository.Update(shipmentSupplier);
        }

        return PaymentDto.FromPaymentRecord(payment);
    }

    /// <inheritdoc />
    public async Task<PaymentDto> FailPaymentAsync(string paymentId, string reason)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
            throw new ArgumentException("Payment ID cannot be null or empty", nameof(paymentId));

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Failure reason cannot be null or empty", nameof(reason));

        var payment = await _paymentRepository.GetByIdAsync(paymentId);
        if (payment == null)
            throw new NotFoundException($"Payment with ID '{paymentId}' not found");

        payment.MarkAsFailed(reason);
        _paymentRepository.Update(payment);

        return PaymentDto.FromPaymentRecord(payment);
    }

    /// <inheritdoc />
    public async Task<PaymentDto> RetryPaymentAsync(string paymentId)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
            throw new ArgumentException("Payment ID cannot be null or empty", nameof(paymentId));

        var payment = await _paymentRepository.GetByIdAsync(paymentId);
        if (payment == null)
            throw new NotFoundException($"Payment with ID '{paymentId}' not found");

        if (payment.Status != PaymentRecordStatus.Failed)
            throw new BusinessException($"Payment is not in failed status, current status: {payment.Status}");

        if (payment.AttemptCount >= 3)
            throw new BusinessException($"Maximum retry attempts ({3}) exceeded for this payment");

        payment.MarkAsRetrying();
        _paymentRepository.Update(payment);

        return PaymentDto.FromPaymentRecord(payment);
    }

    /// <inheritdoc />
    public async Task<PaymentDto?> GetPaymentByIdAsync(string paymentId)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
            throw new ArgumentException("Payment ID cannot be null or empty", nameof(paymentId));

        var payment = await _paymentRepository.GetByIdAsync(paymentId);
        return payment != null ? PaymentDto.FromPaymentRecord(payment) : null;
    }

    /// <inheritdoc />
    public async Task<List<PaymentDto>> GetPendingPaymentsAsync()
    {
        var payments = await _paymentRepository.GetPendingAsync();
        return payments.Select(PaymentDto.FromPaymentRecord).ToList();
    }

    /// <inheritdoc />
    public async Task<List<PaymentDto>> GetRetryablePaymentsAsync()
    {
        var payments = await _paymentRepository.GetRetryableFailedPaymentsAsync();
        return payments.Select(PaymentDto.FromPaymentRecord).ToList();
    }

    /// <inheritdoc />
    public async Task<List<PaymentDto>> GetSupplierPaymentsAsync(string supplierId)
    {
        if (string.IsNullOrWhiteSpace(supplierId))
            throw new ArgumentException("Supplier ID cannot be null or empty", nameof(supplierId));

        // Verify supplier exists
        var supplier = await _supplierRepository.GetByIdAsync(supplierId);
        if (supplier == null)
            throw new NotFoundException($"Supplier with ID '{supplierId}' not found");

        var payments = await _paymentRepository.GetBySupplierIdAsync(supplierId);
        return payments.Select(PaymentDto.FromPaymentRecord).ToList();
    }

    /// <inheritdoc />
    public async Task<List<PaymentDto>> GetShipmentPaymentsAsync(string shipmentId)
    {
        if (string.IsNullOrWhiteSpace(shipmentId))
            throw new ArgumentException("Shipment ID cannot be null or empty", nameof(shipmentId));

        var payments = await _paymentRepository.GetByShipmentIdAsync(shipmentId);
        return payments.Select(PaymentDto.FromPaymentRecord).ToList();
    }

    /// <inheritdoc />
    public async Task<bool> IsSupplierEligibleForPaymentAsync(string supplierId)
    {
        if (string.IsNullOrWhiteSpace(supplierId))
            throw new ArgumentException("Supplier ID cannot be null or empty", nameof(supplierId));

        var supplier = await _supplierRepository.GetByIdAsync(supplierId);
        if (supplier == null)
            return false;

        return supplier.IsActive && supplier.VerificationStatus == SupplierVerificationStatus.Verified;
    }

    /// <inheritdoc />
    public async Task<decimal> GetSupplierTotalEarnedAsync(string supplierId)
    {
        if (string.IsNullOrWhiteSpace(supplierId))
            throw new ArgumentException("Supplier ID cannot be null or empty", nameof(supplierId));

        // Verify supplier exists
        var supplier = await _supplierRepository.GetByIdAsync(supplierId);
        if (supplier == null)
            throw new NotFoundException($"Supplier with ID '{supplierId}' not found");

        var completedTotal = await _paymentRepository.GetTotalBySupplierAndStatusAsync(supplierId, PaymentRecordStatus.Completed);
        return completedTotal;
    }
}
