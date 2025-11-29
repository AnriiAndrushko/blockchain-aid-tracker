using System.Text.Json;
using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.SmartContracts.Models;

namespace BlockchainAidTracker.SmartContracts.Contracts;

/// <summary>
/// Smart contract that handles automatic payment release to suppliers when shipments are confirmed
/// </summary>
public class PaymentReleaseContract : SmartContract
{
    public override string Name => "Payment Release Contract";
    public override string Description => "Automatically releases payments to suppliers when shipment confirmation conditions are met";

    public PaymentReleaseContract() : base("payment-release-v1")
    {
    }

    public override bool CanExecute(ContractExecutionContext context)
    {
        // This contract is triggered when a shipment reaches Confirmed status
        // It validates supplier eligibility and calculates payment amounts
        return context.Transaction.Type == TransactionType.StatusUpdated ||
               context.Transaction.Type == TransactionType.PaymentInitiated;
    }

    public override async Task<ContractExecutionResult> ExecuteAsync(ContractExecutionContext context)
    {
        var events = new List<ContractEvent>();
        var stateChanges = new Dictionary<string, object>();
        var output = new Dictionary<string, object>();

        try
        {
            // Parse transaction payload
            var paymentData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(context.Transaction.PayloadData);
            if (paymentData == null)
            {
                return ContractExecutionResult.FailureResult("Invalid transaction payload");
            }

            var shipmentId = paymentData.TryGetValue("ShipmentId", out var idElement)
                ? idElement.GetString() ?? string.Empty
                : string.Empty;

            if (string.IsNullOrEmpty(shipmentId))
            {
                return ContractExecutionResult.FailureResult("Shipment ID not found in payload");
            }

            // Check if this is a shipment confirmation triggering payment
            if (context.Transaction.Type == TransactionType.StatusUpdated)
            {
                var newStatusStr = paymentData.TryGetValue("NewStatus", out var statusElement)
                    ? statusElement.GetString() ?? string.Empty
                    : string.Empty;

                // Only process payments when shipment reaches Confirmed status
                if (newStatusStr != ShipmentStatus.Confirmed.ToString())
                {
                    output["processed"] = false;
                    output["reason"] = "Shipment not in Confirmed status - payment processing skipped";
                    return ContractExecutionResult.SuccessResult(output, stateChanges, events);
                }

                return await HandlePaymentRelease(shipmentId, paymentData, context, events, stateChanges, output);
            }

            // Direct payment initiation transaction
            if (context.Transaction.Type == TransactionType.PaymentInitiated)
            {
                return await HandlePaymentInitiation(shipmentId, paymentData, context, events, stateChanges, output);
            }

            return ContractExecutionResult.FailureResult("Unsupported transaction type for payment release");
        }
        catch (Exception ex)
        {
            events.Add(EmitEvent("PaymentProcessingError", new Dictionary<string, object>
            {
                { "error", ex.Message }
            }));

            return ContractExecutionResult.FailureResult($"Payment processing error: {ex.Message}");
        }
    }

    private async Task<ContractExecutionResult> HandlePaymentRelease(
        string shipmentId,
        Dictionary<string, JsonElement> paymentData,
        ContractExecutionContext context,
        List<ContractEvent> events,
        Dictionary<string, object> stateChanges,
        Dictionary<string, object> output)
    {
        await Task.CompletedTask; // Placeholder for async operations

        // Extract supplier payment information
        var suppliers = ExtractSupplierPayments(paymentData);
        if (!suppliers.Any())
        {
            output["processed"] = false;
            output["reason"] = "No suppliers found for payment release";
            return ContractExecutionResult.SuccessResult(output, stateChanges, events);
        }

        var totalPaymentAmount = 0m;
        var processedSuppliers = new List<string>();

        foreach (var (supplierId, paymentInfo) in suppliers)
        {
            // Validate supplier verification status
            var verificationStatus = GetStateValue($"supplier_{supplierId}_verification_status")?.ToString();
            if (verificationStatus != "Verified")
            {
                events.Add(EmitEvent("SupplierNotVerified", new Dictionary<string, object>
                {
                    { "supplierId", supplierId },
                    { "shipmentId", shipmentId },
                    { "status", verificationStatus ?? "Unknown" }
                }));

                continue; // Skip unverified suppliers
            }

            // Get supplier's payment threshold
            var paymentThresholdStr = GetStateValue($"supplier_{supplierId}_payment_threshold")?.ToString() ?? "0";
            if (!decimal.TryParse(paymentThresholdStr, out var paymentThreshold))
            {
                paymentThreshold = 0;
            }

            var paymentAmountObj = paymentInfo["amount"];
            decimal paymentAmount = paymentAmountObj is decimal dec ? dec : (paymentAmountObj is int intVal ? intVal : Convert.ToDecimal(paymentAmountObj));

            // Check if payment meets threshold
            if (paymentAmount < paymentThreshold)
            {
                events.Add(EmitEvent("PaymentBelowThreshold", new Dictionary<string, object>
                {
                    { "supplierId", supplierId },
                    { "shipmentId", shipmentId },
                    { "amount", paymentAmount },
                    { "threshold", paymentThreshold }
                }));

                continue; // Skip payments below threshold
            }

            // Create payment record in state
            var paymentId = Guid.NewGuid().ToString();
            var paymentKey = $"payment_{paymentId}";

            stateChanges[$"{paymentKey}_supplier_id"] = supplierId;
            stateChanges[$"{paymentKey}_shipment_id"] = shipmentId;
            stateChanges[$"{paymentKey}_amount"] = paymentAmount;
            var currency = paymentInfo.TryGetValue("currency", out var currencyObj)
                ? currencyObj?.ToString() ?? "USD"
                : "USD";
            stateChanges[$"{paymentKey}_currency"] = currency;
            stateChanges[$"{paymentKey}_status"] = "Initiated";
            stateChanges[$"{paymentKey}_created_at"] = context.ExecutionTime;
            stateChanges[$"{paymentKey}_created_by"] = context.Transaction.SenderPublicKey;

            // Update supplier's total earned amount
            var currentEarnedStr = GetStateValue($"supplier_{supplierId}_total_earned")?.ToString() ?? "0";
            if (decimal.TryParse(currentEarnedStr, out var currentEarned))
            {
                stateChanges[$"supplier_{supplierId}_total_earned"] = (currentEarned + paymentAmount).ToString("F2");
            }

            // Emit payment initiated event
            events.Add(EmitEvent("PaymentInitiated", new Dictionary<string, object>
            {
                { "paymentId", paymentId },
                { "supplierId", supplierId },
                { "shipmentId", shipmentId },
                { "amount", paymentAmount },
                { "currency", currency },
                { "timestamp", context.ExecutionTime }
            }));

            totalPaymentAmount += paymentAmount;
            processedSuppliers.Add(supplierId);
        }

        // Update shipment payment state
        if (processedSuppliers.Any())
        {
            stateChanges[$"shipment_{shipmentId}_payment_status"] = "Released";
            stateChanges[$"shipment_{shipmentId}_payment_released_at"] = context.ExecutionTime;
            stateChanges[$"shipment_{shipmentId}_total_payment_released"] = totalPaymentAmount.ToString("F2");

            events.Add(EmitEvent("PaymentReleased", new Dictionary<string, object>
            {
                { "shipmentId", shipmentId },
                { "suppliersCount", processedSuppliers.Count },
                { "totalAmount", totalPaymentAmount },
                { "suppliers", processedSuppliers },
                { "timestamp", context.ExecutionTime }
            }));

            output["processed"] = true;
            output["paymentStatus"] = "Released";
            output["suppliersCount"] = processedSuppliers.Count;
            output["totalAmount"] = totalPaymentAmount;
            output["suppliers"] = processedSuppliers;
        }
        else
        {
            output["processed"] = false;
            output["reason"] = "No eligible suppliers for payment release";
        }

        // Apply state changes to contract state
        if (stateChanges.Any())
        {
            UpdateState(stateChanges);
        }

        return ContractExecutionResult.SuccessResult(output, stateChanges, events);
    }

    private async Task<ContractExecutionResult> HandlePaymentInitiation(
        string shipmentId,
        Dictionary<string, JsonElement> paymentData,
        ContractExecutionContext context,
        List<ContractEvent> events,
        Dictionary<string, object> stateChanges,
        Dictionary<string, object> output)
    {
        await Task.CompletedTask; // Placeholder for async operations

        var supplierId = paymentData.TryGetValue("SupplierId", out var supplierElement)
            ? supplierElement.GetString() ?? string.Empty
            : string.Empty;

        if (string.IsNullOrEmpty(supplierId))
        {
            return ContractExecutionResult.FailureResult("Supplier ID not found in payload");
        }

        var amountStr = "0";
        if (paymentData.TryGetValue("Amount", out var amountElement))
        {
            amountStr = amountElement.ValueKind == JsonValueKind.String
                ? amountElement.GetString() ?? "0"
                : amountElement.GetRawText();
        }

        if (!decimal.TryParse(amountStr, out var amount) || amount <= 0)
        {
            return ContractExecutionResult.FailureResult($"Invalid payment amount: {amountStr}");
        }

        // Verify supplier status
        var verificationStatus = GetStateValue($"supplier_{supplierId}_verification_status")?.ToString();
        if (verificationStatus != "Verified")
        {
            events.Add(EmitEvent("SupplierNotVerifiedForPayment", new Dictionary<string, object>
            {
                { "supplierId", supplierId },
                { "status", verificationStatus ?? "Unknown" }
            }));

            return ContractExecutionResult.FailureResult($"Supplier {supplierId} is not verified for payment");
        }

        // Create payment record
        var paymentId = Guid.NewGuid().ToString();
        var paymentKey = $"payment_{paymentId}";

        stateChanges[$"{paymentKey}_supplier_id"] = supplierId;
        stateChanges[$"{paymentKey}_shipment_id"] = shipmentId;
        stateChanges[$"{paymentKey}_amount"] = amount;
        stateChanges[$"{paymentKey}_currency"] = paymentData.TryGetValue("Currency", out var currencyObj)
            ? currencyObj.GetString() ?? "USD"
            : "USD";
        stateChanges[$"{paymentKey}_status"] = "Initiated";
        stateChanges[$"{paymentKey}_created_at"] = context.ExecutionTime;
        stateChanges[$"{paymentKey}_created_by"] = context.Transaction.SenderPublicKey;

        output["paymentId"] = paymentId;
        output["supplierId"] = supplierId;
        output["shipmentId"] = shipmentId;
        output["amount"] = amount;
        output["status"] = "Initiated";

        events.Add(EmitEvent("PaymentInitiated", new Dictionary<string, object>
        {
            { "paymentId", paymentId },
            { "supplierId", supplierId },
            { "shipmentId", shipmentId },
            { "amount", amount },
            { "timestamp", context.ExecutionTime }
        }));

        // Apply state changes to contract state
        if (stateChanges.Any())
        {
            UpdateState(stateChanges);
        }

        return ContractExecutionResult.SuccessResult(output, stateChanges, events);
    }

    private Dictionary<string, Dictionary<string, object>> ExtractSupplierPayments(Dictionary<string, JsonElement> paymentData)
    {
        var suppliers = new Dictionary<string, Dictionary<string, object>>();

        // Try to extract suppliers array from payload
        if (!paymentData.TryGetValue("Suppliers", out var suppliersElement))
        {
            return suppliers;
        }

        if (suppliersElement.ValueKind != JsonValueKind.Array)
        {
            return suppliers;
        }

        foreach (var supplierElement in suppliersElement.EnumerateArray())
        {
            if (supplierElement.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var supplier = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(supplierElement.GetRawText());
            if (supplier == null)
            {
                continue;
            }

            var supplierId = supplier.TryGetValue("SupplierId", out var idElement)
                ? idElement.GetString() ?? string.Empty
                : string.Empty;

            if (string.IsNullOrEmpty(supplierId))
            {
                continue;
            }

            var amountStr = "0";
            if (supplier.TryGetValue("Amount", out var amountElement))
            {
                amountStr = amountElement.ValueKind == JsonValueKind.String
                    ? amountElement.GetString() ?? "0"
                    : amountElement.GetRawText();
            }

            if (decimal.TryParse(amountStr, out var amount))
            {
                suppliers[supplierId] = new Dictionary<string, object>
                {
                    { "amount", amount },
                    { "currency", supplier.TryGetValue("Currency", out var cur) ? cur.GetString() ?? "USD" : "USD" }
                };
            }
        }

        return suppliers;
    }
}
