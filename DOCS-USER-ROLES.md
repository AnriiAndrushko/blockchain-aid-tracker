# User Roles and Permissions in Blockchain Aid Tracker

This document describes all six user roles in the system and their specific permissions in the **current implementation**.

## Overview

The system defines six user roles, each with different levels of access and capabilities:

1. **Administrator** - Full system access
2. **Coordinator** - Creates and manages shipments
3. **Recipient** - Receives aid and confirms deliveries
4. **Validator** - Operates validator nodes for blockchain consensus
5. **Donor** - Funds humanitarian aid (limited implementation)
6. **LogisticsPartner** - Handles transportation (not yet implemented)

---

## Role Definitions

### 1. Administrator

**Purpose**: System administrator with full access to all features.

**Capabilities**:
- ✅ **User Management** (Exclusive)
  - View all users
  - Assign roles to users
  - Activate/deactivate user accounts
  - View user details
- ✅ **Validator Management** (Exclusive)
  - Register new validators
  - View all validators
  - Update validator settings (priority, network address)
  - Activate/deactivate validators
- ✅ **Consensus Operations** (Shared with Validator)
  - View consensus status
  - Manually create blocks
  - Validate blocks
  - View active validators
- ✅ **Shipment Operations** (Shared with Coordinator)
  - Create shipments
  - Update shipment status
  - View all shipments
  - Access blockchain history
- ✅ **General Access** (Shared with all authenticated users)
  - View blockchain explorer
  - View smart contracts
  - View dashboard
  - Manage own profile

**UI Access**:
- Dashboard
- Shipments (list, create, detail, update status)
- Blockchain Explorer
- Smart Contracts
- Consensus Dashboard
- **User Management** (Admin only)
- **Validator Management** (Admin only)
- My Profile

**API Endpoints** (Administrator-specific):
- `GET /api/users` - List all users
- `POST /api/users/assign-role` - Assign roles
- `POST /api/users/{id}/activate` - Activate user
- `POST /api/users/{id}/deactivate` - Deactivate user
- `POST /api/validators` - Register validator
- `PUT /api/validators/{id}` - Update validator
- `POST /api/validators/{id}/activate` - Activate validator
- `POST /api/validators/{id}/deactivate` - Deactivate validator

**Code Reference**: `src/BlockchainAidTracker.Api/Controllers/UserController.cs:160,226,281,354,425`

---

### 2. Coordinator

**Purpose**: Humanitarian aid coordinator who creates and manages shipments.

**Capabilities**:
- ✅ **Shipment Creation** (Exclusive with Administrator)
  - Create new shipments with items
  - Assign shipments to recipients
  - Generate QR codes for shipments
  - Record shipment creation on blockchain
- ✅ **Shipment Management**
  - Update shipment status (Created → Validated → InTransit → Delivered)
  - View all shipments
  - View shipment details
  - Access blockchain transaction history
- ✅ **General Access** (Shared with all authenticated users)
  - View blockchain explorer
  - View smart contracts
  - View dashboard
  - Manage own profile

**UI Access**:
- Dashboard
- Shipments (list, detail)
- **Create Shipment** (Coordinator only in navigation menu)
- Update Shipment Status (via modal)
- Blockchain Explorer
- Smart Contracts
- My Profile

**API Endpoints** (Coordinator-specific or shared):
- `POST /api/shipments` - Create shipment (requires Coordinator or Administrator role)
- `PUT /api/shipments/{id}/status` - Update shipment status

**Workflow**:
1. Coordinator creates a shipment with items and assigns it to a recipient
2. System generates QR code and records transaction on blockchain
3. Coordinator updates status as shipment progresses (Validated → InTransit → Delivered)
4. Each status update is recorded on blockchain

**Code Reference**:
- `src/BlockchainAidTracker.Services/Services/ShipmentService.cs:60` - Role validation
- `src/BlockchainAidTracker.Web/Components/Layout/NavMenu.razor:35` - UI authorization

---

### 3. Recipient

**Purpose**: Receives humanitarian aid packages and confirms delivery.

**Capabilities**:
- ✅ **Delivery Confirmation** (Exclusive)
  - Confirm delivery of shipments assigned to them
  - Must be the assigned recipient (verified by shipment ID)
  - Can only confirm when shipment status is "Delivered"
  - Confirmation is recorded on blockchain with signature
- ✅ **Shipment Viewing**
  - View shipments assigned to them
  - View shipment details and QR codes
  - Track blockchain history of their shipments
- ✅ **General Access** (Shared with all authenticated users)
  - View dashboard
  - View blockchain explorer
  - View smart contracts
  - Manage own profile

**UI Access**:
- Dashboard
- Shipments (list, detail, filtered to show their shipments)
- Confirm Delivery button (only for assigned shipments in "Delivered" status)
- Blockchain Explorer
- Smart Contracts
- My Profile

**API Endpoints** (Recipient-specific):
- `POST /api/shipments/{id}/confirm-delivery` - Confirm delivery (only assigned recipient)

**Workflow**:
1. Recipient receives notification that shipment is "Delivered"
2. Recipient scans QR code or accesses shipment detail page
3. Recipient confirms delivery (changes status to "Confirmed")
4. Delivery confirmation is recorded on blockchain
5. Smart contract validates confirmation (verifies recipient identity, timeframe)

**Authorization Logic**:
```csharp
// Only the assigned recipient can confirm delivery
if (shipment.AssignedRecipient != recipientId)
{
    throw new UnauthorizedException("Only the assigned recipient can confirm delivery");
}
```

**Code Reference**: `src/BlockchainAidTracker.Services/Services/ShipmentService.cs:246` - Recipient validation

---

### 4. Validator

**Purpose**: Operates a validator node for Proof-of-Authority (PoA) consensus.

**Capabilities**:
- ✅ **Consensus Operations** (Shared with Administrator)
  - View consensus status
  - View active validators
  - Manually create blocks (via API or UI)
  - Participate in automated block creation (background service)
- ✅ **Validator Node Operations**
  - Validator account is linked to a Validator entity (separate from User)
  - Validators sign blocks with their cryptographic keys
  - Round-robin selection for block proposer role
  - Priority-based ordering for consensus participation
- ✅ **General Access** (Shared with all authenticated users)
  - View blockchain explorer
  - View smart contracts
  - View dashboard
  - Manage own profile

**UI Access**:
- Dashboard
- Shipments (list, detail - read-only)
- Blockchain Explorer
- Smart Contracts
- **Consensus Dashboard** (Validator and Administrator)
- My Profile

**API Endpoints** (Validator-specific):
- `POST /api/consensus/create-block` - Manually create block (Administrator, Validator)
- `POST /api/consensus/validate-block/{index}` - Validate block (Administrator, Validator)
- `GET /api/consensus/validators` - Get active validators
- `GET /api/consensus/status` - View consensus status

**Validator Entity** (Separate from User):
Validators have a dedicated `Validator` entity with:
- Unique name and public key
- Encrypted private key (password-protected)
- Priority for block proposer selection
- Network address for communication
- Activity status (active/inactive)
- Statistics (blocks created, last block timestamp)

**Important Note**:
- The **User role "Validator"** allows a user to access the Consensus Dashboard in the UI
- The **Validator entity** (in database) represents an actual validator node in the PoA consensus
- A User with Validator role can operate/monitor validators, but the actual consensus is performed by Validator entities

**Code Reference**:
- `src/BlockchainAidTracker.Api/Controllers/ConsensusController.cs:102,200` - Role authorization
- `src/BlockchainAidTracker.Services/Services/ProofOfAuthorityConsensusEngine.cs` - Consensus logic
- `src/BlockchainAidTracker.Core/Models/Validator.cs` - Validator entity

---

### 5. Donor

**Purpose**: Individual or organization that funds humanitarian aid shipments.

**Current Implementation Status**: ⚠️ **Limited Functionality**

**Defined Capabilities** (Database Schema):
- Shipment entity has `DonorPublicKey` field to track which donor funded a shipment
- Repository method `GetByDonorAsync()` exists to query shipments by donor

**Actual Capabilities** (Current):
- ✅ **General Access** (Shared with all authenticated users)
  - View dashboard
  - View all shipments (not filtered to their donations)
  - View blockchain explorer
  - View smart contracts
  - Manage own profile

**Missing Functionality** (Not Yet Implemented):
- ❌ No UI to assign donors to shipments during creation
- ❌ No filtering to show only shipments funded by specific donor
- ❌ No donation tracking or financial features
- ❌ No donor-specific dashboard with funding statistics
- ❌ No blockchain transactions recording donor contributions

**UI Access**:
- Dashboard (same as other authenticated users)
- Shipments (list, detail - no donor-specific filtering)
- Blockchain Explorer
- Smart Contracts
- My Profile

**API Endpoints**:
- No donor-specific endpoints currently implemented
- Available repository method: `IShipmentRepository.GetByDonorAsync(donorPublicKey)` (not exposed via API)

**Future Enhancements Needed**:
1. Add donor assignment during shipment creation
2. Create donor dashboard with funding statistics
3. Implement shipment filtering by donor
4. Add blockchain transactions for donation records
5. Financial tracking and reporting features

**Code Reference**:
- `src/BlockchainAidTracker.Core/Models/Shipment.cs:71` - DonorPublicKey field
- `src/BlockchainAidTracker.DataAccess/Repositories/ShipmentRepository.cs:66` - GetByDonorAsync method

---

### 6. LogisticsPartner

**Purpose**: Transportation and logistics company handling shipment delivery.

**Current Implementation Status**: ❌ **Not Implemented**

**Intended Capabilities** (Future):
- Update shipment location during transit
- Record delivery milestones on blockchain
- Manage transportation fleet
- Update shipping status (InTransit → Delivered)
- Track delivery performance metrics

**Current Capabilities**:
- ✅ **General Access** (Shared with all authenticated users)
  - View dashboard
  - View shipments
  - View blockchain explorer
  - View smart contracts
  - Manage own profile

**Missing Functionality** (Not Yet Implemented):
- ❌ No logistics-specific UI or API endpoints
- ❌ No location tracking features
- ❌ No delivery milestone recording
- ❌ No transportation management
- ❌ No logistics-specific blockchain transactions
- ❌ No integration with GPS/IoT devices

**UI Access**:
- Same as general authenticated users (no special features)

**API Endpoints**:
- None currently implemented

**Future Enhancements Needed**:
1. Create logistics dashboard
2. Implement location tracking and updates
3. Add delivery milestone recording
4. Create blockchain transactions for logistics events
5. Integrate with GPS tracking systems
6. Add transportation fleet management
7. Performance analytics and reporting

**Code Reference**:
- `src/BlockchainAidTracker.Core/Models/UserRole.cs:26` - Role definition only

---

## Summary Table

| Role | Create Shipments | Update Status | Confirm Delivery | Consensus Access | User Management | Validator Management | Status |
|------|-----------------|---------------|------------------|------------------|-----------------|---------------------|---------|
| **Administrator** | ✅ | ✅ | ✅ | ✅ | ✅ (Exclusive) | ✅ (Exclusive) | Fully Implemented |
| **Coordinator** | ✅ (Exclusive) | ✅ | ❌ | ❌ | ❌ | ❌ | Fully Implemented |
| **Recipient** | ❌ | ❌ | ✅ (Exclusive) | ❌ | ❌ | ❌ | Fully Implemented |
| **Validator** | ❌ | ❌ | ❌ | ✅ (Shared) | ❌ | ❌ | Fully Implemented |
| **Donor** | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ⚠️ Limited (DB fields exist) |
| **LogisticsPartner** | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ Not Implemented |

---

## Typical User Workflows

### Complete Shipment Lifecycle

1. **Coordinator** creates shipment:
   - `POST /api/shipments` with origin, destination, items, recipient
   - System generates QR code
   - Blockchain transaction: `SHIPMENT_CREATED`
   - Status: `Created`

2. **Smart Contract** auto-validates shipment:
   - If shipment has items, auto-validate
   - Status: `Validated`

3. **Coordinator** updates status to InTransit:
   - `PUT /api/shipments/{id}/status` with `InTransit`
   - Blockchain transaction: `STATUS_UPDATED`
   - Status: `InTransit`

4. **Coordinator** updates status to Delivered:
   - `PUT /api/shipments/{id}/status` with `Delivered`
   - Blockchain transaction: `STATUS_UPDATED`
   - Status: `Delivered`

5. **Recipient** confirms delivery:
   - `POST /api/shipments/{id}/confirm-delivery`
   - Smart contract validates recipient identity and timeframe
   - Blockchain transaction: `DELIVERY_CONFIRMED`
   - Status: `Confirmed`

6. **Validator** creates block (automated or manual):
   - Background service runs every 30 seconds
   - Selects next validator using round-robin
   - Validator signs block with private key
   - Block added to blockchain
   - All pending transactions included in block

### User Management Workflow

1. **New user** registers:
   - `POST /api/authentication/register`
   - Default role: `Recipient`
   - System generates ECDSA key pair
   - Private key encrypted with user password

2. **Administrator** assigns role:
   - `POST /api/users/assign-role`
   - Changes user role (Coordinator, Validator, etc.)
   - User gains access to role-specific features

3. **User** accesses role-specific features:
   - Coordinator: Can now create shipments
   - Validator: Can now access consensus dashboard

---

## Authentication & Authorization

### JWT Token Claims

All authenticated users receive a JWT token with the following claims:
- `sub` (Subject) - User ID
- `unique_name` - Username
- `email` - Email address
- `given_name` - First name
- `family_name` - Last name
- `role` - User role (single role per user)

### Role-Based Authorization

**Controller Level** (API):
```csharp
[Authorize] // Requires authentication
[Authorize(Roles = "Administrator")] // Requires specific role
[Authorize(Roles = "Administrator,Validator")] // Requires one of multiple roles
```

**Component Level** (Blazor UI):
```razor
<AuthorizeView Roles="Coordinator">
    <Authorized>
        <!-- Only shown to Coordinators -->
    </Authorized>
</AuthorizeView>
```

**Service Level** (Business Logic):
```csharp
if (coordinator.Role != UserRole.Coordinator && coordinator.Role != UserRole.Administrator)
{
    throw new UnauthorizedException("Only coordinators and administrators can create shipments");
}
```

---

## Blockchain Integration by Role

### Transactions Signed by Each Role

**Coordinator**:
- `SHIPMENT_CREATED` - When creating new shipment
- `STATUS_UPDATED` - When updating shipment status

**Recipient**:
- `DELIVERY_CONFIRMED` - When confirming delivery

**Validator** (Validator Entity):
- Block signatures - When creating blocks in PoA consensus

**All Transactions**:
- Signed with user's ECDSA private key (decrypted from encrypted storage)
- Verified by blockchain before acceptance
- Immutable once included in a block

---

## Security Considerations

### Private Key Management

- All users have ECDSA key pairs generated at registration
- Private keys encrypted with AES-256 using user password (PBKDF2, 10,000 iterations)
- Private keys decrypted and stored in-memory during active session
- Keys used to sign blockchain transactions
- For production: Consider using Azure Key Vault, AWS KMS, or HSM

### Role-Based Access Control

- Enforced at three levels: API controller, service layer, UI components
- JWT tokens include role claim
- Role cannot be self-assigned (only Administrator can assign roles)
- Single role per user (no multi-role support)

### Transaction Validation

- All blockchain transactions must have valid ECDSA signatures
- Shipment operations verify user has permission (e.g., assigned recipient for delivery)
- Status transitions follow defined state machine (prevent invalid transitions)
- Smart contracts enforce business rules (e.g., delivery timeframe validation)

---

## Recommendations for Future Development

### Donor Role Enhancement
1. Add `DonorId` to `CreateShipmentRequest` to assign donors during creation
2. Create donor dashboard showing funded shipments
3. Implement `GET /api/shipments/by-donor/{donorId}` endpoint
4. Add blockchain transactions for donation records
5. Create financial reporting features

### LogisticsPartner Role Implementation
1. Create logistics dashboard UI
2. Implement location tracking endpoints
3. Add delivery milestone recording
4. Create blockchain transactions for transit events
5. Integrate GPS/IoT device data
6. Build transportation fleet management

### Multi-Role Support (Optional)
- Allow users to have multiple roles (e.g., Coordinator + Donor)
- Modify JWT to include roles array instead of single role
- Update authorization logic to handle role arrays

### Advanced Features
1. Real-time notifications with SignalR
2. Mobile app with .NET MAUI
3. QR code scanning with camera
4. Advanced analytics dashboards
5. Audit logging for all operations
6. Rate limiting and abuse prevention

---

## Conclusion

The current implementation provides **fully functional roles** for:
- ✅ Administrator (complete system management)
- ✅ Coordinator (shipment creation and management)
- ✅ Recipient (delivery confirmation)
- ✅ Validator (consensus operations)

**Partially implemented**:
- ⚠️ Donor (database schema exists, but no UI/API features)

**Not implemented**:
- ❌ LogisticsPartner (role defined but no functionality)

The foundation is solid for extending Donor and LogisticsPartner roles with specific features as the project evolves.
