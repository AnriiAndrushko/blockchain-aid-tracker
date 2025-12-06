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

**Authentication Endpoints (5 endpoints):**
- `POST /api/authentication/register` - Register new user with encrypted private key
- `POST /api/authentication/login` - Login and get JWT tokens (private key decrypted for session)
- `POST /api/authentication/refresh-token` - Refresh access token
- `POST /api/authentication/logout` - Logout (requires authentication)
- `GET /api/authentication/validate` - Validate current token (requires authentication)

**Shipment Endpoints (6 endpoints):**
- `POST /api/shipments` - Create new shipment (Coordinator only, creates blockchain transaction)
- `GET /api/shipments` - List all shipments with optional filtering
- `GET /api/shipments/{id}` - Get shipment details
- `PUT /api/shipments/{id}/status` - Update shipment status (creates blockchain transaction)
- `POST /api/shipments/{id}/confirm-delivery` - Confirm delivery (Recipient only, blockchain transaction)
- `GET /api/shipments/{id}/history` - Get blockchain transaction history
- `GET /api/shipments/{id}/qrcode` - Get shipment QR code as PNG image

**User Management Endpoints (7 endpoints):**
- `GET /api/users/profile` - Get current user's profile (requires authentication)
- `PUT /api/users/profile` - Update current user's profile (requires authentication)
- `GET /api/users/{id}` - Get user by ID (Admin/Coordinator or own profile)
- `GET /api/users` - List all users with optional role filter (Admin only)
- `POST /api/users/assign-role` - Assign role to user (Admin only)
- `POST /api/users/{id}/deactivate` - Deactivate user account (Admin only)
- `POST /api/users/{id}/activate` - Activate user account (Admin only)

**Blockchain Query Endpoints (5 endpoints):**
- `GET /api/blockchain/chain` - Get complete blockchain with all blocks
- `GET /api/blockchain/blocks/{index}` - Get specific block by index
- `GET /api/blockchain/transactions/{id}` - Get transaction details by ID
- `POST /api/blockchain/validate` - Validate entire blockchain integrity
- `GET /api/blockchain/pending` - Get pending transactions awaiting block creation

**Smart Contract Endpoints (4 endpoints):**
- `GET /api/contracts` - Get all deployed smart contracts
- `GET /api/contracts/{contractId}` - Get specific contract details
- `GET /api/contracts/{contractId}/state` - Get contract state
- `POST /api/contracts/execute` - Execute contract for a transaction (requires authentication)

**Validator Management Endpoints (6 endpoints):**
- `POST /api/validators` - Register new validator with key pair generation (Admin only)
- `GET /api/validators` - List all validators (Admin/Validator roles)
- `GET /api/validators/{id}` - Get validator by ID (Admin/Validator roles)
- `PUT /api/validators/{id}` - Update validator details (Admin only)
- `POST /api/validators/{id}/activate` - Activate validator (Admin only)
- `POST /api/validators/{id}/deactivate` - Deactivate validator (Admin only)
- `GET /api/validators/next` - Get next validator for block creation (consensus use)

**Consensus Endpoints (4 endpoints):** NEWEST
- `GET /api/consensus/status` - Get consensus status with chain information
- `POST /api/consensus/create-block` - Manually create new block (Admin/Validator only)
- `POST /api/consensus/validate-block/{index}` - Validate block by consensus rules (Admin/Validator only)
- `GET /api/consensus/validators` - Get all active validators

**Background Services:**
- `BlockCreationBackgroundService` - Automated block creation every 30 seconds (configurable) NEW

**System Endpoints:**
- `GET /health` - Health check endpoint with database monitoring

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

#### Troubleshooting Database Migrations

**Error: "SQLite Error 1: 'table already exists'"**

This occurs when the database schema is out of sync with the migration history. To fix:

```bash
# Option 1: Delete the database and reapply migrations (RECOMMENDED for development)
# This will reset all data but ensure a clean state
rm src/BlockchainAidTracker.DataAccess/blockchain-aid-tracker.db
dotnet ef database update --project src/BlockchainAidTracker.DataAccess

# Option 2: Remove last migration and recreate (if you just added a migration)
dotnet ef migrations remove --project src/BlockchainAidTracker.DataAccess
dotnet ef migrations add YourMigrationName --project src/BlockchainAidTracker.DataAccess
dotnet ef database update --project src/BlockchainAidTracker.DataAccess

# Option 3: Drop and recreate database (for production with data preservation)
dotnet ef database drop --project src/BlockchainAidTracker.DataAccess
dotnet ef database update --project src/BlockchainAidTracker.DataAccess
```

### Docker

```bash
# Build and run with Docker Compose
docker compose up --build
```

## Project Structure

```
blockchain-aid-tracker/
├── src/                                    # Source code
│   ├── BlockchainAidTracker.Core/         # Domain models and interfaces ✅
│   ├── BlockchainAidTracker.Blockchain/   # Blockchain engine (with persistence) ✅
│   ├── BlockchainAidTracker.Cryptography/ # Cryptographic utilities ✅
│   ├── BlockchainAidTracker.DataAccess/   # Entity Framework Core ✅
│   ├── BlockchainAidTracker.Services/     # Business logic (8 services + key mgmt) ✅
│   ├── BlockchainAidTracker.SmartContracts/ # Smart contract framework ✅
│   ├── BlockchainAidTracker.Api/          # Web API (auth + shipment + user mgmt + blockchain + validators) ✅
│   └── BlockchainAidTracker.Web/          # Blazor Web UI (16 pages, role-based access) ✅
├── tests/                                  # Test projects
│   └── BlockchainAidTracker.Tests/        # 594 tests (487 unit + 107 integration) ✅
│       ├── Blockchain/                    # 61 blockchain tests (core + persistence) ✅
│       ├── Cryptography/                  # 31 crypto tests
│       ├── Models/                        # 75 model tests
│       ├── DataAccess/                    # 71 database tests
│       ├── Services/                      # 159 services tests (incl. consensus & background service) ✅
│       ├── SmartContracts/                # 90 smart contract tests ✅
│       ├── Integration/                   # 107 API integration tests (auth + shipments + users + blockchain + consensus) ✅
│       └── Infrastructure/                # Test helpers & builders
├── blockchain-aid-tracker/                # Demo console app
├── docs/                                   # Documentation
└── CLAUDE.md                               # Detailed implementation roadmap
```

See [CLAUDE.md](CLAUDE.md) for detailed architecture and implementation status.

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
- ✅ **Complete Blazor Web UI with 16 pages** NEWEST
- ✅ **Role-based UI with different views for all 6 roles** NEWEST
- ✅ **Shipment status update and delivery confirmation modals** NEWEST
- ✅ **User Management page for administrators** NEWEST
- ✅ **Validator Management page for PoA consensus** NEWEST
- ✅ **Consensus Dashboard with manual block creation** NEWEST
- ✅ **Smart Contracts viewer with state inspection** NEWEST
- ✅ **User Profile management for all users** NEWEST
- ✅ **Blockchain Explorer with block and transaction details** NEWEST
- ✅ **Responsive Bootstrap 5 UI with Bootstrap Icons**
- ✅ **Customer/Supplier Payment System - COMPLETE** (2025-12-06)
  - ✅ Supplier entity with verification workflow (Pending/Verified/Rejected)
  - ✅ SupplierShipment junction entity for goods tracking
  - ✅ PaymentRecord entity for payment lifecycle
  - ✅ 6 new transaction types (SupplierRegistered, SupplierVerified, SupplierUpdated, PaymentInitiated, PaymentReleased, PaymentFailed)
  - ✅ Services layer: SupplierService + PaymentService (24 business methods)
  - ✅ Repositories: SupplierRepository, SupplierShipmentRepository, PaymentRepository (23 query methods)
  - ✅ PaymentReleaseContract smart contract (automatic payment on shipment confirmation)
  - ✅ API endpoints: SupplierController (7) + PaymentController (7) = 14 endpoints total
  - ✅ Complete test coverage: 91 tests (34 service + 12 database + 25 integration + 20 integration)
- ✅ **LogisticsPartner Location Tracking System - COMPLETE** (2025-12-06)
  - ✅ ShipmentLocation and DeliveryEvent entities
  - ✅ LogisticsPartnerService with 7 tracking methods
  - ✅ LogisticsPartnerController with 7 REST API endpoints
  - ✅ Blazor UI: LogisticsPartnerShipments list page
  - ✅ Blazor UI: LogisticsPartnerShipmentDetail with location history and events
  - ✅ Blazor UI: UpdateLocation modal with GPS coordinate validation
  - ✅ Blazor UI: ReportDeliveryIssue modal with priority levels
  - ✅ Blazor UI: ShipmentTrackingTimeline reusable component
  - ✅ Complete test coverage: 66 tests (34 service + 12 database + 20 integration)

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

## Development Roadmap

The project follows a comprehensive implementation roadmap detailed in [CLAUDE.md](CLAUDE.md). Major milestones:

| Milestone | Status | Progress |
|-----------|--------|----------|
| 1. Core Architecture Setup | ✅ Complete | Database, repositories, models |
| 2. Blockchain Core Implementation | ✅ Complete | Engine, real signatures, validation, persistence |
| 3. **Cryptographic Key Management** | ✅ Complete | AES-256 encryption, ECDSA signing |
| 4. Testing Infrastructure | ✅ Complete | 594 tests (487 unit + 107 integration) |
| 5. User Management System | ✅ Complete | Authentication, JWT, key management, APIs |
| 6. Supply Chain Operations | ✅ Complete | Shipment services, QR codes, lifecycle |
| 7. Services Layer | ✅ Complete | 8 services, DTOs, validation, encryption |
| 8. API Endpoints | ✅ Complete | Auth + Shipment + User Mgmt + Blockchain + Smart Contracts + Validators + Consensus, Swagger UI |
| 9. **Smart Contracts** | ✅ Complete | Framework, DeliveryVerification, ShipmentTracking |
| 10. **Smart Contract API Integration** | ✅ Complete | Auto-execution, API endpoints |
| 11. **Validator Node System** | ✅ Complete | Validator management, round-robin selection |
| 12. **Consensus Engine** | ✅ Complete | PoA block creation, validator signature validation |
| 13. **Consensus API Integration** | ✅ Complete | 4 endpoints, automated background service |
| 14. **Blockchain Persistence** | ✅ Complete | File-based storage, automatic save/load, backups |
| 15. **Web Application UI** | ✅ Complete | 16 Blazor pages, role-based access, responsive design |

**Legend:** ✅ Complete | 🔨 In Progress | 📋 Planned

## Testing

The project has a comprehensive test suite with **716 passing tests** (100% success rate):

### Test Coverage

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

### Test Categories

| Category | Tests | Description |
|----------|-------|-------------|
| **Services** | 193 | Business logic, key management, authentication, shipment lifecycle, **LogisticsPartner (34)**, automated block creation |
| **SmartContracts** | 90 | Contract engine, delivery verification, shipment tracking, payment release |
| **Models** | 75 | Domain entities (User, Shipment, Validator, Block, Transaction) |
| **Database** | 71 | Repository tests (9 repositories) with in-memory DB, automatic cleanup |
| **Blockchain** | 61 | Chain validation, block creation, signature verification, **persistence (save/load/backup)** |
| **Cryptography** | 31 | SHA-256 hashing, ECDSA signatures, key generation |
| **Integration** | 127 | API endpoint tests (auth + shipments + user mgmt + blockchain + contracts + validators + consensus + **LogisticsPartner (20)**), real cryptographic validation |

### Test Infrastructure Features

- ✅ **Isolated databases** - Each test gets a unique in-memory database (unit & integration)
- ✅ **Automatic cleanup** - Database state reset after every test
- ✅ **Real cryptographic validation** - All tests use actual ECDSA signatures, no mocks
- ✅ **Fluent builders** - `UserBuilder`, `ShipmentBuilder`, `ValidatorBuilder` for easy test data
- ✅ **Moq framework** - Mocking dependencies for service layer tests
- ✅ **WebApplicationFactory** - Integrated API testing with real HTTP requests
- ✅ **Comprehensive coverage** - Success paths, error handling, edge cases
- ✅ **Zero cross-test contamination** - Tests can run in parallel
- ✅ **Environment separation** - Test-specific configuration (appsettings.Testing.json)
- ✅ **Blockchain validation enabled** - Tests verify transaction signatures are cryptographically valid

**Example:**
```csharp
var user = TestData.CreateUser()
    .WithUsername("alice")
    .AsCoordinator()
    .Build();

// Database automatically cleaned up after test
```

## Documentation

- [CLAUDE.md](CLAUDE.md) - Complete implementation roadmap and development guidelines
- API Documentation - Available via Swagger at `/swagger` when running the API

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