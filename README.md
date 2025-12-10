# Blockchain Aid Tracker

A .NET 9.0 blockchain-based humanitarian aid supply chain tracking system demonstrating decentralized control, transparency, and Proof-of-Authority consensus.

## ⚠️ Project Information

**This is a showcase/diploma project** demonstrating blockchain concepts and supply chain tracking architecture. It is **not intended for production use** and does not require full implementation of all features (such as complete payment processing, real banking integrations, etc.).

The project serves as a **proof-of-concept** to demonstrate:
- Blockchain technology fundamentals (blocks, transactions, signatures, consensus)
- Proof-of-Authority consensus mechanism
- Supply chain transparency and immutability
- Role-based authorization and smart contracts
- Integration of blockchain with traditional web architecture

**Focus areas**: Core blockchain functionality, consensus mechanisms, shipment tracking, user authentication, and UI/UX. Payment functionality is partially implemented as a domain model and service layer to demonstrate the concept.

## Project Status - MVP COMPLETE ✅

**All core features implemented and tested** - Complete end-to-end blockchain-based humanitarian aid tracking system with automatic payment processing. The system demonstrates a full supply chain workflow from shipment creation through delivery tracking to automated payment release upon confirmation.

**Current Metrics:**
-  **741 tests passing** (100% success rate) - **UPDATED 2025-12-06**
-  **Complete Blazor Web UI with 20 pages** including LogisticsPartner dashboard
-  **7 user roles fully implemented**: Administrator, Coordinator, Recipient, Donor, Validator, LogisticsPartner, Customer
-  **14 Payment System API endpoints**: SupplierController (7) + PaymentController (7)
-  **3 Smart Contracts deployed**: DeliveryVerification, ShipmentTracking, **PaymentRelease** (automatic payment on confirmation)
-  **LogisticsPartner System complete**: Backend + Blazor UI with location tracking, issue reporting, and delivery events
-  **Customer/Supplier Payment System complete**: Registration, verification, automatic payment via smart contract
-  **9 repositories with 35+ specialized query methods**
-  **10 services with 80+ business logic methods**
-  **Blockchain persistence** with automatic save/load and backup rotation
-  **Proof-of-Authority consensus** with round-robin validator selection
-  **Automated block creation** every 30 seconds
-  **Real ECDSA signature validation** for all blockchain transactions
-  **AES-256 private key encryption** with user passwords
-  **Complete integration test coverage** (152 integration tests)

**Complete Supply Chain Workflow:**
1. **Coordinator** creates shipment with items and assigns suppliers
2. **Admin** verifies suppliers for payment eligibility
3. **LogisticsPartner** updates location during transit with GPS tracking
4. **LogisticsPartner** reports delivery and any issues (priority-based)
5. **Recipient** confirms delivery receipt
6. **PaymentReleaseContract** automatically releases payment to verified suppliers
7. **All actions** recorded on immutable blockchain with audit trail

**Next Steps (Optional):** Consider implementing Donor UI for transparency, Blazor component tests (bUnit), real-time updates with SignalR, advanced analytics dashboard, or mobile app with .NET MAUI.

## Quick Start

### Prerequisites
- .NET 9.0 SDK
- Docker (optional, for containerized deployment)
- SQLite or PostgreSQL

### Build and Run

```bash
# Build the entire solution
dotnet build blockchain-aid-tracker.sln

# Run all tests
dotnet test

# Run the demo application (Database + Blockchain integration)
dotnet run --project blockchain-aid-tracker

# Run the API with Swagger UI (available at https://localhost:5001 or http://localhost:5000)
dotnet run --project src/BlockchainAidTracker.Api/BlockchainAidTracker.Api.csproj

# Run the Blazor Web UI (available at https://localhost:5003 or http://localhost:5002)
dotnet run --project src/BlockchainAidTracker.Web/BlockchainAidTracker.Web.csproj

# RECOMMENDED: Run both API and Web UI simultaneously
# Terminal 1:
dotnet run --project src/BlockchainAidTracker.Api/BlockchainAidTracker.Api.csproj

# Terminal 2 (after API is running):
dotnet run --project src/BlockchainAidTracker.Web/BlockchainAidTracker.Web.csproj
```

### Using the Blazor Web UI

1. **Start the API** (Terminal 1):
   ```bash
   dotnet run --project src/BlockchainAidTracker.Api/BlockchainAidTracker.Api.csproj
   ```
   Wait for "Now listening on: https://localhost:5001"

2. **Start the Web UI** (Terminal 2):
   ```bash
   dotnet run --project src/BlockchainAidTracker.Web/BlockchainAidTracker.Web.csproj
   ```
   Open your browser to the URL shown (typically https://localhost:5003)

3. **Register a New User:**
   - Click "Create an account" on the login page
   - Fill in your details (First name, Last name, Username, Email, Password)
   - Select a role (Coordinator, Recipient, Donor, or Logistics Partner)
   - Click "Create Account"
   - You'll be automatically logged in and redirected to the dashboard

4. **Explore Features:**
   - **Dashboard**: View statistics, recent shipments, and blockchain status
   - **Shipments**: Browse all shipments with filtering and search
   - **Create Shipment** (Coordinator role only): Create new aid shipments with items
   - **Shipment Details**: View detailed information, QR codes, blockchain history, update status, and confirm delivery
   - **Blockchain Explorer**: Browse blocks, view transactions, and verify hashes
   - **Smart Contracts**: View deployed contracts and their state
   - **User Profile**: View and edit your own profile information
   - **Consensus Dashboard** (Admin/Validator only): Monitor PoA consensus, view active validators, manually create blocks
   - **User Management** (Admin only): Manage users, assign roles, activate/deactivate accounts
   - **Validator Management** (Admin only): Register validators, manage priorities, activate/deactivate validator nodes

5. **Role-Based Access:**
   - **Administrator**: Full system access - user management, validator management, consensus control, all shipment operations
   - **Coordinator**: Can create shipments, update status, view all shipments
   - **Recipient**: Can confirm delivery of assigned shipments, view shipment details
   - **Donor**: Can view shipment transparency and blockchain history
   - **Validator**: Can access consensus dashboard, view validator information, manually create blocks
   - **Logistics Partner**: Can view shipments and blockchain information

**API Configuration:**
- The Web UI connects to the API at `https://localhost:5001` by default
- To change the API URL, edit `src/BlockchainAidTracker.Web/appsettings.json`:
  ```json
  {
    "ApiSettings": {
      "BaseUrl": "https://localhost:5001"
    }
  }
  ```
```

### API Endpoints

Visit the Swagger UI at the root URL when the API is running to test endpoints interactively. All blockchain transactions are signed with real ECDSA signatures and validated.

```

### Database Operations

```bash
# Apply migrations to create/update database
dotnet ef database update --project src/BlockchainAidTracker.DataAccess

# Create a new migration (after model changes)
dotnet ef migrations add MigrationName --project src/BlockchainAidTracker.DataAccess

# View migration list
dotnet ef migrations list --project src/BlockchainAidTracker.DataAccess
```

**Database file location:** `src/BlockchainAidTracker.DataAccess/blockchain-aid-tracker.db`

### Docker

```bash
# Build and run with Docker Compose
docker compose up --build
```


## Features

### Implemented ✅
- ✅ User authentication with JWT tokens (access + refresh)
- ✅ BCrypt password hashing for secure credentials (work factor: 12)
- ✅ **AES-256 private key encryption with user passwords (PBKDF2, 10000 iterations)**
- ✅ **Real ECDSA transaction signing with cryptographic verification**
- ✅ **Blockchain signature validation ENABLED - all transactions verified**
- ✅ Multiple user roles (Recipient, Donor, Coordinator, LogisticsPartner, Validator, Administrator, Customer) - 7 roles
- ✅ Blockchain-based shipment tracking with immutable audit trail
- ✅ QR code generation for shipment verification (Base64 and PNG)
- ✅ Shipment lifecycle management (Created → Validated → InTransit → Delivered → Confirmed)
- ✅ User profile management with role assignment
- ✅ Business logic services layer (8 services including key management & validator service)
- ✅ Authentication REST API endpoints (register, login, refresh, logout, validate)
- ✅ **Shipment REST API endpoints (create, list, get, update, confirm, history, qrcode)**
- ✅ **User Management REST API endpoints (profile, update, get user, list, assign role, activate, deactivate)**
- ✅ **Blockchain Query REST API endpoints (chain, block, transaction, validate, pending)**
- ✅ **Validator Management REST API endpoints (register, list, get, update, activate, deactivate)**
- ✅ JWT Bearer authentication middleware for ASP.NET Core
- ✅ Role-based authorization for API endpoints (Admin/Coordinator/Validator/User permissions)
- ✅ Swagger/OpenAPI documentation with JWT support
- ✅ Integration test infrastructure with WebApplicationFactory
- ✅ **Smart contract framework with execution engine**
- ✅ **DeliveryVerificationContract for delivery confirmation validation**
- ✅ **ShipmentTrackingContract for automated shipment lifecycle**
- ✅ **Validator node system with round-robin block proposer selection**
- ✅ **ECDSA key pair generation for validators**
- ✅ **Proof-of-Authority Consensus Engine with automated block creation**
- ✅ **Block validation with validator signature verification**
- ✅ **Consensus API with 4 endpoints for block operations**
- ✅ **Automated block creation background service (30 second intervals)**
- ✅ **Blockchain persistence with file-based JSON storage**
- ✅ **Automatic save after block creation and load on startup**
- ✅ **Backup file creation with configurable rotation**
- ✅ **594 tests passing with real cryptographic signature validation**
- ✅ **Complete Blazor Web UI with 16 pages**
- ✅ **Role-based UI with different views for all 6 roles**
- ✅ **Shipment status update and delivery confirmation modals**
- ✅ **User Management page for administrators** 
- ✅ **Validator Management page for PoA consensus** 
- ✅ **Consensus Dashboard with manual block creation** 
- ✅ **Smart Contracts viewer with state inspection** 
- ✅ **User Profile management for all users** 
- ✅ **Blockchain Explorer with block and transaction details** 
- ✅ **Responsive Bootstrap 5 UI with Bootstrap Icons**
- ✅ **LogisticsPartner Location Tracking System 

### Planned 📋
- 📋 Multi-node validator network communication (P2P)
- 📋 Peer-to-peer blockchain synchronization
- 📋 Real-time updates with SignalR for live blockchain monitoring
- 📋 Blazor component tests with bUnit
- 📋 Advanced analytics dashboard with charts and graphs

## Technology Stack

- .NET 9.0
- ASP.NET Core Web API
- Blazor Web App
- Entity Framework Core 9.0
- SQLite (development) / PostgreSQL (production)
- JWT Authentication
- BCrypt.Net (password hashing)
- QRCoder (QR code generation)
- Docker

## Testing

The project has a comprehensive test suite with **746 tests**

```bash
# Run all tests (unit + integration)
dotnet test

# Run specific test category
dotnet test --filter "FullyQualifiedName~Services"
dotnet test --filter "FullyQualifiedName~SmartContracts"
dotnet test --filter "FullyQualifiedName~DataAccess"
dotnet test --filter "FullyQualifiedName~Blockchain"
dotnet test --filter "FullyQualifiedName~Cryptography"
dotnet test --filter "FullyQualifiedName~Integration"
```

## Contributing

This is a prototype project for demonstrating blockchain concepts in humanitarian aid tracking. Contributions should follow the guidelines in CLAUDE.md.

## License

MIT License

Copyright (c) 2025 Andrii Andrushko

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.

## Contact

If you have any questions write on andry.i.andrushko@gmail.com