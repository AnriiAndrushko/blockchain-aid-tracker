using System.Text.Json;
using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.SmartContracts.Models;

namespace BlockchainAidTracker.SmartContracts.Contracts;

/// <summary>
/// Smart contract that verifies delivery confirmation and validates business rules
/// </summary>
public class DeliveryVerificationContract : SmartContract
{
    public override string Name => "Delivery Verification Contract";
    public override string Description => "Validates delivery confirmations and ensures proper recipient verification";

    public DeliveryVerificationContract() : base("delivery-verification-v1")
    {
    }

    public override bool CanExecute(ContractExecutionContext context)
    {
        // This contract handles DELIVERY_CONFIRMED transactions
        return context.Transaction.Type == TransactionType.DeliveryConfirmed;
    }

    public override async Task<ContractExecutionResult> ExecuteAsync(ContractExecutionContext context)
    {
        var events = new List<ContractEvent>();
        var stateChanges = new Dictionary<string, object>();
        var output = new Dictionary<string, object>();

        try
        {
            // Parse the shipment data from transaction payload
            var shipmentData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(context.Transaction.PayloadData);
            if (shipmentData == null)
            {
                return ContractExecutionResult.FailureResult("Invalid transaction payload");
            }

            var shipmentId = shipmentData.TryGetValue("ShipmentId", out var idElement)
                ? idElement.GetString() ?? string.Empty
                : string.Empty;

            if (string.IsNullOrEmpty(shipmentId))
            {
                return ContractExecutionResult.FailureResult("Shipment ID not found in payload");
            }

            // Verify the sender is the assigned recipient
            var assignedRecipient = shipmentData.TryGetValue("RecipientId", out var recipientElement)
                ? recipientElement.GetString() ?? string.Empty
                : string.Empty;

            if (context.Transaction.SenderPublicKey != assignedRecipient)
            {
                events.Add(EmitEvent("DeliveryVerificationFailed", new Dictionary<string, object>
                {
                    { "shipmentId", shipmentId },
                    { "reason", "Sender is not the assigned recipient" },
                    { "expectedRecipient", assignedRecipient },
                    { "actualSender", context.Transaction.SenderPublicKey }
                }));

                return ContractExecutionResult.FailureResult("Delivery can only be confirmed by the assigned recipient", events);
            }

            // Verify QR code data if provided
            if (context.Data.TryGetValue("qrCodeData", out var qrCodeObj) && qrCodeObj is string qrCodeData)
            {
                var expectedQrCode = shipmentData.TryGetValue("qrCodeData", out var qrElement)
                    ? qrElement.GetString() ?? string.Empty
                    : string.Empty;

                if (!string.IsNullOrEmpty(expectedQrCode) && qrCodeData != expectedQrCode)
                {
                    events.Add(EmitEvent("QRCodeVerificationFailed", new Dictionary<string, object>
                    {
                        { "shipmentId", shipmentId },
                        { "expectedQrCode", expectedQrCode },
                        { "providedQrCode", qrCodeData }
                    }));

                    return ContractExecutionResult.FailureResult("QR code verification failed", events);
                }

                output["qrCodeVerified"] = true;
            }

            // Check delivery timeframe
            var actualDeliveryDate = context.ExecutionTime;
            var expectedTimeframe = shipmentData.TryGetValue("expectedDeliveryTimeframe", out var timeframeElement)
                ? timeframeElement.GetString() ?? string.Empty
                : string.Empty;

            var onTime = IsDeliveryOnTime(actualDeliveryDate, expectedTimeframe);

            // Record successful verification
            stateChanges[$"delivery_{shipmentId}_verified"] = true;
            stateChanges[$"delivery_{shipmentId}_timestamp"] = actualDeliveryDate;
            stateChanges[$"delivery_{shipmentId}_onTime"] = onTime;

            output["verified"] = true;
            output["shipmentId"] = shipmentId;
            output["deliveryTimestamp"] = actualDeliveryDate;
            output["onTime"] = onTime;

            events.Add(EmitEvent("DeliveryVerified", new Dictionary<string, object>
            {
                { "shipmentId", shipmentId },
                { "recipient", context.Transaction.SenderPublicKey },
                { "deliveryTimestamp", actualDeliveryDate },
                { "onTime", onTime }
            }));

            if (!onTime)
            {
                events.Add(EmitEvent("DeliveryDelayed", new Dictionary<string, object>
                {
                    { "shipmentId", shipmentId },
                    { "expectedTimeframe", expectedTimeframe },
                    { "actualDeliveryDate", actualDeliveryDate }
                }));
            }

            return ContractExecutionResult.SuccessResult(output, stateChanges, events);
        }
        catch (Exception ex)
        {
            events.Add(EmitEvent("DeliveryVerificationError", new Dictionary<string, object>
            {
                { "error", ex.Message }
            }));

            return ContractExecutionResult.FailureResult($"Verification error: {ex.Message}");
        }
    }

    private bool IsDeliveryOnTime(DateTime actualDeliveryDate, string expectedTimeframe)
    {
        // Simple timeframe parsing (format: "YYYY-MM-DD to YYYY-MM-DD")
        if (string.IsNullOrWhiteSpace(expectedTimeframe))
            return true; // No timeframe specified, consider on time

        var parts = expectedTimeframe.Split(" to ", StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
            return true; // Invalid format, can't determine

        if (DateTime.TryParse(parts[1], out var endDate))
        {
            // Delivery is on time if it's before or on the end date
            return actualDeliveryDate.Date <= endDate.Date;
        }

        return true; // Can't parse, assume on time
    }
}
