# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a .NET 9.0 blockchain-based humanitarian aid supply chain tracking system. The project demonstrates a decentralized system for controlling humanitarian aid supply chains using blockchain technology, .NET ecosystem, and Proof-of-Authority consensus.

**Current Status**: Foundation, business logic, authentication API, and cryptographic key management complete. The blockchain engine with real ECDSA signature validation, cryptography services, data access layer, services layer, and authentication endpoints are fully implemented and tested with 351 passing tests.

**Recently Completed** (Latest):
- ✅ **Cryptographic Key Management & Real Signatures** NEW
  - KeyManagementService with AES-256 encryption for private keys
  - TransactionSigningContext for secure in-memory key storage during sessions
  - Private keys encrypted with user passwords using PBKDF2 key derivation
  - Real ECDSA transaction signing (no more placeholder signatures)
  - **Blockchain signature validation ENABLED** - all transactions cryptographically verified
  - Seamless key decryption during login for transaction signing
  - 351 tests passing with real cryptographic validation
- ✅ **Shipment API Endpoints** NEW
  - ShipmentController with 6 endpoints (create, list, get, update-status, confirm-delivery, history)
  - Complete shipment lifecycle with blockchain integration
  - QR code generation endpoint
  - Role-based authorization for shipment operations
  - 22 integration tests for shipment endpoints (all passing)
- ✅ **Authentication API Endpoints**
  - AuthenticationController with 5 endpoints (register, login, refresh-token, logout, validate)
  - JWT Bearer authentication configured in ASP.NET Core
  - Swagger/OpenAPI with JWT authentication UI
  - CORS policy configuration
  - Health checks with database monitoring
  - Comprehensive error handling and logging
- ✅ **Integration Test Infrastructure**
  - CustomWebApplicationFactory for isolated API testing
  - 39 integration tests total (17 authentication + 22 shipment endpoints, all passing)
  - InMemory database configuration for test isolation
  - Test environment configuration with appsettings.Testing.json
- ✅ **Complete Services layer with business logic**
  - 7 core services: PasswordService, TokenService, KeyManagementService, AuthenticationService, UserService, QrCodeService, ShipmentService
  - JWT authentication with access tokens and refresh tokens
  - BCrypt password hashing (work factor: 12)
  - AES-256 private key encryption with password-based key derivation
  - QR code generation for shipment tracking
  - Full shipment lifecycle management with blockchain integration
  - 9 DTO classes for API contracts
  - Custom exception classes (BusinessException, UnauthorizedException, NotFoundException)
  - Dependency injection configuration
- ✅ All 351 tests passing (312 unit tests + 39 integration tests)
- ✅ Complete DataAccess layer with Entity Framework Core
- ✅ Database testing infrastructure with in-memory database isolation

**Next Steps**: Implement remaining API endpoints (User management, Blockchain queries).

## Build and Run Commands

### Local Development
```bash
# Build the solution
dotnet build blockchain-aid-tracker.sln

# Run the API (Swagger available at http://localhost:5000 or https://localhost:5001)
dotnet run --project src/BlockchainAidTracker.Api/BlockchainAidTracker.Api.csproj

# Run the demo console application
dotnet run --project blockchain-aid-tracker/blockchain-aid-tracker.csproj

# Build with specific configuration
dotnet build blockchain-aid-tracker/blockchain-aid-tracker.csproj -c Release
```

### Docker
```bash
# Build and run with Docker Compose
docker compose up --build

# Build Docker image manually
docker build -t blockchain-aid-tracker -f blockchain-aid-tracker/Dockerfile .
```

### Database Operations
```bash
# Apply migrations to create/update database
dotnet ef database update --project src/BlockchainAidTracker.DataAccess

# Create a new migration (after model changes)
dotnet ef migrations add MigrationName --project src/BlockchainAidTracker.DataAccess

# Remove last migration (if needed)
dotnet ef migrations remove --project src/BlockchainAidTracker.DataAccess

# View migration list
dotnet ef migrations list --project src/BlockchainAidTracker.DataAccess

# Run comprehensive database + blockchain demo
dotnet run --project blockchain-aid-tracker

# Run all tests (unit + integration)
dotnet test

# Run only integration tests
dotnet test --filter "FullyQualifiedName~Integration"

# Run only unit tests
dotnet test --filter "FullyQualifiedName!~Integration"
```

### Database File Location
- **SQLite Database**: `src/BlockchainAidTracker.DataAccess/blockchain-aid-tracker.db`
- Use tools like [DB Browser for SQLite](https://sqlitebrowser.org/) or VS Code SQLite extensions to inspect

## Project Structure

- **src/** - Source code directory
  - **BlockchainAidTracker.Core/** - Core domain models and interfaces
  - **BlockchainAidTracker.Blockchain/** - Blockchain engine implementation
  - **BlockchainAidTracker.Cryptography/** - Cryptographic services (SHA-256, ECDSA)
  - **BlockchainAidTracker.DataAccess/** - Entity Framework Core data access layer
  - **BlockchainAidTracker.Services/** - Business logic services (complete with 6 services)
  - **BlockchainAidTracker.Api/** - ASP.NET Core Web API project (authentication endpoints functional)
  - **BlockchainAidTracker.Web/** - Blazor Server web application (referenced)
- **tests/** - Test projects
  - **BlockchainAidTracker.Tests/** - xUnit test project (329 passing tests: 312 unit + 17 integration)
- **blockchain-aid-tracker/** - Main console application/demo project
  - `blockchain-aid-tracker.csproj` - .NET 9.0 console app with Docker support
  - `Program.cs` - Comprehensive demo of database and blockchain integration
  - `Dockerfile` - Multi-stage Docker build configuration
- **compose.yaml** - Docker Compose configuration for containerized deployment
- **blockchain-aid-tracker.sln** - Solution file

## Technical Configuration

- **Target Framework**: .NET 9.0
- **Output Type**: Console executable (will transition to ASP.NET Core Web API + Blazor Server)
- **Nullable Reference Types**: Enabled
- **Implicit Usings**: Enabled
- **Docker Base Image**: mcr.microsoft.com/dotnet/runtime:9.0
- **Docker SDK Image**: mcr.microsoft.com/dotnet/sdk:9.0
- **Target Database**: SQLite (prototype) or PostgreSQL (production)
- **Authentication**: JWT-based with cryptographic key pairs
- **Consensus Mechanism**: Proof-of-Authority (PoA)

---

## Implemented Components Summary

### ✅ Core Module (100% Complete)
**Location**: `src/BlockchainAidTracker.Core/`

**Domain Models**:
- `Block` - Blockchain block with index, timestamp, transactions, hashes, and validator signature
- `Transaction` - Blockchain transaction with type, sender, payload, and signature
- `TransactionType` - Enum (ShipmentCreated, StatusUpdated, DeliveryConfirmed)
- `Shipment` - Shipment entity with full lifecycle management
- `ShipmentItem` - Individual items within shipments
- `ShipmentStatus` - Enum (Created, Validated, InTransit, Delivered, Confirmed)
- `User` - User entity with authentication fields and role-based access
- `UserRole` - Enum (Recipient, Donor, Coordinator, LogisticsPartner, Validator, Administrator)

**Interfaces**:
- `IHashService` - SHA-256 hashing interface
- `IDigitalSignatureService` - ECDSA signature interface

**Extensions**:
- `BlockExtensions` - Block signing and verification
- `TransactionExtensions` - Transaction signing and verification

### ✅ Cryptography Module (100% Complete)
**Location**: `src/BlockchainAidTracker.Cryptography/`

**Services**:
- `HashService` - SHA-256 hashing implementation (string and byte array overloads)
- `DigitalSignatureService` - ECDSA (P-256 curve) for signing and verification
  - Key pair generation (base64 encoded)
  - Data signing with SHA-256
  - Signature verification

**Test Coverage**: 31 unit tests (100% passing)

### ✅ Blockchain Module (100% Complete)
**Location**: `src/BlockchainAidTracker.Blockchain/`

**Implementation**:
- `Blockchain` - Complete blockchain engine with:
  - Genesis block initialization
  - Transaction management (pending pool, validation)
  - Block creation and validation
  - Chain validation (hashes, signatures, integrity)
  - Query methods (GetBlockByIndex, GetTransactionById, etc.)

**Test Coverage**: 42 unit tests (100% passing)

### ✅ DataAccess Module (100% Complete)
**Location**: `src/BlockchainAidTracker.DataAccess/`

**Database Context**:
- `ApplicationDbContext` - EF Core DbContext with:
  - DbSets for Shipments, ShipmentItems, Users
  - Automatic timestamp updates
  - Fluent API entity configurations

**Entity Configurations**:
- `ShipmentConfiguration` - Shipment table schema, indexes, relationships
- `ShipmentItemConfiguration` - ShipmentItem with foreign key to Shipment
- `UserConfiguration` - User table with unique constraints

**Repository Pattern**:
- `IRepository<T>` - Generic repository interface (12 operations)
- `Repository<T>` - Generic repository implementation
- `IShipmentRepository` - Specialized shipment queries (8 methods)
- `ShipmentRepository` - Shipment repository with eager loading
- `IUserRepository` - User-specific queries (7 methods)
- `UserRepository` - User repository implementation

**Dependency Injection**:
- `DependencyInjection` class with extension methods:
  - `AddDataAccess()` - SQLite configuration
  - `AddDataAccessWithPostgreSQL()` - PostgreSQL configuration
  - `AddDataAccessWithInMemoryDatabase()` - In-memory for testing
- `DesignTimeDbContextFactory` - For EF Core design-time operations

**Migrations**:
- `InitialCreate` - Initial database schema with 3 tables (Users, Shipments, ShipmentItems)
- Comprehensive indexes and foreign key constraints

**Database Schema**:
- **Users**: 14 columns, unique indexes on Username/Email/PublicKey, role-based filtering
- **Shipments**: 11 columns, unique QR code, foreign key relationships
- **ShipmentItems**: 7 columns, cascade delete from Shipments

### ✅ Services Module (100% Complete)
**Location**: `src/BlockchainAidTracker.Services/`

**Core Services**:
- `PasswordService` - BCrypt password hashing and verification (work factor: 12)
- `TokenService` - JWT access token and refresh token generation/validation
- `KeyManagementService` - AES-256 encryption/decryption of private keys with PBKDF2
- `TransactionSigningContext` - Thread-safe in-memory storage for decrypted private keys
- `AuthenticationService` - User registration, login, token refresh, validation with key management
- `UserService` - User CRUD operations, role assignment, activation/deactivation
- `QrCodeService` - QR code generation for shipment tracking (Base64 and PNG formats)
- `ShipmentService` - Complete shipment lifecycle with blockchain integration and real signatures

**DTOs (Data Transfer Objects)**:
- Authentication: `RegisterRequest`, `LoginRequest`, `AuthenticationResponse`, `RefreshTokenRequest`
- User: `UserDto`, `UpdateUserRequest`, `AssignRoleRequest`
- Shipment: `CreateShipmentRequest`, `ShipmentDto`, `UpdateShipmentStatusRequest`, `ShipmentItemDto`

**Exception Classes**:
- `BusinessException` - Business logic validation errors
- `UnauthorizedException` - Authentication/authorization failures
- `NotFoundException` - Resource not found errors

**Configuration**:
- `JwtSettings` - JWT token configuration (secret key, issuer, audience, expiration times)
- `DependencyInjection` - Service registration extension methods

**Key Features**:
- JWT-based authentication with access tokens (60 min) and refresh tokens (7 days)
- BCrypt password hashing for secure credential storage
- **AES-256 private key encryption with user passwords (PBKDF2, 10000 iterations)**
- **Real ECDSA transaction signing with cryptographically verified signatures**
- **Blockchain transaction signature validation ENABLED**
- Automatic key decryption and secure in-memory storage during user sessions
- QR code generation for shipment tracking
- Complete shipment lifecycle with cryptographically signed blockchain transactions
- Role-based access control validation
- Complete CRUD operations for users and shipments

**Security Implementation**:
- Private keys encrypted at rest with user passwords
- Keys decrypted and stored in memory only during active sessions
- All blockchain transactions signed with real ECDSA private keys
- Transaction signatures validated before adding to blockchain
- AES-256-CBC encryption with random salt and IV per key
- PBKDF2 key derivation with 10,000 iterations and SHA-256

**Production Considerations**:
- Current implementation uses in-memory key storage (suitable for prototype/demo)
- For production, consider:
  - Azure Key Vault, AWS KMS, or Hardware Security Module (HSM)
  - Session expiration for decrypted keys
  - Key rotation mechanisms
  - Proper logout to clear signing context

**NuGet Packages**:
- BCrypt.Net-Next 4.0.3
- QRCoder 1.6.0
- System.IdentityModel.Tokens.Jwt 8.2.1
- Microsoft.IdentityModel.Tokens 8.2.1

**Test Coverage**: 123 unit tests (100% passing)

### ✅ API Module (60% - Authentication & Shipments Complete)
**Location**: `src/BlockchainAidTracker.Api/`

**Controllers**:
- `AuthenticationController` - Complete authentication endpoints (5 endpoints)
  - POST /api/authentication/register
  - POST /api/authentication/login
  - POST /api/authentication/refresh-token
  - POST /api/authentication/logout (requires auth)
  - GET /api/authentication/validate (requires auth)
- `ShipmentController` - Complete shipment management endpoints (6 endpoints)
  - POST /api/shipments - Create new shipment (Coordinator only)
  - GET /api/shipments - List all shipments with optional filtering
  - GET /api/shipments/{id} - Get shipment details
  - PUT /api/shipments/{id}/status - Update shipment status
  - POST /api/shipments/{id}/confirm-delivery - Confirm delivery (Recipient only)
  - GET /api/shipments/{id}/history - Get blockchain transaction history
  - GET /api/shipments/{id}/qrcode - Get shipment QR code as PNG

**Configuration** (Program.cs):
- JWT Bearer authentication with token validation
- **Blockchain with transaction signature validation ENABLED**
- Swagger/OpenAPI with JWT authentication UI
- CORS policy for cross-origin requests
- Health checks with database context monitoring
- Environment-specific database initialization
- All service layers registered (Cryptography, Blockchain, DataAccess, Services, KeyManagement)

**NuGet Packages**:
- Microsoft.AspNetCore.Authentication.JwtBearer 9.0.10
- Swashbuckle.AspNetCore 7.2.0
- Microsoft.AspNetCore.Mvc.Testing 9.0.10
- Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore 9.0.10

**Configuration Files**:
- `appsettings.json` - Production configuration
- `appsettings.Testing.json` - Test environment configuration

**Next**: User management and blockchain query endpoints

### ✅ Test Suite (351 Tests - 100% Passing)
**Location**: `tests/BlockchainAidTracker.Tests/`
- Cryptography tests: 31 tests
- Blockchain tests: 42 tests
- Core model tests: 53 tests
- Database tests: 63 tests
  - UserRepository tests: 31 tests
  - ShipmentRepository tests: 32 tests
  - ApplicationDbContext tests: 20 tests
- **Services tests: 123 tests**
  - PasswordService tests: 13 tests
  - TokenService tests: 17 tests
  - AuthenticationService tests: 21 tests
  - UserService tests: 16 tests
  - QrCodeService tests: 14 tests
  - ShipmentService tests: 42 tests
- **Integration tests: 39 tests**
  - AuthenticationController API tests: 17 tests
  - ShipmentController API tests: 22 tests
  - Full end-to-end authentication and shipment workflows
  - Real ECDSA signature generation and validation in tests
  - In-memory database for test isolation
  - WebApplicationFactory for API testing
- **Test Infrastructure**:
  - `DatabaseTestBase` - Base class with automatic cleanup and isolation (unit tests)
  - `CustomWebApplicationFactory` - API test factory with in-memory database (integration tests)
  - `TestDataBuilder` - Fluent builders (UserBuilder, ShipmentBuilder)
  - In-memory database with unique instances per test
  - Moq framework for mocking dependencies in service tests
  - All tests pass with real cryptographic signature validation
- Execution time: ~25 seconds

---

## Implementation Roadmap (TODO)

All features below are planned for step-by-step implementation. Each section represents a major component of the system.

### 1. Core Architecture Setup

#### TODO: Project Structure Reorganization
- [ ] Convert console app to ASP.NET Core Web API project
- [ ] Add Blazor Server project for web interface
- [x] Create class library projects for:
  - [x] Core domain models and interfaces
  - [x] Blockchain engine
  - [x] Data access layer (created, empty)
  - [x] Business logic services (created, empty)
  - [x] Cryptography utilities
- [x] Set up solution folder structure:
  - [x] `src/` - Source code
  - [x] `tests/` - Test projects
  - [ ] `docs/` - Documentation

#### Technology Stack Setup (Partially Complete)
- [x] Add NuGet packages:
  - [ ] ASP.NET Core Web API (template exists)
  - [ ] Blazor Server (referenced)
  - [x] Entity Framework Core (9.0.10)
  - [x] SQLite/PostgreSQL provider (SQLite 9.0.10, Npgsql 9.0.4)
  - [x] JWT authentication libraries (System.IdentityModel.Tokens.Jwt 8.2.1, Microsoft.IdentityModel.Tokens 8.2.1)
  - [x] QR code generation library (QRCoder 1.6.0)
  - [x] BCrypt.NET or similar for password hashing (BCrypt.Net-Next - added to Services project)
- [x] Configure dependency injection container (DataAccess DI extensions created)
- [ ] Set up appsettings.json configuration structure
- [ ] Configure HTTPS and CORS policies

#### ✅ DONE: Database Infrastructure
- [x] Design Entity Framework Core data models
- [x] Create database context class (ApplicationDbContext)
- [x] Implement repository pattern interfaces (IRepository<T>, IShipmentRepository, IUserRepository)
- [x] Implement repository pattern concrete classes (Repository<T>, ShipmentRepository, UserRepository)
- [x] Set up migrations system (EF Core Migrations configured)
- [x] Create initial database schema migration (InitialCreate migration created)
- [x] Configure connection string management (supports SQLite, PostgreSQL, and In-Memory)
- [x] Create dependency injection extension methods (AddDataAccess, AddDataAccessWithPostgreSQL, AddDataAccessWithInMemoryDatabase)
- [ ] Implement caching mechanism (in-memory cache)

---

### 2. User Management System

#### User Authentication & Authorization (70% Complete)
- [x] Implement user entity model with roles (Recipient, Donor, Coordinator, LogisticsPartner, Validator, Administrator)
- [x] Create UserRole enum with all role types
- [x] Create cryptographic key pair generation service (ECDSA)
- [x] Implement password hashing with BCrypt (work factor: 12)
- [x] Create JWT token generation and validation service (access + refresh tokens)
- [x] Implement AuthenticationService with registration, login, and token refresh
- [x] Build role-based access control validation in services
- [ ] Build private key encryption/decryption with user passwords (critical for production)
- [ ] Implement multi-factor authentication framework
- [x] Build role-based access control (RBAC) middleware for API
- [x] Create authentication API endpoints:
  - [x] POST /api/authentication/register
  - [x] POST /api/authentication/login
  - [x] POST /api/authentication/refresh-token
  - [x] POST /api/authentication/logout
  - [x] GET /api/authentication/validate

#### User Profile Management (80% Complete)
- [x] Create user profile entity and repository (User entity with IUserRepository and UserRepository)
- [x] Implement user profile CRUD operations (via UserRepository and UserService)
- [x] Build secure credential storage (password hash fields, placeholder for encrypted private key)
- [x] Implement UserService with profile updates, role assignment, activation/deactivation
- [ ] Create user management API endpoints:
  - [ ] GET /api/users/profile
  - [ ] PUT /api/users/profile
  - [ ] GET /api/users/{id}
  - [ ] POST /api/users/assign-role

#### TODO: User Management UI (Blazor)
- [ ] Create login page component
- [ ] Create registration page component
- [ ] Build user profile management page
- [ ] Implement role assignment interface (admin only)
- [ ] Add user authentication state management

---

### 3. Blockchain Core Implementation

#### ✅ DONE: Blockchain Data Structures
- [x] Create Block class with properties:
  - [x] Index
  - [x] Timestamp
  - [x] Transactions list
  - [x] Previous hash
  - [x] Current hash
  - [x] Nonce (if needed)
  - [x] Validator signature
- [x] Create Transaction class with properties:
  - [x] Transaction ID
  - [x] Type (SHIPMENT_CREATED, STATUS_UPDATED, DELIVERY_CONFIRMED)
  - [x] Timestamp
  - [x] Sender public key
  - [x] Payload data
  - [x] Digital signature
- [x] Create Blockchain class to manage chain operations

#### ✅ DONE: Cryptographic Functions
- [x] Implement SHA-256 hashing for blocks
- [x] Implement ECDSA digital signature generation
- [x] Implement ECDSA signature verification
- [x] Create hash calculation for blocks
- [ ] Build merkle tree implementation (optional for prototype)

#### ✅ DONE: Blockchain Operations
- [x] Implement add transaction to pending pool
- [x] Implement block creation logic
- [x] Implement block validation logic
- [x] Implement chain validation (verify all hashes and signatures)
- [x] Create genesis block initialization
- [ ] Implement blockchain persistence (file-based or in-memory)
- [ ] Build blockchain loading and saving mechanisms

#### TODO: Blockchain API Endpoints
- [ ] GET /api/blockchain/chain - Get full blockchain
- [ ] GET /api/blockchain/blocks/{index} - Get specific block
- [ ] GET /api/blockchain/transactions/{id} - Get transaction details
- [ ] POST /api/blockchain/validate - Validate entire chain
- [ ] GET /api/blockchain/pending - Get pending transactions

---

### 4. Proof-of-Authority Consensus

#### TODO: Validator Node System
- [ ] Create Validator entity model
- [ ] Implement validator registration and configuration (3-5 validators)
- [ ] Build validator node service
- [ ] Create validator authentication mechanism
- [ ] Implement validator key pair management

#### TODO: Consensus Engine
- [ ] Create consensus interface and base implementation
- [ ] Implement PoA consensus algorithm:
  - [ ] Block proposer selection (round-robin or similar)
  - [ ] Transaction validation by validators
  - [ ] Block confirmation by validator quorum
  - [ ] Consensus threshold logic (e.g., 2/3 majority)
- [ ] Build consensus state management
- [ ] Implement fork resolution (simple longest chain rule)

#### TODO: Peer-to-Peer Network (Simplified)
- [ ] Create node communication service (HTTP-based)
- [ ] Implement node discovery mechanism
- [ ] Build transaction broadcast to validators
- [ ] Implement block broadcast to network
- [ ] Create blockchain synchronization logic
- [ ] Handle network partitioning scenarios

#### TODO: Consensus API Endpoints
- [ ] POST /api/consensus/propose-block - Propose new block
- [ ] POST /api/consensus/validate-block - Validate proposed block
- [ ] GET /api/consensus/validators - Get validator list
- [ ] GET /api/consensus/status - Get consensus status

---

### 5. Supply Chain Operations

#### ✅ DONE: Shipment Data Model
- [x] Create Shipment entity with properties:
  - [x] Shipment ID
  - [x] Item descriptions and quantities
  - [x] Origin point
  - [x] Destination point
  - [x] Expected delivery timeframe
  - [x] Assigned recipient
  - [x] Current status
  - [x] QR code data
  - [x] Created timestamp
  - [x] Updated timestamp
- [x] Create ShipmentStatus enum (Created, Validated, InTransit, Delivered, Confirmed)
- [x] Create ShipmentItem entity for item details

#### ✅ DONE: Shipment Service Layer
- [x] Create ShipmentService with business logic
- [x] Implement shipment creation workflow:
  - [x] Validate user permissions (Coordinator role)
  - [x] Create shipment record
  - [x] Generate blockchain transaction (SHIPMENT_CREATED)
  - [ ] Broadcast transaction to validators (single-node implementation, no broadcast needed)
- [x] Implement shipment status update workflow with blockchain transactions
- [x] Implement delivery confirmation workflow with blockchain transactions
- [x] Build shipment validation logic (status transitions, role-based permissions)
- [x] Implement shipment query operations (by ID, by status, by recipient)
- [x] Build blockchain history and verification methods

**Note**: Transaction signatures currently use placeholders. Private key management infrastructure required for production use.

#### ✅ DONE: QR Code System
- [x] Integrate QR code generation library (QRCoder 1.6.0)
- [x] Create QR code generation service (QrCodeService)
- [x] Generate unique QR codes for shipments (Base64 and PNG formats)
- [x] Support custom data QR code generation
- [ ] Implement QR code scanning simulation (UI layer)
- [ ] Build QR code validation logic (UI/API layer)

#### TODO: Shipment API Endpoints
- [ ] POST /api/shipments - Create new shipment
- [ ] GET /api/shipments - List all shipments (with filtering)
- [ ] GET /api/shipments/{id} - Get shipment details
- [ ] PUT /api/shipments/{id}/status - Update shipment status
- [ ] POST /api/shipments/{id}/confirm-delivery - Confirm delivery
- [ ] GET /api/shipments/{id}/history - Get blockchain transaction history
- [ ] GET /api/shipments/{id}/qrcode - Get QR code image

#### TODO: Shipment Management UI (Blazor)
- [ ] Create shipment creation form component
- [ ] Build shipment list/grid component with filtering
- [ ] Create shipment detail view component
- [ ] Implement shipment tracking timeline visualization
- [ ] Build status update interface
- [ ] Create QR code display component
- [ ] Implement delivery confirmation page with QR code scanner simulation

---

### 6. Smart Contracts

#### TODO: Smart Contract Framework
- [ ] Design smart contract interface
- [ ] Create smart contract base class
- [ ] Implement contract execution engine
- [ ] Build contract state management
- [ ] Create contract deployment mechanism

#### TODO: Shipment Tracking Smart Contract
- [ ] Define contract logic for automatic state transitions
- [ ] Implement conditions for state changes:
  - [ ] Created → Validated (when confirmed by validators)
  - [ ] Validated → InTransit (when coordinator updates)
  - [ ] InTransit → Delivered (when location confirmed)
  - [ ] Delivered → Confirmed (when recipient confirms)
- [ ] Build event emission for state changes
- [ ] Implement validation rules

#### TODO: Delivery Verification Smart Contract
- [ ] Define contract logic for delivery verification
- [ ] Implement QR code scan validation
- [ ] Build automated confirmation when recipient scans QR code
- [ ] Create notification/alert system for successful delivery
- [ ] Implement penalty/escalation logic for missed deliveries (optional)

#### TODO: Smart Contract API
- [ ] POST /api/contracts/deploy - Deploy new contract
- [ ] POST /api/contracts/execute - Execute contract function
- [ ] GET /api/contracts/{id}/state - Get contract state
- [ ] GET /api/contracts/{id}/events - Get contract events

---

### 7. Web Application (Blazor UI)

#### TODO: Dashboard Components
- [ ] Create main dashboard layout
- [ ] Build overview statistics cards (total shipments, active, delivered, etc.)
- [ ] Implement recent shipments list component
- [ ] Create system status indicators (blockchain sync, validator status)
- [ ] Build role-specific dashboard views

#### TODO: Blockchain Explorer UI
- [ ] Create blockchain explorer page
- [ ] Build block list component with pagination
- [ ] Implement block detail view with transaction list
- [ ] Create transaction detail modal/page
- [ ] Build hash verification visualizer
- [ ] Implement digital signature verification display
- [ ] Create chain visualization (optional)

#### TODO: Reporting & Analytics
- [ ] Create reporting dashboard page
- [ ] Build shipment statistics components:
  - [ ] Total shipments by status
  - [ ] Delivery success rate
  - [ ] Average delivery time
  - [ ] Shipments by route
- [ ] Implement donor transparency view (funded shipments)
- [ ] Create export functionality (CSV/PDF reports)

#### TODO: UI/UX Polish
- [ ] Implement responsive design for mobile devices
- [ ] Add loading indicators for async operations
- [ ] Create error notification system
- [ ] Build success/confirmation toasts
- [ ] Implement form validation with user feedback
- [ ] Add accessibility features (ARIA labels, keyboard navigation)

---

### 8. Security Implementation

#### TODO: Cryptographic Security
- [ ] Implement JWT token generation with secure secrets
- [ ] Build token refresh mechanism
- [ ] Create password complexity validation
- [ ] Implement secure password reset workflow
- [ ] Build private key backup and recovery system
- [ ] Implement key rotation mechanism

#### TODO: API Security
- [ ] Add authentication middleware to all protected endpoints
- [ ] Implement rate limiting
- [ ] Build input validation and sanitization for all endpoints
- [ ] Create SQL injection prevention measures
- [ ] Implement XSS protection
- [ ] Add CSRF protection for Blazor forms
- [ ] Configure HTTPS enforcement
- [ ] Implement security headers (HSTS, CSP, etc.)

#### Blockchain Security (Partially Complete)
- [x] Implement transaction tampering detection
- [ ] Build double-spending prevention
- [x] Create signature verification for all transactions
- [x] Implement block validation before adding to chain
- [ ] Build access control for validator operations
- [ ] Create audit logging for all blockchain operations

---

### 9. Functional Workflows Implementation

#### TODO: Workflow 1 - Complete Shipment Lifecycle
- [ ] Implement coordinator shipment creation
- [ ] Build SHIPMENT_CREATED transaction generation
- [ ] Create validator confirmation workflow
- [ ] Implement automatic status update to "Validated"
- [ ] Build QR code generation and display
- [ ] Create "In Transit" status update by coordinator
- [ ] Implement delivery confirmation by recipient (QR scan)
- [ ] Build DELIVERY_CONFIRMED transaction generation
- [ ] Create smart contract automatic validation
- [ ] Build end-to-end workflow test

#### TODO: Workflow 2 - Donor Transparency
- [ ] Implement donor view of funded shipments
- [ ] Build shipment detail view with full history
- [ ] Create blockchain transaction history display
- [ ] Implement transaction hash verification UI
- [ ] Build digital signature verification display
- [ ] Create audit trail visualization

#### TODO: Workflow 3 - Consensus Demonstration
- [ ] Implement new transaction creation and broadcast
- [ ] Build transaction broadcasting to validator nodes
- [ ] Create validator validation process
- [ ] Implement block proposer selection
- [ ] Build block creation by proposer
- [ ] Create block confirmation by other validators
- [ ] Implement consensus threshold check
- [ ] Build block addition to chain after consensus
- [ ] Create visualization of consensus process

---

### 10. Testing Strategy

#### ✅ Unit Tests (312 Tests - All Passing)
*Note: Total test count is now 329, including 17 integration tests*
- [x] Set up xUnit test project
- [x] Create test fixtures and helpers
- [x] **Create database test infrastructure** (DatabaseTestBase, TestDataBuilder)
- [x] Write tests for cryptographic functions (31 tests):
  - [x] SHA-256 hashing
  - [x] ECDSA signature generation
  - [x] ECDSA signature verification
  - [x] Key pair generation
  - [x] Edge cases (null inputs, tampered data, wrong keys)
- [x] Write tests for blockchain operations (42 tests):
  - [x] Block creation
  - [x] Block validation
  - [x] Chain validation
  - [x] Transaction creation
  - [x] Transaction validation
  - [x] Genesis block initialization
  - [x] Tampering detection
  - [x] End-to-end workflows
- [ ] Write tests for consensus logic:
  - [ ] Validator selection
  - [ ] Block proposal
  - [ ] Consensus threshold
  - [ ] Fork resolution
- [x] Write tests for core models (53 tests):
  - [x] Shipment entity (lifecycle, validation, state transitions)
  - [x] ShipmentItem entity
  - [x] Block entity
  - [x] Transaction entity
- [x] Write tests for services (123 tests):
  - [x] PasswordService (13 tests) - Hashing, verification, edge cases
  - [x] TokenService (17 tests) - JWT generation, validation, claim extraction
  - [x] AuthenticationService (21 tests) - Registration, login, token refresh
  - [x] UserService (16 tests) - CRUD operations, role assignment, activation
  - [x] QrCodeService (14 tests) - QR generation, various formats, data types
  - [x] ShipmentService (42 tests) - Complete lifecycle, blockchain integration, validation
- [x] **Write tests for repositories with in-memory database (63 tests)**:
  - [x] UserRepository tests (31 tests) - All CRUD operations, role filtering, existence checks
  - [x] ShipmentRepository tests (32 tests) - Complex queries, eager loading, date ranges, QR codes
  - [x] ApplicationDbContext tests (20 tests) - Relationships, cascade delete, indexes, change tracking
  - [x] Database isolation and automatic cleanup verified
  - [x] Bulk operations and performance testing
- [ ] Write tests for smart contracts:
  - [ ] Delivery verification logic

#### Integration Tests (20% Complete)
- [x] Set up integration test project with WebApplicationFactory
- [x] Create test database setup/teardown (in-memory database)
- [ ] Write API endpoint tests:
  - [x] Authentication endpoints (17 tests - all passing)
  - [ ] User management endpoints
  - [ ] Shipment endpoints
  - [ ] Blockchain endpoints
  - [ ] Consensus endpoints
- [ ] Write workflow integration tests:
  - [x] Complete authentication lifecycle (register, login, refresh, logout)
  - [ ] Complete shipment lifecycle
  - [ ] Multi-node consensus
  - [ ] Delivery confirmation workflow
- [x] Test authentication and authorization
- [ ] Test blockchain synchronization between nodes

#### TODO: End-to-End Tests
- [ ] Set up Playwright or Selenium for E2E tests
- [ ] Write UI workflow tests:
  - [ ] User registration and login
  - [ ] Shipment creation
  - [ ] Shipment tracking
  - [ ] Delivery confirmation
  - [ ] Blockchain explorer navigation
- [ ] Test multi-user scenarios
- [ ] Test role-based access control in UI

#### TODO: Security Tests
- [ ] Implement penetration testing checklist
- [ ] Test authentication bypass attempts
- [ ] Test authorization violations
- [ ] Test SQL injection vulnerabilities
- [ ] Test XSS vulnerabilities
- [ ] Test CSRF protection
- [ ] Test rate limiting
- [ ] Verify cryptographic implementations

#### TODO: Performance Tests
- [ ] Create performance test project
- [ ] Test blockchain performance with large number of blocks
- [ ] Test transaction throughput
- [ ] Test concurrent user scenarios
- [ ] Test database query performance
- [ ] Identify and document bottlenecks

---

### 11. Documentation & Deployment

#### TODO: Technical Documentation
- [ ] Write API documentation (Swagger/OpenAPI)
- [ ] Create architecture diagram
- [ ] Document blockchain structure
- [ ] Write consensus algorithm documentation
- [ ] Create database schema documentation
- [ ] Write deployment guide
- [ ] Create developer setup guide
- [ ] Document security best practices

#### TODO: User Documentation
- [ ] Write user guide for each role:
  - [ ] Donor guide
  - [ ] Coordinator guide
  - [ ] Logistics Partner guide
  - [ ] Recipient guide
- [ ] Create FAQ document
- [ ] Write troubleshooting guide

#### TODO: Deployment Configuration
- [ ] Update Docker configuration for multi-container setup:
  - [ ] API container
  - [ ] Blazor container
  - [ ] Database container
  - [ ] Multiple validator node containers
- [ ] Create docker-compose for complete system
- [ ] Configure environment variables
- [ ] Set up database initialization scripts
- [ ] Create validator node setup scripts
- [ ] Configure HTTPS certificates for production

---

## Success Criteria

The prototype will be considered successful when it demonstrates:

- ✓ Creation and storage of shipments in blockchain
- ✓ Immutability of blockchain data (blocks cannot be modified)
- ✓ Transaction verification through digital signatures
- ✓ Multi-role user access with different permissions
- ✓ Complete shipment lifecycle from creation to delivery confirmation
- ✓ Basic PoA consensus mechanism working across validator nodes
- ✓ Transparency: ability to trace any shipment through blockchain
- ✓ Smart contract execution (at least one working example)

---

## Prototype Limitations (Out of Scope)

The following features are NOT included in this prototype but may be considered for future versions:

- IoT device integration (GPS tracking, temperature sensors)
- Offline functionality and sync
- Complex smart contracts (escrow, automated penalties, insurance)
- ERP system integration
- Mobile applications (MAUI)
- Advanced encryption schemes (homomorphic encryption, zero-knowledge proofs)
- Sharding or horizontal scaling
- gRPC inter-node communication (using HTTP/JSON instead)
- Complex P2P network topology
- Advanced consensus mechanisms (beyond basic PoA)
- Real QR code scanning with camera (simulation only)
- Multi-language support (i18n)
- Advanced analytics and machine learning

---

## Development Guidelines

When implementing features:

1. **Follow SOLID principles** and clean architecture patterns
2. **Write tests first** (TDD approach recommended) or immediately after implementation
3. **Use async/await** for all I/O operations
4. **Implement proper error handling** with custom exceptions
5. **Add logging** using ILogger for all critical operations
6. **Document public APIs** with XML comments
7. **Follow C# coding conventions** and use nullable reference types
8. **Validate all inputs** at API boundaries
9. **Use dependency injection** for all services
10. **Keep blockchain operations atomic** and consistent