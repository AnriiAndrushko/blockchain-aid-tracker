# Blockchain Aid Tracker

A .NET 9.0 blockchain-based humanitarian aid supply chain tracking system demonstrating decentralized control, transparency, and Proof-of-Authority consensus.

## Project Status

**Foundation, Business Logic, Authentication, Shipment, User Management, Blockchain Query APIs, Smart Contract Framework, Smart Contract API Integration, Validator Node System, Proof-of-Authority Consensus Engine, Consensus API Integration, Automated Block Creation Background Service, Blockchain Persistence, Cryptographic Key Management, and Blazor Web UI Complete** - The core blockchain engine with real ECDSA signature validation, PoA consensus, automated block creation, blockchain persistence, smart contracts, smart contract API, validator management, cryptography services, key management, data access layer, services layer, all API endpoints, and full Blazor Web UI are fully implemented and tested.

**Current Metrics:**
-  **594 tests passing** (100% success rate: 487 unit + 107 integration)
-  **Complete Docker configuration with docker-compose** (API + Web UI with persistent volumes) NEWEST
-  **Complete Blazor Web UI with 16 pages** (auth, dashboard, shipments, users, validators, consensus, contracts, blockchain explorer)
-  **Full role-based UI behavior** (Administrator, Coordinator, Recipient, Donor, Validator, LogisticsPartner)
-  **Blockchain persistence with automatic save/load and backup rotation**
-  **Consensus API with 4 endpoints for block creation and validation**
-  **Automated background service creating blocks every 30 seconds**
-  Authentication, Shipment, User Management, Blockchain Query, Smart Contract, Validator & Consensus API endpoints operational with Swagger UI
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
-  **16 Blazor pages with authentication, role-based access, and responsive UI**

**Next:** Consider implementing Blazor component tests (bUnit), real-time updates with SignalR, advanced analytics, additional security features (rate limiting, audit logging), or mobile app development with .NET MAUI.

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

### Docker (Recommended for Quick Start)

#### Quick Start with Docker Compose

The easiest way to run the entire application stack (API + Web UI) with persistent data:

```bash
# Build and run all services (API + Web UI)
docker-compose up --build

# Run in detached mode (background)
docker-compose up -d --build

# View logs from all services
docker-compose logs -f

# View logs from a specific service
docker-compose logs -f api
docker-compose logs -f web

# Stop all services (keeps data)
docker-compose down

# Stop and remove all data (clean slate)
docker-compose down -v
```

#### Service URLs (Docker)

When running with Docker Compose, access the services at:
- **Web UI**: http://localhost:5002
- **API**: http://localhost:5000
- **Swagger UI**: http://localhost:5000/swagger

#### Production Configuration

For production use, set a strong JWT secret key:

```bash
# Set JWT secret key as environment variable (Linux/macOS)
export JWT_SECRET_KEY="your-very-secure-secret-key-at-least-32-characters-long"
docker-compose up -d --build

# Or edit docker-compose.yml and set JWT_SECRET_KEY directly
```

#### Data Persistence

Docker volumes automatically persist data between container restarts:
- **Database**: SQLite database stored in `api-database` volume
- **Blockchain**: JSON blockchain data stored in `api-blockchain` volume
- **Backups**: Blockchain backups stored in `api-blockchain` volume

#### Managing Docker Volumes

```bash
# List all volumes
docker volume ls

# Inspect the database volume
docker volume inspect blockchain-aid-tracker_api-database

# Backup database from container
docker cp blockchain-aid-tracker-api:/app/data/database/blockchain-aid-tracker.db ./backup.db

# Backup blockchain data from container
docker cp blockchain-aid-tracker-api:/app/data/blockchain ./blockchain-backup

# Restore database to container
docker cp ./backup.db blockchain-aid-tracker-api:/app/data/database/blockchain-aid-tracker.db

# Remove all volumes (WARNING: deletes all data)
docker-compose down -v
```

#### Individual Docker Commands

If you need to build or run services separately:

```bash
# Build API image
docker build -t blockchain-aid-tracker-api -f Dockerfile.api .

# Build Web UI image
docker build -t blockchain-aid-tracker-web -f Dockerfile.web .

# Run API container manually
docker run -p 5000:8080 \
  -v api-data:/app/data \
  -e JWT_SECRET_KEY="your-secret-key" \
  blockchain-aid-tracker-api

# Run Web UI container manually (requires API running)
docker run -p 5002:8080 \
  -e ApiSettings__BaseUrl=http://api:8080 \
  blockchain-aid-tracker-web
```

#### Docker Environment Variables

Customize the application behavior via environment variables in `docker-compose.yml`:

| Variable | Description | Default |
|----------|-------------|---------|
| `JWT_SECRET_KEY` | JWT signing key (min 32 chars) | `your-secret-key-min-32-characters-long-for-production-use` |
| `ASPNETCORE_ENVIRONMENT` | Environment mode | `Production` |
| `BlockchainPersistence__Enabled` | Enable blockchain persistence | `true` |
| `BlockchainPersistence__MaxBackupCount` | Max blockchain backups | `5` |
| `ConsensusSettings__BlockCreationIntervalSeconds` | Block creation interval | `30` |
| `ConsensusSettings__MinTransactionsPerBlock` | Min transactions per block | `1` |

#### Health Checks

Both containers include health checks:

```bash
# Check container health status
docker ps

# Check API health directly
curl http://localhost:5000/health

# Check Web UI health
curl http://localhost:5002/health
```

#### Troubleshooting Docker

**Port already in use:**
```bash
# Find process using port 5000
lsof -i :5000

# Kill the process
kill -9 <PID>

# Or change ports in docker-compose.yml
```

**Container won't start:**
```bash
# View container logs
docker-compose logs api
docker-compose logs web

# Restart containers
docker-compose restart

# Rebuild from scratch
docker-compose down -v
docker-compose up --build
```

**Database locked error:**
```bash
# Stop containers
docker-compose down

# Remove volumes
docker volume rm blockchain-aid-tracker_api-database

# Restart
docker-compose up --build
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
- ✅ Multiple user roles (Recipient, Donor, Coordinator, LogisticsPartner, Validator, Administrator)
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
- ✅ **Responsive Bootstrap 5 UI with Bootstrap Icons** NEWEST
- ✅ **Complete Docker configuration with multi-stage builds** NEWEST
- ✅ **Docker Compose orchestration for API + Web UI** NEWEST
- ✅ **Persistent Docker volumes for database and blockchain data** NEWEST
- ✅ **Container health checks and dependency management** NEWEST

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

The project has a comprehensive test suite with **594 passing tests** (100% success rate):

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
| **Services** | 159 | Business logic, key management, authentication, shipment lifecycle, **automated block creation** |
| **SmartContracts** | 90 | Contract engine, delivery verification, shipment tracking |
| **Models** | 75 | Domain entities (User, Shipment, Validator, Block, Transaction) |
| **Blockchain** | 61 | Chain validation, block creation, signature verification, **persistence (save/load/backup)** |
| **Database** | 71 | Repository tests with in-memory DB, automatic cleanup |
| **Cryptography** | 31 | SHA-256 hashing, ECDSA signatures, key generation |
| **Integration** | 107 | API endpoint tests (auth + shipments + user mgmt + blockchain + contracts + validators + **consensus**), real cryptographic validation |

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