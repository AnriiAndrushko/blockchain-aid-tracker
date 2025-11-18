# User Roles Documentation

## Overview

The Blockchain Aid Tracker system implements a role-based access control (RBAC) system with six distinct user roles, each with specific responsibilities in the humanitarian aid supply chain.

## Role Definitions

### 1. Administrator
**Purpose**: System administration and full control

**Capabilities**:
- Full access to all system features
- User management (create, update, deactivate, assign roles)
- Validator management (register, activate, deactivate)
- Shipment creation and status updates
- Consensus operations and manual block creation
- View all blockchain data and smart contracts
- Access to admin dashboard with system-wide statistics

**Backend Permissions**:
- Can create shipments (ShipmentService)
- Can update any shipment status
- Can manage users (UserService)
- Can manage validators (ValidatorService)
- Can perform consensus operations (ConsensusController)

**UI Access**:
- Dashboard
- All shipment operations
- User Management page
- Validator Management page
- Consensus Dashboard
- Blockchain Explorer
- Smart Contracts viewer
- User Profile

---

### 2. Coordinator
**Purpose**: Create and manage humanitarian aid shipments

**Capabilities**:
- Create new shipments with recipient assignment
- Update shipment status through the supply chain
- View all shipments
- Track shipment progress on blockchain
- Generate and view QR codes for shipments
- View blockchain explorer and smart contracts

**Backend Permissions**:
- Can create shipments (ShipmentService.CreateShipmentAsync)
- Can update shipment status
- Can view all shipments
- Can view user profiles (limited)

**UI Access**:
- Dashboard with shipment statistics
- Create Shipment page
- Shipments List (all shipments)
- Shipment Detail view with status update
- Blockchain Explorer
- Smart Contracts viewer
- User Profile

**Typical Workflow**:
1. Create new shipment with items and recipient
2. Update status to "Validated" after verification
3. Mark as "InTransit" when logistics partner begins delivery
4. Update to "Delivered" when received at destination
5. Monitor recipient confirmation

---

### 3. LogisticsPartner
**Purpose**: Handle transportation and delivery of aid shipments

**Capabilities**:
- View assigned shipments for transportation
- Update shipment status during transit
- Mark shipments as "InTransit" when starting delivery
- Update status to "Delivered" when arriving at destination
- View shipment details and QR codes
- Track delivery progress on blockchain

**Backend Permissions**:
- Can update shipment status (especially InTransit and Delivered states)
- Can view shipments
- Can view blockchain history for shipments

**UI Access**:
- Logistics Dashboard (shipments in transit, assigned shipments)
- Shipments List (filtered for relevant shipments)
- Shipment Detail view with status update capabilities
- Blockchain Explorer
- User Profile

**Typical Workflow**:
1. View shipments assigned for transportation
2. Update status to "Validated" if quality check needed
3. Mark as "InTransit" when beginning delivery
4. Update location or status during transportation
5. Mark as "Delivered" when arriving at destination

---

### 4. Recipient
**Purpose**: Receive and confirm delivery of humanitarian aid

**Capabilities**:
- View shipments assigned to them
- Confirm delivery of received shipments
- Scan QR codes for verification (simulated)
- View blockchain history of their shipments
- Track shipment progress

**Backend Permissions**:
- Can view shipments assigned to them
- Can confirm delivery only for their assigned shipments (ShipmentService.ConfirmDeliveryAsync)
- Cannot create or update shipments

**UI Access**:
- Dashboard with assigned shipment statistics
- Shipments List (filtered to assigned shipments)
- Shipment Detail view
- Delivery Confirmation (for assigned shipments only)
- Blockchain Explorer
- User Profile

**Typical Workflow**:
1. View incoming shipments
2. Track shipment status as it moves through supply chain
3. Receive physical delivery
4. Scan QR code (optional)
5. Confirm delivery in system (triggers blockchain transaction)

---

### 5. Donor
**Purpose**: Fund humanitarian aid and monitor transparency

**Capabilities**:
- View all shipments in the system (transparency)
- Track shipment progress through supply chain
- View blockchain history for verification
- See delivery confirmations and statistics
- Monitor smart contract executions
- **Read-only access** - cannot modify any data

**Backend Permissions**:
- Can view all shipments (read-only)
- Can view blockchain data
- Can view smart contracts
- Cannot create or update any data

**UI Access**:
- Donor Dashboard with system-wide statistics
- Shipments List (all shipments, read-only)
- Shipment Detail view (read-only, no actions)
- Blockchain Explorer (full transparency)
- Smart Contracts viewer
- User Profile

**Typical Workflow**:
1. View funded shipments or all system shipments
2. Monitor delivery progress
3. Verify blockchain transactions for transparency
4. Review delivery confirmations
5. Track aid effectiveness through statistics

---

### 6. Validator
**Purpose**: Operate validator nodes for Proof-of-Authority consensus

**Capabilities**:
- Participate in PoA consensus mechanism
- Validate and sign blocks
- View consensus status and validator statistics
- Monitor blockchain integrity
- Create blocks manually (when assigned as proposer)

**Backend Permissions**:
- Can view validator information
- Can access consensus operations
- Can validate blocks
- Can create blocks (when selected as proposer)

**UI Access**:
- Dashboard
- Consensus Dashboard (view status, create blocks)
- Validator information
- Blockchain Explorer
- User Profile

**Typical Workflow**:
1. Monitor consensus status
2. Wait for block proposer selection (round-robin)
3. Validate transactions and create blocks when selected
4. Sign blocks with validator private key
5. Monitor blockchain validity

---

## Role-Based Access Control Matrix

| Feature | Administrator | Coordinator | LogisticsPartner | Recipient | Donor | Validator |
|---------|---------------|-------------|------------------|-----------|-------|-----------|
| Create Shipment | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| Update Shipment Status | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| Confirm Delivery | ✅ | ✅ | ❌ | ✅ (own only) | ❌ | ❌ |
| View All Shipments | ✅ | ✅ | ✅ | ❌ | ✅ (read-only) | ✅ |
| View Assigned Shipments | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| User Management | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Validator Management | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Consensus Operations | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ |
| View Blockchain | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| View Smart Contracts | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

---

## Shipment Status Transitions by Role

### Who Can Perform Each Transition:

- **Created → Validated**: Coordinator, Administrator, LogisticsPartner (after quality check)
- **Validated → InTransit**: Coordinator, Administrator, LogisticsPartner (when starting delivery)
- **InTransit → Delivered**: Coordinator, Administrator, LogisticsPartner (upon arrival)
- **Delivered → Confirmed**: Recipient (assigned only), Administrator

---

## Implementation Status

### ✅ Fully Implemented:
- Administrator (100% complete)
- Coordinator (100% complete)
- Recipient (100% complete)
- Validator (100% complete)

### 🔄 Partially Implemented:
- LogisticsPartner (backend implemented, UI needed)
- Donor (backend read access works, dedicated UI needed)

### 🎯 Next Steps:
1. Create LogisticsPartner dashboard and shipment management UI
2. Create Donor read-only dashboard with transparency focus
3. Add role-specific filtering and actions in existing UI components
4. Write comprehensive integration tests for all roles

---

## Security Considerations

- All roles require authentication
- Role-based authorization enforced at both API and UI levels
- Sensitive operations (user management, validator management) restricted to Administrator only
- Shipment confirmation enforced to assigned recipient only
- Transaction signing requires user's private key (decrypted with password)
- All blockchain operations are immutable and cryptographically verified
