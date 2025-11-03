# Blockchain Aid Tracker

A .NET 9.0 blockchain-based humanitarian aid supply chain tracking system demonstrating decentralized control, transparency, and Proof-of-Authority consensus.

## Project Status

**Foundation, Business Logic, and Authentication API Complete** - The core blockchain engine, cryptography services, data access layer, services layer, and authentication endpoints are fully implemented and tested.

**Current Metrics:**
-  **329 tests passing** (100% success rate: 312 unit + 17 integration)
-  Authentication API endpoints operational with Swagger UI
-  6 core business services fully implemented
-  Blockchain engine with PoA consensus support
-  JWT authentication with BCrypt password hashing
-  QR code generation for shipment tracking
-  Complete data access layer with EF Core
-  Repository pattern fully tested
-  Cryptographic services (SHA-256, ECDSA)
-  Integration test infrastructure with WebApplicationFactory

**Next:** User management and shipment API endpoints

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

The following authentication endpoints are available:

- `POST /api/authentication/register` - Register new user
- `POST /api/authentication/login` - Login and get JWT tokens
- `POST /api/authentication/refresh-token` - Refresh access token
- `POST /api/authentication/logout` - Logout (requires authentication)
- `GET /api/authentication/validate` - Validate current token (requires authentication)
- `GET /health` - Health check endpoint

Visit the Swagger UI at the root URL when the API is running to test endpoints interactively.

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

## Project Structure

```
blockchain-aid-tracker/
├── src/                                    # Source code
│   ├── BlockchainAidTracker.Core/         # Domain models and interfaces ✅
│   ├── BlockchainAidTracker.Blockchain/   # Blockchain engine ✅
│   ├── BlockchainAidTracker.Cryptography/ # Cryptographic utilities ✅
│   ├── BlockchainAidTracker.DataAccess/   # Entity Framework Core ✅
│   ├── BlockchainAidTracker.Services/     # Business logic (6 services) ✅
│   ├── BlockchainAidTracker.Api/          # Web API (auth endpoints functional) ✅
│   └── BlockchainAidTracker.Web/          # Blazor UI (referenced)
├── tests/                                  # Test projects
│   └── BlockchainAidTracker.Tests/        # 329 tests (312 unit + 17 integration) ✅
│       ├── Blockchain/                    # 42 blockchain tests
│       ├── Cryptography/                  # 31 crypto tests
│       ├── Models/                        # 53 model tests
│       ├── DataAccess/                    # 63 database tests
│       ├── Services/                      # 123 services tests
│       ├── Integration/                   # 17 API integration tests ✅ NEW
│       └── Infrastructure/                # Test helpers & builders
├── blockchain-aid-tracker/                # Demo console app
├── docs/                                   # Documentation
└── CLAUDE.md                               # Detailed implementation roadmap
```

See [CLAUDE.md](CLAUDE.md) for detailed architecture and implementation status.

## Features

### Implemented ✅
- ✅ User authentication with JWT tokens (access + refresh)
- ✅ BCrypt password hashing for secure credentials
- ✅ Multiple user roles (Recipient, Donor, Coordinator, LogisticsPartner, Validator, Administrator)
- ✅ Blockchain-based shipment tracking with immutable audit trail
- ✅ Digital signatures for transaction verification (ECDSA)
- ✅ QR code generation for shipment verification (Base64 and PNG)
- ✅ Shipment lifecycle management (Created → Validated → InTransit → Delivered → Confirmed)
- ✅ User profile management with role assignment
- ✅ Business logic services layer
- ✅ Authentication REST API endpoints (register, login, refresh, logout, validate)
- ✅ JWT Bearer authentication middleware for ASP.NET Core
- ✅ Swagger/OpenAPI documentation with JWT support
- ✅ Integration test infrastructure with WebApplicationFactory

### In Progress 🔨
- 🔨 Private key encryption/decryption with user passwords
- 🔨 User management API endpoints
- 🔨 Shipment operations API endpoints
- 🔨 Blockchain query API endpoints

### Planned 📋
- 📋 Proof-of-Authority consensus with validator nodes
- 📋 Smart contracts for automated state transitions
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
| 2. Blockchain Core Implementation | ✅ Complete | Engine, consensus, cryptography |
| 3. Testing Infrastructure | ✅ Complete | 329 tests (312 unit + 17 integration) |
| 4. User Management System | ✅ Complete | Authentication, JWT, user services |
| 5. Supply Chain Operations | ✅ Complete | Shipment services, QR codes, lifecycle |
| 6. Services Layer | ✅ Complete | 6 services, DTOs, validation |
| 7. API Endpoints | 🔨 In Progress (30%) | Auth endpoints complete, Swagger UI |
| 8. Proof-of-Authority Consensus | 📋 Planned | Validator nodes, P2P |
| 9. Smart Contracts | 📋 Planned | Automated workflows |
| 10. Web Application UI | 📋 Planned | Blazor dashboard |

**Legend:** ✅ Complete | 🔨 In Progress | 📋 Planned

## Testing

The project has a comprehensive test suite with **329 passing tests**:

### Test Coverage

```bash
# Run all tests (unit + integration)
dotnet test

# Run specific test category
dotnet test --filter "FullyQualifiedName~Services"
dotnet test --filter "FullyQualifiedName~DataAccess"
dotnet test --filter "FullyQualifiedName~Blockchain"
dotnet test --filter "FullyQualifiedName~Cryptography"
dotnet test --filter "FullyQualifiedName~Integration"
```

### Test Categories

| Category | Tests | Description |
|----------|-------|-------------|
| **Services** | 123 | Business logic, authentication, shipment lifecycle, QR codes |
| **Database** | 63 | Repository tests with in-memory DB, automatic cleanup |
| **Models** | 53 | Domain entities (User, Shipment, Block, Transaction) |
| **Blockchain** | 42 | Chain validation, block creation, transaction handling |
| **Cryptography** | 31 | SHA-256 hashing, ECDSA signatures, key generation |
| **Integration** | 17 | API endpoint tests, full auth workflows ✅ NEW |

### Test Infrastructure Features

- ✅ **Isolated databases** - Each test gets a unique in-memory database (unit & integration)
- ✅ **Automatic cleanup** - Database state reset after every test
- ✅ **Fluent builders** - `UserBuilder`, `ShipmentBuilder` for easy test data
- ✅ **Moq framework** - Mocking dependencies for service layer tests
- ✅ **WebApplicationFactory** - Integrated API testing with real HTTP requests
- ✅ **Comprehensive coverage** - Success paths, error handling, edge cases
- ✅ **Zero cross-test contamination** - Tests can run in parallel
- ✅ **Environment separation** - Test-specific configuration (appsettings.Testing.json)

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