using System.Text.Json;
using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.SmartContracts.Models;

namespace BlockchainAidTracker.SmartContracts.Contracts;

/// <summary>
/// Smart contract that manages shipment state transitions and validates business rules
/// </summary>
public class ShipmentTrackingContract : SmartContract
{
    public override string Name => "Shipment Tracking Contract";
    public override string Description => "Manages shipment lifecycle state transitions and validates business rules";

    public ShipmentTrackingContract() : base("shipment-tracking-v1")
    {
    }

    public override bool CanExecute(ContractExecutionContext context)
    {
        // This contract handles all shipment-related transactions
        return context.Transaction.Type == TransactionType.ShipmentCreated ||
               context.Transaction.Type == TransactionType.StatusUpdated;
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

            var shipmentId = shipmentData.TryGetValue("shipmentId", out var idElement)
                ? idElement.GetString() ?? string.Empty
                : string.Empty;

            if (string.IsNullOrEmpty(shipmentId))
            {
                return ContractExecutionResult.FailureResult("Shipment ID not found in payload");
            }

            if (context.Transaction.Type == TransactionType.ShipmentCreated)
            {
                return await HandleShipmentCreation(shipmentId, shipmentData, context, events, stateChanges, output);
            }
            else if (context.Transaction.Type == TransactionType.StatusUpdated)
            {
                return await HandleStatusUpdate(shipmentId, shipmentData, context, events, stateChanges, output);
            }

            return ContractExecutionResult.FailureResult("Unsupported transaction type");
        }
        catch (Exception ex)
        {
            events.Add(EmitEvent("ShipmentTrackingError", new Dictionary<string, object>
            {
                { "error", ex.Message }
            }));

            return ContractExecutionResult.FailureResult($"Tracking error: {ex.Message}");
        }
    }

    private async Task<ContractExecutionResult> HandleShipmentCreation(
        string shipmentId,
        Dictionary<string, JsonElement> shipmentData,
        ContractExecutionContext context,
        List<ContractEvent> events,
        Dictionary<string, object> stateChanges,
        Dictionary<string, object> output)
    {
        // Validate required fields
        var requiredFields = new[] { "origin", "destination", "assignedRecipient" };
        foreach (var field in requiredFields)
        {
            if (!shipmentData.ContainsKey(field) || string.IsNullOrWhiteSpace(shipmentData[field].GetString()))
            {
                return ContractExecutionResult.FailureResult($"Required field '{field}' is missing or empty");
            }
        }

        // Initialize shipment state
        stateChanges[$"shipment_{shipmentId}_status"] = ShipmentStatus.Created.ToString();
        stateChanges[$"shipment_{shipmentId}_createdBy"] = context.Transaction.SenderPublicKey;
        stateChanges[$"shipment_{shipmentId}_createdAt"] = context.ExecutionTime;
        stateChanges[$"shipment_{shipmentId}_lastUpdatedAt"] = context.ExecutionTime;

        output["shipmentId"] = shipmentId;
        output["status"] = ShipmentStatus.Created.ToString();
        output["initialized"] = true;

        events.Add(EmitEvent("ShipmentCreated", new Dictionary<string, object>
        {
            { "shipmentId", shipmentId },
            { "origin", shipmentData["origin"].GetString() ?? string.Empty },
            { "destination", shipmentData["destination"].GetString() ?? string.Empty },
            { "coordinator", context.Transaction.SenderPublicKey },
            { "timestamp", context.ExecutionTime }
        }));

        // Check if shipment can be auto-validated (e.g., if it meets certain criteria)
        if (await ShouldAutoValidate(shipmentData))
        {
            stateChanges[$"shipment_{shipmentId}_status"] = ShipmentStatus.Validated.ToString();
            stateChanges[$"shipment_{shipmentId}_validatedAt"] = context.ExecutionTime;

            events.Add(EmitEvent("ShipmentAutoValidated", new Dictionary<string, object>
            {
                { "shipmentId", shipmentId },
                { "timestamp", context.ExecutionTime }
            }));

            output["status"] = ShipmentStatus.Validated.ToString();
            output["autoValidated"] = true;
        }

        return ContractExecutionResult.SuccessResult(output, stateChanges, events);
    }

    private async Task<ContractExecutionResult> HandleStatusUpdate(
        string shipmentId,
        Dictionary<string, JsonElement> shipmentData,
        ContractExecutionContext context,
        List<ContractEvent> events,
        Dictionary<string, object> stateChanges,
        Dictionary<string, object> output)
    {
        // Get current and new status
        var currentStatusStr = GetStateValue($"shipment_{shipmentId}_status")?.ToString();
        if (currentStatusStr == null)
        {
            return ContractExecutionResult.FailureResult("Shipment not found in contract state");
        }

        if (!Enum.TryParse<ShipmentStatus>(currentStatusStr, out var currentStatus))
        {
            return ContractExecutionResult.FailureResult("Invalid current status in state");
        }

        var newStatusStr = shipmentData.TryGetValue("newStatus", out var statusElement)
            ? statusElement.GetString() ?? string.Empty
            : string.Empty;

        if (!Enum.TryParse<ShipmentStatus>(newStatusStr, out var newStatus))
        {
            return ContractExecutionResult.FailureResult("Invalid new status in payload");
        }

        // Validate state transition
        if (!IsValidTransition(currentStatus, newStatus))
        {
            events.Add(EmitEvent("InvalidStateTransition", new Dictionary<string, object>
            {
                { "shipmentId", shipmentId },
                { "currentStatus", currentStatus.ToString() },
                { "attemptedStatus", newStatus.ToString() }
            }));

            return ContractExecutionResult.FailureResult(
                $"Invalid state transition from {currentStatus} to {newStatus}");
        }

        // Update state
        stateChanges[$"shipment_{shipmentId}_status"] = newStatus.ToString();
        stateChanges[$"shipment_{shipmentId}_lastUpdatedAt"] = context.ExecutionTime;
        stateChanges[$"shipment_{shipmentId}_lastUpdatedBy"] = context.Transaction.SenderPublicKey;

        output["shipmentId"] = shipmentId;
        output["previousStatus"] = currentStatus.ToString();
        output["newStatus"] = newStatus.ToString();
        output["transitionValid"] = true;

        events.Add(EmitEvent("ShipmentStatusUpdated", new Dictionary<string, object>
        {
            { "shipmentId", shipmentId },
            { "previousStatus", currentStatus.ToString() },
            { "newStatus", newStatus.ToString() },
            { "updatedBy", context.Transaction.SenderPublicKey },
            { "timestamp", context.ExecutionTime }
        }));

        // Check if shipment reached destination (Delivered status)
        if (newStatus == ShipmentStatus.Delivered)
        {
            events.Add(EmitEvent("ShipmentReachedDestination", new Dictionary<string, object>
            {
                { "shipmentId", shipmentId },
                { "timestamp", context.ExecutionTime }
            }));
        }

        return ContractExecutionResult.SuccessResult(output, stateChanges, events);
    }

    private bool IsValidTransition(ShipmentStatus currentStatus, ShipmentStatus newStatus)
    {
        return currentStatus switch
        {
            ShipmentStatus.Created => newStatus == ShipmentStatus.Validated,
            ShipmentStatus.Validated => newStatus == ShipmentStatus.InTransit,
            ShipmentStatus.InTransit => newStatus == ShipmentStatus.Delivered,
            ShipmentStatus.Delivered => newStatus == ShipmentStatus.Confirmed,
            ShipmentStatus.Confirmed => false, // Terminal state
            _ => false
        };
    }

    private async Task<bool> ShouldAutoValidate(Dictionary<string, JsonElement> shipmentData)
    {
        // Simple auto-validation logic: shipment is auto-validated if it has all required fields
        // In a real system, this could involve checking against external validation services,
        // verifying donor funding, checking coordinator permissions, etc.

        await Task.CompletedTask; // Placeholder for async operations

        // For now, we'll auto-validate if the shipment has items
        if (shipmentData.TryGetValue("items", out var itemsElement))
        {
            if (itemsElement.ValueKind == JsonValueKind.Array)
            {
                var itemsArray = itemsElement.EnumerateArray().ToList();
                return itemsArray.Count > 0;
            }
        }

        return false;
    }
}
