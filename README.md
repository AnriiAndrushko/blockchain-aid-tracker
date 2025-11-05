# Blockchain Aid Tracker

A .NET 9.0 blockchain-based humanitarian aid supply chain tracking system demonstrating decentralized control, transparency, and Proof-of-Authority consensus.

## Project Status

**Foundation, Business Logic, Authentication, Shipment, User Management, Blockchain Query APIs, Smart Contract Framework, Smart Contract API Integration, Validator Node System, Proof-of-Authority Consensus Engine, and Cryptographic Key Management Complete** - The core blockchain engine with real ECDSA signature validation, PoA consensus, smart contracts, smart contract API, validator management, cryptography services, key management, data access layer, services layer, and API endpoints are fully implemented and tested.

**Current Metrics:**
-  **556 tests passing** (100% success rate: 462 unit + 94 integration) NEW
-  **Proof-of-Authority Consensus Engine with automated block creation** NEW
-  Authentication, Shipment, User Management, Blockchain Query, Smart Contract & Validator API endpoints operational with Swagger UI
-  8 core business services fully implemented (including key management & validator service)
-  **Validator node system with 6 API endpoints**
-  **Smart contract framework with 2 built-in contracts (DeliveryVerification, ShipmentTracking)**
-  **Smart contract API integration with 4 endpoints (list, get, execute, get state)**
-  **Blockchain engine with real ECDSA signature validation ENABLED**
-  **AES-256 private key encryption with user passwords**
-  **Round-robin validator selection for block proposer (PoA consensus)**
-  JWT authentication with BCrypt password hashing
-  QR code generation for shipment tracking
-  Complete data access layer with EF Core
-  Repository pattern fully tested
-  Cryptographic services (SHA-256, ECDSA) with real signatures
-  Integration test infrastructure with WebApplicationFactory
-  All blockchain transactions cryptographically signed and validated

**Next:** Integrate consensus engine with API endpoints for automated block creation, then begin Blazor UI development

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

**Validator Management Endpoints (6 endpoints):** NEW
- `POST /api/validators` - Register new validator with key pair generation (Admin only)
- `GET /api/validators` - List all validators (Admin/Validator roles)
- `GET /api/validators/{id}` - Get validator by ID (Admin/Validator roles)
- `PUT /api/validators/{id}` - Update validator details (Admin only)
- `POST /api/validators/{id}/activate` - Activate validator (Admin only)
- `POST /api/validators/{id}/deactivate` - Deactivate validator (Admin only)
- `GET /api/validators/next` - Get next validator for block creation (consensus use)

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

**Note:** The database file (*.db, *.db-shm, *.db-wal) should NOT be committed to version control. These files are now excluded in `.gitignore`.

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
│   ├── BlockchainAidTracker.Blockchain/   # Blockchain engine ✅
│   ├── BlockchainAidTracker.Cryptography/ # Cryptographic utilities ✅
│   ├── BlockchainAidTracker.DataAccess/   # Entity Framework Core ✅
│   ├── BlockchainAidTracker.Services/     # Business logic (8 services + key mgmt) ✅
│   ├── BlockchainAidTracker.SmartContracts/ # Smart contract framework ✅
│   ├── BlockchainAidTracker.Api/          # Web API (auth + shipment + user mgmt + blockchain + validators) ✅
│   └── BlockchainAidTracker.Web/          # Blazor UI (referenced)
├── tests/                                  # Test projects
│   └── BlockchainAidTracker.Tests/        # 526 tests (432 unit + 94 integration) ✅
│       ├── Blockchain/                    # 42 blockchain tests
│       ├── Cryptography/                  # 31 crypto tests
│       ├── Models/                        # 53 model tests
│       ├── DataAccess/                    # 63 database tests
│       ├── Services/                      # 123 services tests
│       ├── SmartContracts/                # 90 smart contract tests ✅
│       ├── Integration/                   # 83 API integration tests (auth + shipments + users + blockchain) ✅
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
- ✅ Multiple user roles (Recipient, Donor, Coordinator, LogisticsPartner, Validator, Administrator)
- ✅ Blockchain-based shipment tracking with immutable audit trail
- ✅ QR code generation for shipment verification (Base64 and PNG)
- ✅ Shipment lifecycle management (Created → Validated → InTransit → Delivered → Confirmed)
- ✅ User profile management with role assignment
- ✅ Business logic services layer (8 services including key management & validator service) NEW
- ✅ Authentication REST API endpoints (register, login, refresh, logout, validate)
- ✅ **Shipment REST API endpoints (create, list, get, update, confirm, history, qrcode)**
- ✅ **User Management REST API endpoints (profile, update, get user, list, assign role, activate, deactivate)**
- ✅ **Blockchain Query REST API endpoints (chain, block, transaction, validate, pending)**
- ✅ **Validator Management REST API endpoints (register, list, get, update, activate, deactivate)** NEW
- ✅ JWT Bearer authentication middleware for ASP.NET Core
- ✅ Role-based authorization for API endpoints (Admin/Coordinator/Validator/User permissions)
- ✅ Swagger/OpenAPI documentation with JWT support
- ✅ Integration test infrastructure with WebApplicationFactory
- ✅ **Smart contract framework with execution engine**
- ✅ **DeliveryVerificationContract for delivery confirmation validation**
- ✅ **ShipmentTrackingContract for automated shipment lifecycle**
- ✅ **Validator node system with round-robin block proposer selection**
- ✅ **ECDSA key pair generation for validators**
- ✅ **Proof-of-Authority Consensus Engine with automated block creation** NEW
- ✅ **Block validation with validator signature verification** NEW
- ✅ **556 tests passing with real cryptographic signature validation** NEW

### In Progress 🔨
- 🔨 Consensus Engine API integration for automated block creation

### Planned 📋
- 📋 Multi-node validator network communication
- 📋 Peer-to-peer blockchain synchronization
- 📋 Real-time blockchain explorer UI
- 📋 Blazor web application interface
- 📋 Transparent donation tracking dashboard

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
| 2. Blockchain Core Implementation | ✅ Complete | Engine, real signatures, validation |
| 3. **Cryptographic Key Management** | ✅ Complete | AES-256 encryption, ECDSA signing |
| 4. Testing Infrastructure | ✅ Complete | 556 tests (462 unit + 94 integration) |
| 5. User Management System | ✅ Complete | Authentication, JWT, key management, APIs |
| 6. Supply Chain Operations | ✅ Complete | Shipment services, QR codes, lifecycle |
| 7. Services Layer | ✅ Complete | 8 services, DTOs, validation, encryption |
| 8. API Endpoints | ✅ Complete (95%) | Auth + Shipment + User Mgmt + Blockchain + Smart Contracts + Validators, Swagger UI |
| 9. **Smart Contracts** | ✅ Complete | Framework, DeliveryVerification, ShipmentTracking |
| 10. **Smart Contract API Integration** | ✅ Complete | Auto-execution, API endpoints |
| 11. **Validator Node System** | ✅ Complete | Validator management, round-robin selection |
| 12. **Consensus Engine** | ✅ Complete | PoA block creation, validator signature validation |
| 13. Consensus API Integration | 🔨 In Progress | Automated block creation endpoints |
| 14. Web Application UI | 📋 Planned | Blazor dashboard |

**Legend:** ✅ Complete | 🔨 In Progress | 📋 Planned

## Testing

The project has a comprehensive test suite with **556 passing tests** (100% success rate):

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
| **Services** | 123 | Business logic, key management, authentication, shipment lifecycle |
| **SmartContracts** | 90 | Contract engine, delivery verification, shipment tracking |
| **Models** | 75 | Domain entities (User, Shipment, Validator, Block, Transaction) |
| **Blockchain** | 72 | Chain validation, block creation, **PoA consensus engine**, signature verification |
| **Database** | 71 | Repository tests with in-memory DB, automatic cleanup |
| **Cryptography** | 31 | SHA-256 hashing, ECDSA signatures, key generation |
| **Integration** | 94 | API endpoint tests (auth + shipments + user mgmt + blockchain + contracts + validators), real cryptographic validation |

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