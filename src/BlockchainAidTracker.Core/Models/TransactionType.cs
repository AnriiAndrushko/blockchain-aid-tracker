namespace BlockchainAidTracker.Core.Models;

/// <summary>
/// Defines the types of transactions that can occur in the blockchain.
/// </summary>
public enum TransactionType
{
    /// <summary>
    /// A new shipment has been created in the system.
    /// </summary>
    ShipmentCreated,

    /// <summary>
    /// The status of an existing shipment has been updated.
    /// </summary>
    StatusUpdated,

    /// <summary>
    /// A shipment delivery has been confirmed by the recipient.
    /// </summary>
    DeliveryConfirmed,

    /// <summary>
    /// A shipment has been validated by a coordinator.
    /// </summary>
    ShipmentValidated,

    /// <summary>
    /// A shipment has been marked as in transit.
    /// </summary>
    ShipmentInTransit,

    /// <summary>
    /// A supplier has been registered in the system.
    /// </summary>
    SupplierRegistered,

    /// <summary>
    /// A supplier has been verified by an administrator.
    /// </summary>
    SupplierVerified,

    /// <summary>
    /// A supplier's information has been updated.
    /// </summary>
    SupplierUpdated,

    /// <summary>
    /// A payment for goods from a supplier has been initiated.
    /// </summary>
    PaymentInitiated,

    /// <summary>
    /// A payment for goods from a supplier has been released/completed.
    /// </summary>
    PaymentReleased,

    /// <summary>
    /// A payment has failed to process.
    /// </summary>
    PaymentFailed,

    /// <summary>
    /// A shipment location has been updated by logistics partner.
    /// </summary>
    LocationUpdated,

    /// <summary>
    /// Delivery has been started by logistics partner.
    /// </summary>
    DeliveryStarted,

    /// <summary>
    /// A delivery issue has been reported.
    /// </summary>
    DeliveryIssueReported,

    /// <summary>
    /// A delivery receipt has been confirmed.
    /// </summary>
    DeliveryReceiptConfirmed
}
