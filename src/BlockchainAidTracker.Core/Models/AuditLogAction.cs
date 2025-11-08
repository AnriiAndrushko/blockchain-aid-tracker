namespace BlockchainAidTracker.Core.Models;

/// <summary>
/// Represents the type of action performed in the system
/// </summary>
public enum AuditLogAction
{
    // Authentication Actions
    UserRegistered,
    UserLoggedIn,
    UserLoggedOut,
    TokenRefreshed,

    // User Management Actions
    UserProfileUpdated,
    UserRoleAssigned,
    UserActivated,
    UserDeactivated,

    // Shipment Actions
    ShipmentCreated,
    ShipmentStatusUpdated,
    ShipmentDeliveryConfirmed,
    ShipmentQrCodeGenerated,

    // Blockchain Actions
    TransactionAdded,
    BlockCreated,
    BlockchainValidated,
    BlockchainSaved,
    BlockchainLoaded,

    // Validator Actions
    ValidatorRegistered,
    ValidatorActivated,
    ValidatorDeactivated,
    ValidatorUpdated,
    ValidatorSelectedForBlock,

    // Smart Contract Actions
    SmartContractDeployed,
    SmartContractExecuted,
    SmartContractEventEmitted
}
