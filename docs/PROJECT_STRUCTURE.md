# Project Structure

This document describes the organization of the Blockchain Aid Tracker solution.

## Solution Structure

The solution follows a clean architecture pattern with clear separation of concerns:

```
blockchain-aid-tracker/
├── src/                                    # Source code projects
│   ├── BlockchainAidTracker.Core/         # Core domain models and interfaces
│   ├── BlockchainAidTracker.Blockchain/   # Blockchain engine implementation
│   ├── BlockchainAidTracker.Cryptography/ # Cryptographic utilities (ECDSA, hashing)
│   ├── BlockchainAidTracker.DataAccess/   # Entity Framework Core and repositories
│   ├── BlockchainAidTracker.Services/     # Business logic services
│   ├── BlockchainAidTracker.Api/          # ASP.NET Core Web API
│   └── BlockchainAidTracker.Web/          # Blazor Web App
├── tests/                                  # Test projects (to be added)
├── docs/                                   # Documentation
└── blockchain-aid-tracker/                 # Legacy console app (to be deprecated)
```

## Project Dependencies

The dependency hierarchy is designed to follow the Dependency Inversion Principle:

```
BlockchainAidTracker.Core (No dependencies)
    ↑
    ├── BlockchainAidTracker.Cryptography
    │       ↑
    │       └── BlockchainAidTracker.Blockchain
    │
    ├── BlockchainAidTracker.DataAccess
    │
    └── BlockchainAidTracker.Services
            ↑
            ├── BlockchainAidTracker.Api
            └── BlockchainAidTracker.Web
```

## Project Descriptions

### BlockchainAidTracker.Core
Contains core domain entities, interfaces, and value objects. This project has no external dependencies (except .NET framework) and defines the business domain.

**Contains:**
- Domain entities (User, Shipment, Block, Transaction, etc.)
- Repository interfaces
- Service interfaces
- Enums and value objects

### BlockchainAidTracker.Cryptography
Implements cryptographic operations required for blockchain security.

**Contains:**
- ECDSA key pair generation
- Digital signature creation and verification
- SHA-256 hashing
- Key encryption/decryption utilities

**Dependencies:** Core

### BlockchainAidTracker.Blockchain
Implements the blockchain engine and consensus mechanism.

**Contains:**
- Block and chain management
- Transaction processing
- Proof-of-Authority consensus
- Chain validation
- Blockchain persistence

**Dependencies:** Core, Cryptography

### BlockchainAidTracker.DataAccess
Implements data persistence using Entity Framework Core.

**Contains:**
- DbContext configuration
- Entity configurations
- Repository implementations
- Database migrations
- SQLite and PostgreSQL support

**Dependencies:** Core, EF Core packages

### BlockchainAidTracker.Services
Contains business logic and orchestration services.

**Contains:**
- User authentication and authorization services
- Shipment management services
- Blockchain integration services
- Smart contract execution
- QR code generation
- Password hashing

**Dependencies:** Core, Blockchain, DataAccess, Cryptography, BCrypt.Net, QRCoder

### BlockchainAidTracker.Api
ASP.NET Core Web API exposing RESTful endpoints.

**Contains:**
- API controllers
- JWT authentication middleware
- API configuration
- Dependency injection setup
- Swagger/OpenAPI documentation

**Dependencies:** Services, Core, JWT Bearer authentication

### BlockchainAidTracker.Web
Blazor Web App providing the user interface.

**Contains:**
- Blazor components and pages
- UI for shipment management
- Blockchain explorer interface
- User authentication UI
- Dashboard and reporting views

**Dependencies:** Services, Core

## Technology Stack

- **.NET 9.0** - Target framework
- **ASP.NET Core** - Web API and Blazor hosting
- **Entity Framework Core 9.0** - ORM for data access
- **SQLite** - Development database (prototype)
- **PostgreSQL** - Production database (optional)
- **JWT Bearer Authentication** - API authentication
- **BCrypt.Net** - Password hashing
- **QRCoder** - QR code generation
- **Docker** - Containerization support

## Next Steps

1. Implement domain models in Core project
2. Set up database context and migrations in DataAccess
3. Implement cryptographic services
4. Build blockchain engine
5. Create business logic services
6. Develop API endpoints
7. Build Blazor UI components
8. Add comprehensive testing

Refer to CLAUDE.md for detailed implementation roadmap.