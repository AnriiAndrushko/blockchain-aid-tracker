# Blockchain Aid Tracker

A .NET 9.0 blockchain-based humanitarian aid supply chain tracking system demonstrating decentralized control, transparency, and Proof-of-Authority consensus.

## Project Status

**Foundation Complete** - The core blockchain engine, cryptography services, and data access layer are fully implemented and tested.

**Current Metrics:**
-  **189 unit tests passing** (100% success rate)
-  Blockchain engine with PoA consensus support
-  Complete data access layer with EF Core
-  Repository pattern fully tested
-  Cryptographic services (SHA-256, ECDSA)
-  Comprehensive test infrastructure

**Next:** Services layer (business logic) and API endpoints

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

# Run the API (when ready)
dotnet run --project src/BlockchainAidTracker.Api/BlockchainAidTracker.Api.csproj
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
│   ├── BlockchainAidTracker.Core/         # Domain models and interfaces 
│   ├── BlockchainAidTracker.Blockchain/   # Blockchain engine 
│   ├── BlockchainAidTracker.Cryptography/ # Cryptographic utilities 
│   ├── BlockchainAidTracker.DataAccess/   # Entity Framework Core 
│   ├── BlockchainAidTracker.Services/     # Business logic (ready)
│   ├── BlockchainAidTracker.Api/          # Web API (template)
│   └── BlockchainAidTracker.Web/          # Blazor UI (referenced)
├── tests/                                  # Test projects
│   └── BlockchainAidTracker.Tests/        # 189 unit tests 
│       ├── Blockchain/                    # 42 blockchain tests
│       ├── Cryptography/                  # 31 crypto tests
│       ├── Models/                        # 53 model tests
│       ├── DataAccess/                    # 63 database tests 
│       └── Infrastructure/                # Test helpers & builders
├── blockchain-aid-tracker/                # Demo console app
├── docs/                                   # Documentation
└── CLAUDE.md                               # Detailed implementation roadmap
```

See [CLAUDE.md](CLAUDE.md) for detailed architecture and implementation status.

## Features (Planned)

- User authentication with multiple roles (Donor, Coordinator, Logistics Partner, Recipient)
- Blockchain-based shipment tracking with immutable audit trail
- Digital signatures for transaction verification
- Proof-of-Authority consensus mechanism
- Smart contracts for automated state transitions
- QR code generation for shipment verification
- Real-time blockchain explorer
- Transparent donation tracking for donors

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
| 3. Testing Infrastructure | ✅ Complete | 189 tests, DB test helpers |
| 4. User Management System | 🔨 In Progress | Entity/repo done, services pending |
| 5. Supply Chain Operations | 🔨 In Progress | Models done, services pending |
| 6. Services Layer | 📋 Next | Business logic implementation |
| 7. API Endpoints | 📋 Planned | REST API with authentication |
| 8. Proof-of-Authority Consensus | 📋 Planned | Validator nodes, P2P |
| 9. Smart Contracts | 📋 Planned | Automated workflows |
| 10. Web Application UI | 📋 Planned | Blazor dashboard |

**Legend:** ✅ Complete | 🔨 In Progress | 📋 Planned

## Testing

The project has a comprehensive test suite with **189 passing tests**:

### Test Coverage

```bash
# Run all tests
dotnet test

# Run specific test category
dotnet test --filter "FullyQualifiedName~DataAccess"
dotnet test --filter "FullyQualifiedName~Blockchain"
dotnet test --filter "FullyQualifiedName~Cryptography"
```

### Test Categories

| Category | Tests | Description |
|----------|-------|-------------|
| **Cryptography** | 31 | SHA-256 hashing, ECDSA signatures, key generation |
| **Blockchain** | 42 | Chain validation, block creation, transaction handling |
| **Models** | 53 | Domain entities (User, Shipment, Block, Transaction) |
| **Database** | 63 | Repository tests with in-memory DB, automatic cleanup |

### Test Infrastructure Features

- ✅ **Isolated databases** - Each test gets a unique in-memory database
- ✅ **Automatic cleanup** - Database state reset after every test
- ✅ **Fluent builders** - `UserBuilder`, `ShipmentBuilder` for easy test data
- ✅ **Zero cross-test contamination** - Tests can run in parallel

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