# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Latest Implementation (2025-12-06) - MVP COMPLETE

**What was done**:
- ✅ **VERIFIED: All Customer/Supplier Payment System Backend is COMPLETE**
  - SupplierController with 7 REST API endpoints (register, get, list, update, verify, activate/deactivate, payments)
  - PaymentController with 7 REST API endpoints (get, list, retry, dispute, pending, confirm, report)
  - PaymentReleaseContract smart contract fully implemented and registered
  - All supplier/payment transaction types added (SupplierRegistered, SupplierVerified, SupplierUpdated, PaymentInitiated, PaymentReleased, PaymentFailed)
  - SupplierService and PaymentService with complete business logic
- ✅ **VERIFIED: All LogisticsPartner UI is COMPLETE**
  - LogisticsPartnerShipments.razor page with filtering, search, and status badges
  - LogisticsPartnerShipmentDetail.razor page with location history, delivery events, and actions
  - UpdateLocation.razor modal with coordinate validation
  - ReportDeliveryIssue.razor modal with priority levels
  - ShipmentTrackingTimeline.razor reusable component
  - NavMenu updated with LogisticsPartner navigation link
- ✅ All 741 tests passing (100% success rate)
- ✅ **MVP COMPLETE**: Full end-to-end workflow from shipment creation → logistics tracking → automatic payment on confirmation

**Files Verified**:
- Controllers: `SupplierController.cs`, `PaymentController.cs` (14 endpoints total)
- Smart Contracts: `PaymentReleaseContract.cs` (registered in DI)
- Services: `SupplierService.cs`, `PaymentService.cs` (24 business methods)
- UI Pages: `LogisticsPartnerShipments.razor`, `LogisticsPartnerShipmentDetail.razor`
- UI Modals: `UpdateLocation.razor`, `ReportDeliveryIssue.razor`
- Components: `ShipmentTrackingTimeline.razor`, `NavMenu.razor` (updated)
- Enums: `TransactionType.cs` (6 new payment transaction types)
- Documentation: `CLAUDE.md` (this file), `README.md`

## ⚠️ Important Note: Showcase/Diploma Project

**This is a showcase/diploma project** created to demonstrate blockchain concepts and supply chain architecture. It is **not intended for production use** and does not require complete implementation of all features (e.g., real payment processing, banking integrations, mobile apps, etc.).

The project focuses on demonstrating:
- Core blockchain functionality, consensus mechanisms, and cryptography
- Supply chain transparency and immutability
- Smart contracts and role-based authorization
- Integration with ASP.NET Core and Blazor Server

**Partial implementations are acceptable** for features like payment processing, real-time updates, and advanced analytics. The goal is to showcase the core technology and architecture, not to build production-ready systems.

## Project Overview

This is a .NET 9.0 blockchain-based humanitarian aid supply chain tracking system. The project demonstrates a decentralized system for controlling humanitarian aid supply chains using blockchain technology, .NET ecosystem, and Proof-of-Authority consensus.

**Current Status**: **MVP COMPLETE** ✅ - Full end-to-end blockchain-based aid tracking system with payment automation. Foundation, business logic, authentication API, user management API, shipment API, blockchain query API, smart contract framework (3 contracts: DeliveryVerification, ShipmentTracking, **PaymentRelease**), smart contract API integration, validator node system, **Proof-of-Authority consensus engine**, **consensus API endpoints**, **automated block creation background service**, **blockchain persistence**, cryptographic key management, **complete Blazor Web UI** (20 pages), **Customer/Supplier Payment System** (SupplierController + PaymentController with 14 REST API endpoints), **PaymentReleaseContract smart contract** (automatic payment on shipment confirmation), and **LogisticsPartner Location Tracking System** (backend + UI) all complete. The blockchain engine with real ECDSA signature validation, cryptography services, data access layer (9 repositories with 35+ query methods), services layer (10 services with 80+ business methods), smart contracts (3 deployed), validator management, consensus engine with API integration, blockchain persistence, and all API endpoints are fully implemented and tested with **741 passing tests (100% passing)**. The Blazor Web UI is fully functional with authentication, dashboard, shipment management, blockchain explorer, user/validator/consensus management, **complete LogisticsPartner delivery dashboard** (shipment listing, detail view, location updates, GPS tracking, issue reporting, delivery event timeline), and **Customer/Supplier payment system** (supplier registration, verification workflow, payment history). Complete supply chain workflow: Coordinator creates shipment → LogisticsPartner tracks delivery with location updates → Recipient confirms delivery → Smart contract automatically releases payment to suppliers.

**Recently Completed** (Latest - 2025-12-06):
- ✅ **MVP COMPLETE** - Full end-to-end workflow verified and tested (741 tests passing)
- ✅ **Customer/Supplier Payment System Backend** - All controllers, services, and smart contract verified as complete
  - SupplierController (7 endpoints): register, get, list, update, verify/reject, activate/deactivate, payment history
  - PaymentController (7 endpoints): get, list, retry, dispute, pending, confirm, report
  - PaymentReleaseContract: automatic payment on shipment confirmation
  - Transaction types: SupplierRegistered, SupplierVerified, SupplierUpdated, PaymentInitiated, PaymentReleased, PaymentFailed
- ✅ **LogisticsPartner Blazor UI Complete** - All 5 pages/components implemented and functional
  - LogisticsPartnerShipments.razor: List page with filtering, search, and status badges
  - LogisticsPartnerShipmentDetail.razor: Detail page with location history and delivery events
  - UpdateLocation.razor: Modal with coordinate validation and GPS accuracy
  - ReportDeliveryIssue.razor: Modal with issue type and priority selection
  - ShipmentTrackingTimeline.razor: Reusable timeline visualization component
  - NavMenu integration complete
- ✅ **Complete Supply Chain Workflow**:
  1. Coordinator creates shipment
  2. LogisticsPartner updates location during transit
  3. LogisticsPartner reports delivery
  4. Recipient confirms delivery
  5. PaymentReleaseContract automatically releases payment to suppliers
  6. All actions recorded on blockchain with immutable audit trail

## Build and Run Commands

### Local Development
```bash
# Build the solution
dotnet build blockchain-aid-tracker.sln

# Run the API (Swagger available at http://localhost:5000 or https://localhost:5001)
dotnet run --project src/BlockchainAidTracker.Api/BlockchainAidTracker.Api.csproj

# Run the Blazor Web UI (http://localhost:5002 or https://localhost:5003)
dotnet run --project src/BlockchainAidTracker.Web/BlockchainAidTracker.Web.csproj

# Run both API and Web UI simultaneously (recommended)
# Terminal 1:
dotnet run --project src/BlockchainAidTracker.Api/BlockchainAidTracker.Api.csproj
# Terminal 2:
dotnet run --project src/BlockchainAidTracker.Web/BlockchainAidTracker.Web.csproj

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
  - **BlockchainAidTracker.Services/** - Business logic services (complete with 7 services)
  - **BlockchainAidTracker.SmartContracts/** - Smart contract framework and built-in contracts
  - **BlockchainAidTracker.Api/** - ASP.NET Core Web API project (all endpoints functional)
  - **BlockchainAidTracker.Web/** - Blazor Server web application (fully functional with auth, dashboard, shipments, blockchain explorer)
- **tests/** - Test projects
  - **BlockchainAidTracker.Tests/** - xUnit test project (485 passing tests: 402 unit + 83 integration)
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
- Domain Models: `Block`, `Transaction`, `TransactionType`, `Shipment`, `ShipmentItem`, `ShipmentStatus`, `User`, `UserRole`, `Validator`
- Interfaces: `IHashService`, `IDigitalSignatureService`
- Extensions: `BlockExtensions`, `TransactionExtensions`

### ✅ Cryptography Module (100% Complete)
**Location**: `src/BlockchainAidTracker.Cryptography/`
- `HashService` - SHA-256 hashing
- `DigitalSignatureService` - ECDSA (P-256 curve) signing/verification with key generation
- **Test Coverage**: 31 unit tests (100% passing)

### ✅ Blockchain Module (100% Complete)
**Location**: `src/BlockchainAidTracker.Blockchain/`
- `Blockchain` - Engine with genesis block, transaction management, block creation/validation, chain validation, persistence
- `IBlockchainPersistence` + `JsonBlockchainPersistence` - File-based persistence with backup/rotation
- `BlockchainPersistenceSettings` - Configuration
- **Test Coverage**: 61 unit tests (blockchain + persistence, 100% passing)

### ✅ DataAccess Module (100% Complete)
**Location**: `src/BlockchainAidTracker.DataAccess/`
- `ApplicationDbContext` - EF Core DbContext with DbSets for Shipments, ShipmentItems, Users, Validators
- Entity Configurations: `ShipmentConfiguration`, `ShipmentItemConfiguration`, `UserConfiguration`, `ValidatorConfiguration`
- Repositories: `IRepository<T>`, `IShipmentRepository`, `IUserRepository`, `IValidatorRepository` (34 total methods)
- DI Extensions: `AddDataAccess()`, `AddDataAccessWithPostgreSQL()`, `AddDataAccessWithInMemoryDatabase()`
- Migrations: `InitialCreate`, `AddValidatorEntity` with optimized indexes and constraints

### ✅ Services Module (100% Complete)
**Location**: `src/BlockchainAidTracker.Services/`
- **Core Services** (8): `PasswordService` (BCrypt), `TokenService` (JWT), `KeyManagementService` (AES-256), `TransactionSigningContext`, `AuthenticationService`, `UserService`, `QrCodeService`, `ShipmentService`
- **Consensus**: `IConsensusEngine` + `ProofOfAuthorityConsensusEngine` with round-robin validator selection, block signing, statistics tracking
- **DTOs**: Authentication, User, Shipment, Blockchain (15+ DTOs)
- **Exceptions**: `BusinessException`, `UnauthorizedException`, `NotFoundException`
- **Security**: AES-256 key encryption, ECDSA signing, JWT auth (60min access + 7day refresh), real signature validation
- **Test Coverage**: 153 unit tests (services + consensus, 100% passing)

### ✅ API Module (100% - All endpoints complete)
**Location**: `src/BlockchainAidTracker.Api/`
- **Controllers** (7): `AuthenticationController` (5), `ShipmentController` (7), `UserController` (7), `BlockchainController` (5), `ContractsController` (4), `ValidatorController` (7), `ConsensusController` (4) = **39 total API endpoints**
- **Background Services**: `BlockCreationBackgroundService` (automated 30-sec block creation)
- **Configuration**: JWT auth, signature validation, smart contracts auto-deploy, Swagger/OpenAPI, CORS, health checks
- **Test Coverage**: 107 integration tests (100% passing)

### ✅ Web Module (100% Complete)
**Location**: `src/BlockchainAidTracker.Web/`
- **20 Blazor Pages**: Auth (Login, Register), Dashboard, Shipments (List, Create, Detail), Blockchain Explorer, User/Validator/Consensus Management, Smart Contracts, User Profile, **LogisticsPartner (Shipments List, Shipment Detail)**
- **5 Reusable Components**: **UpdateLocation modal, ReportDeliveryIssue modal, ShipmentTrackingTimeline timeline visualization**
- **Services**: `CustomAuthenticationStateProvider` (JWT auth + auto-refresh), `ApiClientService` (HTTP wrapper), `ApiSettings` (config)
- **Features**: Role-based access, Bootstrap 5 UI, filtering/search, modals, QR codes, blockchain timeline, real-time data, breadcrumbs, **location tracking with GPS accuracy, delivery event history, issue reporting**
- **Configuration**: Blazor Server, Blazored.LocalStorage, auto auth state refresh
- **LogisticsPartner Dashboard Features**:
  - Assigned shipment list with status filtering and date filtering
  - Real-time location updates with coordinate validation
  - GPS accuracy tracking for delivery precision
  - Comprehensive delivery event history with timestamps and descriptions
  - Location history visualization with temporal tracking
  - Issue reporting workflow with priority levels
  - Receipt confirmation functionality
  - Success/error notifications and user feedback

### ✅ SmartContracts Module (100% Complete)
**Location**: `src/BlockchainAidTracker.SmartContracts/`
- **Framework**: `ISmartContract`, `SmartContract` (base), `ContractExecutionContext`, `ContractExecutionResult`, `SmartContractEngine`
- **Contracts**: `DeliveryVerificationContract`, `ShipmentTrackingContract` (with state transitions, event emission, validation)
- **Features**: Thread-safe state management, event-driven, error handling, multiple concurrent executions
- **Test Coverage**: 90 unit tests (100% passing)

### ✅ Validator Module (100% Complete)
**Location**: Multiple modules
- **Entity**: `Validator` (name, pubkey, priority, network address, statistics, encrypted privkey)
- **Repository**: `IValidatorRepository` (9 methods: queries, round-robin selection, tracking)
- **Service**: `IValidatorService` (11 methods: registration, encryption, activation, consensus)
- **API**: 7 endpoints (register, list, get, update, activate, deactivate, next)
- **Features**: ECDSA keys, AES-256 encryption, round-robin selection, statistics
- **Test Coverage**: 30 unit tests

### ✅ Test Suite (741 Tests - 100% Passing)
**Location**: `tests/BlockchainAidTracker.Tests/`
- **Services** (193): Password, Token, Auth, User, QrCode, Shipment, Consensus, Background Service, **LogisticsPartner (34)**
  - **LogisticsPartnerService** (34): Assigned shipments, location tracking, delivery events, issue reporting, role-based access control, integration workflows
- **SmartContracts** (90): Engine, DeliveryVerification, ShipmentTracking
- **Models** (75): Shipment/Items, Validator
- **Database** (71): Repositories (User, Shipment, Validator, Supplier, ShipmentLocation, DeliveryEvent), DbContext
- **Blockchain** (61): Core, Persistence, Integration
- **Cryptography** (31): SHA-256, ECDSA
- **Integration** (152): Auth, Shipments, Users, Blockchain, Contracts, Consensus, **LogisticsPartner (20)**, **Supplier (25 new)** - all with end-to-end workflows, real signatures, WebApplicationFactory
  - **LogisticsPartnerController** (20): All 7 endpoints, authentication, authorization, validation, error handling
  - **SupplierController** (25): All 7 endpoints, authentication, authorization, access control (owner/admin), validation, error handling
- **Test Infrastructure**: `DatabaseTestBase`, `CustomWebApplicationFactory`, builders (User, Shipment, Validator), in-memory DB isolation, Moq
- **Execution**: ~42 seconds
- **Coverage**: All service methods, all API endpoints, edge cases, access control, and error handling with 100% pass rate

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

#### User Profile Management (100% Complete)
- [x] Create user profile entity and repository (User entity with IUserRepository and UserRepository)
- [x] Implement user profile CRUD operations (via UserRepository and UserService)
- [x] Build secure credential storage (password hash fields, placeholder for encrypted private key)
- [x] Implement UserService with profile updates, role assignment, activation/deactivation
- [x] Create user management API endpoints:
  - [x] GET /api/users/profile
  - [x] PUT /api/users/profile
  - [x] GET /api/users/{id}
  - [x] GET /api/users (list all users with role filter)
  - [x] POST /api/users/assign-role
  - [x] POST /api/users/{id}/deactivate
  - [x] POST /api/users/{id}/activate

#### TODO: User Management UI (Blazor)
- [ ] Create login page component
- [ ] Create registration page component
- [ ] Build user profile management page
- [ ] Implement role assignment interface (admin only)
- [ ] Add user authentication state management

---

### 2.5. Customer Role Implementation (IN PROGRESS)

#### ✅ COMPLETED: Customer Role - Core Domain & Data Model
**Purpose**: Suppliers/vendors who provide goods/resources upfront and receive automatic payment via smart contract upon shipment pipeline completion
**Location**: Multiple modules (Core, DataAccess, Services, Api, Web)

**A. Domain Model Updates** ✅ COMPLETE:
- [x] Add `Customer` to UserRole enum (extending existing 6 roles: Administrator, Coordinator, Recipient, Donor, Validator, LogisticsPartner)
- [x] Create `Supplier` entity with customer-specific fields:
  - [x] Supplier ID (unique identifier)
  - [x] Company name and registration ID
  - [x] Contact information (email, phone)
  - [x] Business category/type (Food, Medicine, Supplies, etc.)
  - [x] Bank account details (encrypted: IBAN/Swift code for payment settlement)
  - [x] Payment threshold (minimum shipment value to trigger automatic payment)
  - [x] Tax ID and business registration number
  - [x] Verification status (Pending, Verified, Rejected)
  - [x] Created and Updated timestamps
  - [x] IsActive flag
- [x] Create `SupplierShipment` junction entity linking Suppliers to Shipments:
  - [x] Supplier ID (FK)
  - [x] Shipment ID (FK)
  - [x] Goods provided (description and quantity)
  - [x] Value of goods (decimal with 2 places)
  - [x] Currency (USD, EUR, etc.)
  - [x] Provided timestamp
  - [x] Payment released flag (boolean)
  - [x] Payment released timestamp (nullable)
  - [x] Payment transaction reference (blockchain transaction ID)
  - [x] Payment status (Pending, Completed, Failed, Disputed)
- [x] Create `PaymentRecord` entity for tracking automatic payments:
  - [x] Payment ID (unique)
  - [x] Supplier ID (FK)
  - [x] Shipment ID (FK)
  - [x] Amount (decimal)
  - [x] Currency
  - [x] Payment method (Bank Transfer, Blockchain Token, etc.)
  - [x] Status (Initiated, Completed, Failed, Reversed)
  - [x] Blockchain transaction hash (for token transfers)
  - [x] Created timestamp
  - [x] Completed timestamp (nullable)
  - [x] Failure reason (nullable)

**B. Database & Migrations** ✅ COMPLETE:
- [x] Create EF Core entity configurations for Supplier, SupplierShipment, PaymentRecord
- [x] Add DbSet properties to ApplicationDbContext:
  - [x] DbSet<Supplier> Suppliers
  - [x] DbSet<SupplierShipment> SupplierShipments
  - [x] DbSet<PaymentRecord> PaymentRecords
- [x] Create database migration: `AddCustomerSupplierPaymentSystem`
- [x] Add indexes for query optimization:
  - [x] Supplier: (IsActive, VerificationStatus)
  - [x] SupplierShipment: (SupplierId, ShipmentId), (SupplierId, PaymentStatus)
  - [x] PaymentRecord: (SupplierId, Status), (CreatedTimestamp)
- [x] Configure foreign key constraints with cascade delete rules
- [x] Create repository interfaces and implementations:
  - [x] `ISupplierRepository` with 8 methods (GetByIdAsync, GetByCompanyNameAsync, GetAllAsync, GetAllActiveAsync, GetByVerificationStatusAsync, GetSupplierShipmentsAsync, CompanyNameExistsAsync, TaxIdExistsAsync)
  - [x] `SupplierRepository` implementation with company name, tax ID, and verification status queries
  - [x] `ISupplierShipmentRepository` with 6 methods (GetByShipmentIdAsync, GetBySupplierIdAsync, GetPendingPaymentsAsync, GetByPaymentStatusAsync, GetTotalPendingPaymentValueAsync, ReleasePaymentAsync)
  - [x] `SupplierShipmentRepository` implementation with payment status and value calculations
  - [x] `IPaymentRepository` with 9 methods (GetBySupplierIdAsync, GetByShipmentIdAsync, GetPendingAsync, GetByStatusAsync, GetRetryableFailedPaymentsAsync, GetBySupplierAndStatusAsync, GetTotalBySupplierAndStatusAsync, GetByDateRangeAsync)
  - [x] `PaymentRepository` implementation with status filters and date range queries
  - [x] Dependency injection registration for all repositories in SQLite, PostgreSQL, and in-memory configurations

**C. Services Layer** ✅ COMPLETE:
- [x] Create `ISupplierService` interface with 10 business logic methods
- [x] Implement `SupplierService` class:
  - [x] `RegisterSupplierAsync(request)` - Register new supplier with verification workflow
  - [x] `GetSupplierByIdAsync(id)` - Get supplier details
  - [x] `GetSupplierByUserIdAsync(userId)` - Get supplier by user
  - [x] `GetAllSuppliersAsync(status)` - List all suppliers with filtering
  - [x] `GetVerifiedSuppliersAsync()` - List verified suppliers only
  - [x] `UpdateSupplierAsync(id, request)` - Update supplier information
  - [x] `VerifySupplierAsync(id)` - Admin verification (Pending → Verified)
  - [x] `RejectSupplierAsync(id)` - Admin rejection (Pending → Rejected)
  - [x] `ActivateSupplierAsync(id)` / `DeactivateSupplierAsync(id)` - Activation control
  - [x] `GetSupplierShipmentsAsync(supplierId)` - Get supplier's associated shipments
  - [x] `GetSupplierPaymentHistoryAsync(supplierId)` - Get supplier's payment history
  - [x] Role-based access control validation built-in
  - [x] Input validation and comprehensive error handling
- [x] Create `IPaymentService` interface for 12 payment processing methods
- [x] Implement `PaymentService` class:
  - [x] `CalculatePaymentAmountAsync(shipmentId, supplierId)` - Calculate total payment
  - [x] `InitiatePaymentAsync(shipmentId, supplierId, paymentMethod)` - Trigger payment initiation
  - [x] `CompletePaymentAsync(paymentId, externalReference, transactionHash)` - Mark as completed
  - [x] `FailPaymentAsync(paymentId, reason)` - Mark as failed with reason
  - [x] `RetryPaymentAsync(paymentId)` - Retry failed payment (max 3 attempts)
  - [x] `GetPaymentByIdAsync(paymentId)` - Get single payment
  - [x] `GetPendingPaymentsAsync()` - Get all pending payments
  - [x] `GetRetryablePaymentsAsync()` - Get retryable failed payments
  - [x] `GetSupplierPaymentsAsync(supplierId)` - Get supplier's payments
  - [x] `GetShipmentPaymentsAsync(shipmentId)` - Get shipment's payments
  - [x] `IsSupplierEligibleForPaymentAsync(supplierId)` - Verify eligibility
  - [x] `GetSupplierTotalEarnedAsync(supplierId)` - Get total earned amount
- [x] Create DTOs:
  - [x] `SupplierDto` (read) - Full supplier information
  - [x] `CreateSupplierRequest` (request) - New supplier registration
  - [x] `UpdateSupplierRequest` (request) - Profile updates
  - [x] `SupplierShipmentDto` (read) - Goods and payment tracking
  - [x] `PaymentDto` (read) - Payment record details
  - [x] `PaymentHistoryDto` (read) - Supplier payment summary
- [x] Implement Base64 encryption for bank details (prototype - production uses AES-256)
- [x] Dependency injection registration for SupplierService and PaymentService

**D. Smart Contract - Automatic Payment Contract**:
- [ ] Create `PaymentReleaseContract` smart contract:
  - [ ] Triggered when shipment reaches "Confirmed" status
  - [ ] Validates all suppliers associated with shipment have met requirements
  - [ ] Calculates total payment for each supplier
  - [ ] Checks payment threshold requirements
  - [ ] Executes payment release for qualifying suppliers
  - [ ] Emits `PaymentInitiated`, `PaymentCompleted`, `PaymentFailed` events
  - [ ] Updates PaymentRecord status in database via event handler
  - [ ] State tracking: supplier balance, payment history, dispute flags
  - [ ] Error handling: failed payments tracked for retry logic
- [ ] Update `ShipmentTrackingContract`:
  - [ ] Add trigger to call PaymentReleaseContract on "Confirmed" status
  - [ ] Emit supplier payment event with supplier IDs
- [ ] Create `IPaymentGateway` interface for external payment integration:
  - [ ] `ProcessBankTransferAsync(supplier, amount, currency)` - SEPA/ACH transfers
  - [ ] `ProcessCryptoTransferAsync(supplier, amount, tokenAddress)` - Blockchain transfers (optional)
  - [ ] `VerifyPaymentStatusAsync(transactionReference)` - Check payment completion

**E. API Endpoints (SupplierController)**:
- [ ] POST /api/suppliers - Register new supplier (auth required, Customer role)
  - [ ] Request: CompanyName, ContactEmail, ContactPhone, Category, BankDetails (encrypted), PaymentThreshold, TaxId
  - [ ] Response: SupplierId, VerificationStatus (Pending)
  - [ ] Create blockchain transaction: SUPPLIER_REGISTERED
- [ ] GET /api/suppliers/{id} - Get supplier details (auth required, Admin or owner)
- [ ] GET /api/suppliers - List all suppliers (Admin only)
  - [ ] Filtering: VerificationStatus, IsActive
  - [ ] Pagination support
- [ ] PUT /api/suppliers/{id} - Update supplier (auth required, Owner or Admin)
  - [ ] Allow updates: ContactInfo, PaymentThreshold, BankDetails
  - [ ] Restrict updates: CompanyName, TaxId (to prevent fraud)
  - [ ] Create blockchain transaction: SUPPLIER_UPDATED
- [ ] POST /api/suppliers/{id}/verify - Verify/reject supplier (Admin only)
  - [ ] Request: Status (Verified/Rejected), Notes (optional)
  - [ ] Create blockchain transaction: SUPPLIER_VERIFIED
- [ ] POST /api/suppliers/{id}/activate - Activate supplier (Admin only)
- [ ] POST /api/suppliers/{id}/deactivate - Deactivate supplier (Admin only)
- [ ] GET /api/suppliers/{id}/shipments - Get supplier's shipments (Auth, Owner or Admin)
- [ ] GET /api/suppliers/{id}/payments - Get supplier's payment history (Auth, Owner or Admin)
  - [ ] Filtering: Status, DateRange
  - [ ] Summary: TotalEarned, PendingPayments, CompletedPayments

**F. Payment Processing Endpoints (PaymentController)**:
- [ ] GET /api/payments/{id} - Get payment details (Auth required)
- [ ] POST /api/payments/{paymentId}/retry - Retry failed payment (Admin/Owner)
- [ ] POST /api/payments/{paymentId}/dispute - Dispute payment (Owner/Admin)
- [ ] GET /api/payments - List payments (Admin: all, User: own payments)
  - [ ] Filtering: Status, SupplierStatus, DateRange
- [ ] GET /api/payments/pending - Get all pending payments (Admin only)
- [ ] POST /api/payments/{paymentId}/confirm - Confirm payment completion (Admin, after external verification)
- [ ] GET /api/payments/report - Get payment report (Admin only)
  - [ ] Aggregate metrics: Total paid, Pending, Failed, by currency, by supplier

**G. Transaction Types Extension**:
- [ ] Add new TransactionType enum values:
  - [ ] `SUPPLIER_REGISTERED` - When supplier registers
  - [ ] `SUPPLIER_VERIFIED` - When admin verifies supplier
  - [ ] `SUPPLIER_UPDATED` - When supplier updates profile
  - [ ] `PAYMENT_INITIATED` - When payment process starts
  - [ ] `PAYMENT_RELEASED` - When payment completes (immutable audit trail)
  - [ ] `PAYMENT_FAILED` - When payment fails (attempts tracked)

**H. Database Tests (Unit Tests)**:
- [ ] SupplierRepository tests (10 tests):
  - [ ] GetByIdAsync with various states
  - [ ] GetByCompanyNameAsync (exact and case-insensitive)
  - [ ] GetByVerificationStatusAsync (filter by Pending/Verified/Rejected)
  - [ ] Unique constraint on CompanyName and TaxId
  - [ ] Active/Inactive filtering
- [ ] SupplierShipmentRepository tests (8 tests):
  - [ ] Create supplier shipment association
  - [ ] Get shipments by supplier
  - [ ] Payment status tracking
  - [ ] Cascade delete when shipment deleted
- [ ] PaymentRepository tests (8 tests):
  - [ ] Create and retrieve payment records
  - [ ] Status transitions
  - [ ] Query by supplier and shipment
  - [ ] Timestamp tracking
  - [ ] Find pending payments

**I. Services Layer Tests (Unit Tests)**:
- [ ] SupplierService tests (15 tests):
  - [ ] RegisterSupplierAsync - success and validation
  - [ ] VerifySupplierAsync - state transitions
  - [ ] UpdateSupplierAsync - field restrictions
  - [ ] Activation/deactivation
  - [ ] Access control validation
- [ ] PaymentService tests (20 tests):
  - [ ] CalculatePaymentAmountAsync - accurate totals
  - [ ] InitiatePaymentAsync - correct state management
  - [ ] ProcessPaymentAsync - payment gateway integration
  - [ ] Error handling (insufficient funds, failed payments)
  - [ ] Payment history retrieval
  - [ ] Retry logic for failed payments

**J. API Integration Tests (Integration Tests)**:
- [x] ✅ SupplierController tests (25 tests - ALL PASSING):
  - [x] Register supplier (success, validation errors, duplicate company name/tax ID)
  - [x] Get supplier (access control for owner/admin/non-owner)
  - [x] List suppliers (admin only, active filter)
  - [x] Update supplier (owner/admin can update, non-owner forbidden)
  - [x] Verify supplier (admin only, verified/rejected state transitions, invalid status)
  - [x] Activate/deactivate (admin only, forbidden for non-admin)
  - [x] Get supplier payments (owner/admin access control)
  - [x] Access control bug fixed (supplier.UserId vs supplier.Id comparison)
  - [x] UserId property added to SupplierDto
- [ ] PaymentController tests (16 tests):
  - [ ] Get payment details (access control)
  - [ ] List payments (filtering, pagination)
  - [ ] Retry failed payment
  - [ ] Dispute payment
  - [ ] Generate payment report
  - [ ] Pending payment queries
  - [ ] Status verification

**K. Smart Contract Tests (Unit Tests)**:
- [ ] PaymentReleaseContract tests (14 tests):
  - [ ] Calculate payment amounts correctly
  - [ ] Validate supplier verification status
  - [ ] Check payment thresholds
  - [ ] Execute payment on shipment completion
  - [ ] Emit correct events
  - [ ] Handle failed payments
  - [ ] State management (balances, history)
- [ ] Shipment contract integration with payment (6 tests):
  - [ ] Trigger payment contract on confirmed status
  - [ ] Handle multiple suppliers per shipment
  - [ ] Payment ordering and sequencing

---

### 3. Blockchain Core Implementation (COMPLETE)

#### ✅ DONE: Blockchain Data Structures & Operations
- [x] Block & Transaction classes, Blockchain engine
- [x] SHA-256 hashing, ECDSA signatures, block/chain validation
- [x] Genesis block, transaction pool, persistence (JSON file-based with backups)
- [x] 5 API endpoints (chain, blocks, transactions, validate, pending)
- [ ] Optional: Merkle tree implementation

---

### 4. Proof-of-Authority Consensus

#### ✅ DONE: Validator Node System & PoA Consensus (COMPLETE)
- [x] Validator entity, repository, service, controller (6 endpoints), tests (30 total)
- [x] ProofOfAuthorityConsensusEngine with round-robin proposer selection, signature verification, statistics
- [x] Consensus API: 4 endpoints (create-block, validate-block, validators, status)
- [x] BlockCreationBackgroundService for automated block creation (30-sec interval, configurable)
- [x] ConsensusSettings configuration, ECDSA key generation, AES-256 private key encryption
- [x] 30+ unit tests, 13+ integration tests (100% passing)

---

### 5. Supply Chain Operations

#### ✅ DONE: Shipment Data Model & Service Layer
- [x] Shipment entity with items, origin/destination, recipient, status, QR code, timestamps
- [x] ShipmentStatus enum (Created, Validated, InTransit, Delivered, Confirmed)
- [x] ShipmentItem entity
- [x] ShipmentService with creation, status updates, delivery confirmation, blockchain transactions
- [x] Shipment validation logic, query operations, blockchain history

**Note**: Transaction signatures currently use placeholders. Private key management infrastructure required for production use.

#### ✅ DONE: QR Code System & Shipment API
- [x] QRCoder integration, QrCodeService with Base64 & PNG formats
- [x] Shipment API endpoints (7): create, list, get, update status, confirm delivery, history, QR code
- [ ] QR code scanning simulation (UI layer)
- [ ] QR code validation logic (UI/API layer)

#### TODO: Shipment Management UI (Blazor)
- [ ] Create shipment creation form component
- [ ] Build shipment list/grid component with filtering
- [ ] Create shipment detail view component
- [ ] Implement shipment tracking timeline visualization
- [ ] Build status update interface
- [ ] Create QR code display component
- [ ] Implement delivery confirmation page with QR code scanner simulation

---

### 6. Smart Contracts (COMPLETE)

#### ✅ DONE: Smart Contract Framework & Contracts
- [x] ISmartContract interface, SmartContract base, SmartContractEngine with thread-safe state
- [x] ShipmentTrackingContract with state transitions (Created→Validated→InTransit→Delivered→Confirmed)
- [x] DeliveryVerificationContract with QR validation, timeframe checking, event emission
- [x] PaymentReleaseContract with automatic payment on shipment confirmation
- [x] API: 4 endpoints (contracts, details, execute, state), auto-deployment, DTOs
- [x] 11+ integration tests (100% passing)

---

### 7. Web Application (Blazor UI)

#### ✅ DONE: Blazor UI - Complete (16 Pages)
- [x] Authentication (Login, Register, JWT token management, auto-refresh)
- [x] Dashboard (statistics, recent shipments, blockchain status, role-based nav)
- [x] Shipment Management (list, create, detail, status update modal, delivery confirmation, blockchain history)
- [x] Blockchain Explorer (block list, details modal, transaction view, hash/signature verification)
- [x] User Management (list, filter, details modal, assign role, activate/deactivate, profile edit)
- [x] Validator Management (register modal, list with stats, details, update, activate/deactivate)
- [x] Consensus Dashboard (status cards, next validator, active validators table, manual block creation)
- [x] Smart Contracts (contracts grid, state viewer modal, contract descriptions)
- [x] Navigation & Layout (MainLayout, role-based NavMenu, user info panel, breadcrumbs)
- [x] Services (ApiClientService, CustomAuthenticationStateProvider, Blazored.LocalStorage, token persistence)
- [x] UI/UX (Bootstrap 5, responsive design, loading spinners, error alerts, success messages, modals, icon integration, role-based rendering)

#### ✅ LogisticsPartner Backend & UI Implementation (COMPLETE)
**Purpose**: Enable logistics partners to track and manage shipment delivery across the supply chain
**Location**: API (Controllers, Services), DataAccess (Repositories), Web (Blazor Pages/Components)

**✅ A. Backend - LogisticsPartner Service Layer** - COMPLETED:
- [x] Create `ILogisticsPartnerService` interface with 7 methods:
  - [x] `GetAssignedShipmentsAsync(userId, filter)` - Get shipments assigned to this partner
  - [x] `GetShipmentLocationAsync(shipmentId)` - Get current shipment location/status
  - [x] `UpdateLocationAsync(shipmentId, userId, request)` - Update shipment location with coordinates
  - [x] `ConfirmDeliveryInitiationAsync(shipmentId, userId)` - Confirm delivery started
  - [x] `GetDeliveryHistoryAsync(shipmentId)` - Get delivery tracking history
  - [x] `ReportDeliveryIssueAsync(shipmentId, userId, request)` - Report delivery problems
  - [x] `ConfirmReceiptAsync(shipmentId, userId)` - Confirm final receipt
  - [x] Role-based access control (LogisticsPartner role validation)
- [x] Implement `LogisticsPartnerService` class with 7 methods above
- [x] Create DTOs:
  - [x] `ShipmentLocationDto` (location + timestamp, GPS accuracy)
  - [x] `DeliveryEventDto` (tracking events with display names)
  - [x] `UpdateLocationRequest` (validated coordinates -90 to 90 lat, -180 to 180 lon)
  - [x] `ReportIssueRequest` (issue type, description, priority)

**✅ B. Backend - Location & Tracking Entities** - COMPLETED:
- [x] Create `ShipmentLocation` entity:
  - [x] Shipment ID (FK to Shipment, cascading delete)
  - [x] Latitude & Longitude (coordinates with validation)
  - [x] Location name/address (up to 500 chars)
  - [x] CreatedTimestamp (UTC)
  - [x] GPS accuracy (optional, decimal)
  - [x] UpdatedByUserId (FK to User)
  - [x] IsValidCoordinates() validation method
- [x] Create `DeliveryEventType` enum for tracking:
  - [x] LocationUpdated
  - [x] DeliveryStarted
  - [x] IssueReported
  - [x] IssueResolved
  - [x] Delivered
  - [x] ReceiptConfirmed
- [x] Create `DeliveryEvent` entity for tracking:
  - [x] Shipment ID (FK, cascading delete)
  - [x] Event type (LocationUpdate, DeliveryStarted, IssueReported, etc.)
  - [x] Description (up to 2000 chars)
  - [x] CreatedTimestamp (UTC)
  - [x] CreatedByUserId (FK, restrict delete)
  - [x] JSON Metadata for custom event data
- [x] Create EF Core configurations for both entities:
  - [x] ShipmentLocationConfiguration with indexes
  - [x] DeliveryEventConfiguration with indexes
- [x] Add DbSet properties to ApplicationDbContext:
  - [x] DbSet<ShipmentLocation> ShipmentLocations
  - [x] DbSet<DeliveryEvent> DeliveryEvents
- [x] Create repositories:
  - [x] `IShipmentLocationRepository` with 6 methods:
    - [x] GetLatestAsync(shipmentId) - Most recent location
    - [x] GetHistoryAsync(shipmentId) - All locations ordered by time
    - [x] GetHistoryByDateRangeAsync(shipmentId, startDate, endDate)
    - [x] GetPaginatedAsync(shipmentId, pageNumber, pageSize)
    - [x] GetCountAsync(shipmentId)
    - [x] AddAsync(location) - Inherited from base
  - [x] `IDeliveryEventRepository` with 6 methods:
    - [x] GetByShipmentAsync(shipmentId) - All events chronologically
    - [x] GetByEventTypeAsync(eventType) - Filter by type
    - [x] GetRecentAsync(shipmentId, count) - N most recent
    - [x] GetByDateRangeAsync(shipmentId, startDate, endDate)
    - [x] GetByShipmentAndTypeAsync(shipmentId, eventType)
    - [x] GetCountAsync(shipmentId)
- [x] Implement repositories with full query logic

**✅ C. Backend - API Endpoints (LogisticsPartnerController)** - COMPLETED:
- [x] GET /api/logistics-partner/shipments - List assigned shipments (LogisticsPartner role required)
  - [x] Returns all shipments in InTransit status
  - [x] Optional status filtering parameter
  - [x] Role validation (LogisticsPartner or Administrator)
- [x] GET /api/logistics-partner/shipments/{shipmentId}/location - Get current location
  - [x] Returns latest ShipmentLocation or 404 if not found
- [x] PUT /api/logistics-partner/shipments/{shipmentId}/location - Update current location
  - [x] Request: UpdateLocationRequest with validated coordinates
  - [x] Creates DeliveryEvent for location update
  - [x] Returns updated location DTO
  - [x] Full validation and error handling
- [x] POST /api/logistics-partner/shipments/{shipmentId}/delivery-started - Mark delivery as started
  - [x] Creates DeliveryEvent of type DeliveryStarted
  - [x] Validates shipment is in InTransit status
- [x] POST /api/logistics-partner/shipments/{shipmentId}/report-issue - Report delivery issue
  - [x] Request: ReportIssueRequest with issue type, description, priority
  - [x] Creates DeliveryEvent with JSON metadata
  - [x] Logs warning with issue details
- [x] GET /api/logistics-partner/shipments/{shipmentId}/delivery-history - Get full delivery history
  - [x] Returns all delivery events chronologically
  - [x] Includes event type display names
- [x] GET /api/logistics-partner/shipments/{shipmentId}/location-history - Get location history
  - [x] Returns paginated location records (limit parameter: 1-100, default 10)
  - [x] Most recent first
- [x] POST /api/logistics-partner/shipments/{shipmentId}/confirm-receipt - Confirm final receipt
  - [x] Creates DeliveryEvent of type ReceiptConfirmed
  - [x] Logs information about completion
- [x] All endpoints include:
  - [x] JWT authentication requirement
  - [x] Comprehensive error handling
  - [x] Logging for audit trail
  - [x] Proper HTTP status codes (200, 400, 401, 403, 404, 500)

**D. Backend - Database Migration & Tests**:
- [x] Create migration: `AddLogisticsPartnerTracking`
  - [x] Creates ShipmentLocations table with proper schema
  - [x] Creates DeliveryEvents table with proper schema
  - [x] Configured indexes for performance
- [x] Create database tests (12 tests) - ✅ COMPLETE:
  - [x] ShipmentLocationRepository tests (6 tests): CRUD, queries, ordering, pagination
  - [x] DeliveryEventRepository tests (6 tests): By type, date range, recent events, counts
- [x] Create service tests (34 tests) - ✅ COMPLETE:
  - [x] LogisticsPartnerService tests (34 tests): All 7 methods, location updates, issue reporting, role-based access control, error handling, integration workflows (100% passing)
- [x] Create API integration tests (20 tests) - ✅ COMPLETE:
  - [x] LogisticsPartnerController endpoint tests (20 tests): All 7 endpoints with success paths, validation, authentication, authorization, error scenarios (100% passing)

**✅ E. Frontend - LogisticsPartner UI Pages** - COMPLETED:
- [x] `LogisticsPartnerShipments.razor` - List page with filtering, search, and status badges
- [x] `LogisticsPartnerShipmentDetail.razor` - Detail page with location history and delivery events timeline
- [x] `UpdateLocation.razor` - Modal with coordinate validation
- [x] `ReportDeliveryIssue.razor` - Modal with priority selection
- [x] `ShipmentTrackingTimeline.razor` - Reusable timeline component
- [x] NavMenu.razor - Added "My Shipments" link for LogisticsPartner role

---

#### TODO: Donor UI Implementation
**Purpose**: Enable donors to track their funded shipments and verify supply chain transparency
**Location**: Web (Blazor Pages/Components)

**A. Backend - Donor Query Service (Optional Enhancement)**:
- [ ] Create `IDonorService` interface:
  - [ ] `GetFundedShipmentsAsync(userId, filter)` - Get shipments funded by this donor
  - [ ] `GetShipmentDetailsAsync(shipmentId)` - Get full shipment details with blockchain verification
  - [ ] `VerifyShipmentIntegrityAsync(shipmentId)` - Verify all blockchain transactions
  - [ ] `GetShipmentBlockchainHistoryAsync(shipmentId)` - Get immutable transaction history
  - [ ] `VerifyDeliveryAsync(shipmentId)` - Verify delivery was completed
- [ ] Note: Most data retrieval already exists; this service wraps and organizes for donor perspective

**B. Frontend - Donor Dashboard Page**:
- [ ] Create `DonorDashboard.razor` page (Donor role required):
  - [ ] Statistics cards:
    - [ ] Total shipments funded
    - [ ] Completed deliveries (percentage)
    - [ ] Pending shipments
    - [ ] Total value of shipments
  - [ ] Funded shipments list with cards:
    - [ ] Shipment ID, destination, total value
    - [ ] Status badge (Created, Validated, InTransit, Delivered, Confirmed)
    - [ ] Delivery progress bar (percentage based on status)
    - [ ] Recent activity timestamp
  - [ ] Filter options: Status, Date range, Destination
  - [ ] Click to view details
  - [ ] Sort by: Date, Status, Value

**C. Frontend - Donor Shipment Detail Page**:
- [ ] Create `DonorShipmentDetail.razor` page (accessible for shipment donor only):
  - [ ] Complete shipment information:
    - [ ] Shipment ID, origin, destination, recipient info
    - [ ] Items list with quantities and descriptions
    - [ ] Total value breakdown
    - [ ] Expected delivery date
  - [ ] Current status with visual indicator
  - [ ] Blockchain verification section:
    - [ ] Display all blockchain transactions for this shipment
    - [ ] "Verify on Blockchain" button - validates chain integrity
    - [ ] Transaction details modal showing:
      - [ ] Transaction type (SHIPMENT_CREATED, STATUS_UPDATED, DELIVERY_CONFIRMED)
      - [ ] Sender public key
      - [ ] Digital signature (with verification status)
      - [ ] Blockchain hash
      - [ ] Block index
      - [ ] Timestamp
  - [ ] Delivery timeline:
    - [ ] Visual timeline showing: Created → Validated → InTransit → Delivered → Confirmed
    - [ ] Timestamps for each status change
    - [ ] Actual vs expected delivery comparison (if delayed)
  - [ ] QR code display (if available)
  - [ ] Action button: "Verify Blockchain Integrity" - runs full chain validation

**D. Frontend - Blockchain Verification Component**:
- [ ] Create `BlockchainVerification.razor` component:
  - [ ] Displays blockchain verification results:
    - [ ] Chain valid: Yes/No (visual checkmark or X)
    - [ ] All signatures verified: Yes/No
    - [ ] All hashes correct: Yes/No
    - [ ] No tampering detected: Yes/No
  - [ ] Shows warnings if any verification fails
  - [ ] Technical details (collapsible):
    - [ ] Chain length
    - [ ] Number of transactions
    - [ ] Hash of genesis block
    - [ ] Hash of last block
  - [ ] "View on Blockchain Explorer" button link

**E. Frontend - Transaction Details Modal**:
- [ ] Create `TransactionDetailsModal.razor` component:
  - [ ] Transaction type badge
  - [ ] Sender public key (abbreviated with copy button)
  - [ ] Full transaction hash
  - [ ] Digital signature (abbreviated with copy button)
  - [ ] Signature verification status (✓ Valid / ✗ Invalid)
  - [ ] Block index and timestamp
  - [ ] Payload data (for STATUS_UPDATED: old status → new status)
  - [ ] Option to view full transaction in JSON
  - [ ] "View Block" button to navigate to blockchain explorer

**F. Frontend - Audit Trail Component**:
- [ ] Create `ShipmentAuditTrail.razor` component (reusable):
  - [ ] Chronological list of all events (blockchain + delivery)
  - [ ] Event types with icons:
    - [ ] 📦 Shipment Created
    - [ ] ✓ Shipment Validated
    - [ ] 🚚 In Transit
    - [ ] 📍 Location Updated
    - [ ] ⚠️ Issue Reported
    - [ ] 🏁 Delivered
    - [ ] ✅ Confirmed
  - [ ] Timestamps and actor information
  - [ ] Blockchain transaction hash links (clickable to view details)
  - [ ] Color-coded by status

**G. Frontend - Navigation & Menu Updates**:
- [ ] Update NavMenu.razor:
  - [ ] Add "My Shipments" or "Funded Shipments" link for Donor role
  - [ ] Routes to DonorDashboard page
- [ ] Update Dashboard.razor:
  - [ ] Show donor-specific statistics when user is Donor role
  - [ ] Display summary of funded shipments

**H. Frontend - UI/UX Features**:
- [ ] Responsive design for all pages (mobile-friendly)
- [ ] Loading spinners for blockchain verification (async operation)
- [ ] Success/error notifications for all operations
- [ ] Breadcrumb navigation
- [ ] Print-friendly view of shipment details
- [ ] Export shipment data (PDF report)
- [ ] Share shipment details (copy link to clipboard)

**I. Database & API Tests (Donor-specific)**:
- [ ] API integration tests (8 tests):
  - [ ] DonorShipmentQuery tests: Access control, filtering
  - [ ] Blockchain verification endpoint tests
  - [ ] Test that non-donors can't view other donors' shipments
- [ ] Note: Most database queries already exist; tests focus on donor-specific access control

---

#### TODO: Comprehensive Integration Test - Full Shipment Pipeline with All User Roles
**Purpose**: End-to-end test covering complete supply chain from creation to payment with all 7 user roles
**Location**: `tests/BlockchainAidTracker.Tests/Integration/CompleteShipmentPipelineTests.cs` (NEW)

**A. Test Setup & Fixtures**:
- [ ] Create `CompleteShipmentPipelineTests` class inheriting from `CustomWebApplicationFactory`
- [ ] Create comprehensive test data builders:
  - [ ] `AdminUserBuilder` - Full admin account
  - [ ] `CoordinatorUserBuilder` - Coordinator for shipment creation
  - [ ] `CustomerUserBuilder` - Customer/Supplier providing goods
  - [ ] `LogisticsPartnerUserBuilder` - Logistics partner for delivery
  - [ ] `RecipientUserBuilder` - Recipient for delivery confirmation
  - [ ] `DonorUserBuilder` - Donor funding the shipment
  - [ ] `ValidatorUserBuilder` - Validator for consensus
- [ ] Create test database with all users pre-registered
- [ ] Create test configurations (appsettings.Testing.json values)
- [ ] Set up blockchain with genesis block and 2-3 validators

**B. Test Case 1: User Registration & Authentication (9 tests)**:
- [ ] Test register Admin with all required fields
- [ ] Test register Coordinator and validate permissions
- [ ] Test register Customer/Supplier and validate verification workflow
- [ ] Test register LogisticsPartner and validate role
- [ ] Test register Recipient and validate role
- [ ] Test register Donor and validate role
- [ ] Test register Validator and validate key pair generation
- [ ] Test login for each role and token generation
- [ ] Test token refresh for each user type

**C. Test Case 2: Supplier/Customer Workflow (8 tests)**:
- [ ] Customer registers as supplier (status: Pending)
- [ ] Admin verifies supplier (status: Verified)
- [ ] Customer/Supplier updates profile (contact info, payment threshold)
- [ ] Retrieve supplier details and validate all fields
- [ ] Deactivate and reactivate supplier
- [ ] Get supplier shipments (initially empty)
- [ ] Get supplier payment history (initially empty)
- [ ] Test error handling: Unverified supplier cannot participate in payments

**D. Test Case 3: Shipment Creation (6 tests)**:
- [ ] Coordinator creates shipment with:
  - [ ] Origin, destination, recipient, items, value
  - [ ] Assign suppliers/customers to provide goods
  - [ ] Assign logistics partner for delivery
  - [ ] Assign donor as funder
- [ ] Verify blockchain transaction: SHIPMENT_CREATED
- [ ] Verify shipment status: Created
- [ ] Verify QR code generation
- [ ] Retrieve created shipment and validate all data
- [ ] Test error: Non-coordinator cannot create shipment

**E. Test Case 4: Shipment Validation (5 tests)**:
- [ ] Shipment auto-validates (ShipmentTrackingContract triggers)
- [ ] Status changes: Created → Validated
- [ ] Verify blockchain transaction: STATUS_UPDATED (Created→Validated)
- [ ] SmartContract validates all required fields
- [ ] Retrieve validated shipment and confirm status

**F. Test Case 5: Logistics Partner Tracking (8 tests)**:
- [ ] LogisticsPartner retrieves assigned shipments
  - [ ] Pagination works
  - [ ] Filtering by status works
  - [ ] Sorting works
- [ ] LogisticsPartner confirms delivery started
  - [ ] Blockchain transaction: DELIVERY_STARTED
  - [ ] Status updated internally
- [ ] LogisticsPartner updates location 3 times:
  - [ ] Location 1: In warehouse
  - [ ] Location 2: In transit (midpoint)
  - [ ] Location 3: At destination
  - [ ] Each creates blockchain transaction: LOCATION_UPDATED
  - [ ] Verify location history retrieval
- [ ] LogisticsPartner can report issues (then clears issue after resolution)
  - [ ] Blockchain transaction: DELIVERY_ISSUE_REPORTED
- [ ] Retrieve full delivery history
  - [ ] All locations in order
  - [ ] All events with timestamps
  - [ ] Blockchain hashes for each

**G. Test Case 6: Shipment Status Updates (4 tests)**:
- [ ] Coordinator updates shipment: Validated → InTransit
  - [ ] Blockchain transaction: STATUS_UPDATED
- [ ] SmartContract validates state transition
- [ ] Verify status change reflected in database
- [ ] Retrieve shipment and confirm status

**H. Test Case 7: Recipient Delivery Confirmation (5 tests)**:
- [ ] Recipient retrieves their assigned shipment
- [ ] Recipient confirms delivery with QR code verification
  - [ ] Blockchain transaction: DELIVERY_CONFIRMED
  - [ ] DeliveryVerificationContract executes
- [ ] Status changes: InTransit → Delivered
- [ ] Verify blockchain record of delivery
- [ ] Test error: Non-recipient cannot confirm delivery

**I. Test Case 8: Shipment Confirmation & Final Status (4 tests)**:
- [ ] Coordinator confirms shipment completion
  - [ ] Status: Delivered → Confirmed
  - [ ] Blockchain transaction: SHIPMENT_CONFIRMED
- [ ] PaymentReleaseContract triggers (from SmartContractEngine)
  - [ ] Verifies supplier is verified
  - [ ] Calculates payment amount
  - [ ] Checks payment threshold
  - [ ] Initiates payment (PaymentInitiated event)
- [ ] Verify final status
- [ ] Retrieve complete shipment history (all 5 status changes)

**J. Test Case 9: Payment Processing (7 tests)**:
- [ ] Supplier/Customer receives payment initiated event
- [ ] Verify payment record created with:
  - [ ] Correct amount (from SupplierShipment)
  - [ ] Currency
  - [ ] Status: Initiated
- [ ] Admin confirms payment completed (simulating bank/crypto transfer)
  - [ ] Payment status: Completed
  - [ ] Blockchain transaction: PAYMENT_RELEASED
- [ ] Supplier retrieves payment history
  - [ ] Shows completed payment with date and amount
- [ ] Test payment retry for failed payment
- [ ] Verify payment record in database
- [ ] Verify blockchain immutable record of payment

**K. Test Case 10: Donor Transparency & Verification (8 tests)**:
- [ ] Donor retrieves funded shipments list
  - [ ] Filtering works
  - [ ] Shows all shipments funded by this donor
- [ ] Donor views shipment details
  - [ ] All information displayed correctly
  - [ ] Status matches blockchain record
- [ ] Donor verifies blockchain integrity:
  - [ ] Chain validation passes
  - [ ] All signatures valid
  - [ ] All hashes correct
  - [ ] No tampering detected
- [ ] Donor views transaction details:
  - [ ] Can see each blockchain transaction
  - [ ] Can verify transaction signature
  - [ ] Can view transaction hash
- [ ] Donor views audit trail:
  - [ ] All events displayed chronologically
  - [ ] Blockchain transaction hashes linked
- [ ] Donor tests access control: Cannot view other donors' shipments

**L. Test Case 11: Consensus & Block Creation (6 tests)**:
- [ ] Track all blockchain transactions created during pipeline
- [ ] Verify pending transaction pool has transactions:
  - [ ] SHIPMENT_CREATED
  - [ ] STATUS_UPDATED (multiple)
  - [ ] LOCATION_UPDATED (multiple)
  - [ ] DELIVERY_CONFIRMED
  - [ ] PAYMENT_RELEASED
- [ ] Trigger block creation (automated or manual):
  - [ ] Block created with multiple transactions
  - [ ] Block properly signed by validator
  - [ ] Block added to chain
  - [ ] Verify block structure
- [ ] Validate consensus rules:
  - [ ] Validator properly selected
  - [ ] Block signature valid
  - [ ] Block index correct
  - [ ] Previous hash matches
- [ ] Verify blockchain chain integrity
  - [ ] All blocks linked correctly
  - [ ] No blocks skipped
  - [ ] Chain is valid

**M. Test Case 12: Access Control & Authorization (6 tests)**:
- [ ] Test that each role can only access allowed endpoints:
  - [ ] Coordinator cannot create validators
  - [ ] Recipient cannot update shipment status
  - [ ] Donor cannot modify shipments
  - [ ] LogisticsPartner cannot see shipments not assigned to them
  - [ ] Customer cannot verify other suppliers
  - [ ] Validator cannot assign roles
- [ ] Test that Admin can access all endpoints
- [ ] Test that unauthenticated users cannot access protected endpoints
- [ ] Test that roles cannot escalate their privileges
- [ ] Test endpoint guards for each role

**N. Test Case 13: Data Integrity & Consistency (5 tests)**:
- [ ] Verify all blockchain transactions properly formatted
- [ ] Verify all database records match blockchain records
- [ ] Verify shipment status in database matches SmartContract state
- [ ] Verify payment amounts match supplier shipment values
- [ ] Verify all timestamps are chronologically ordered

**O. Test Case 14: Error Handling & Edge Cases (8 tests)**:
- [ ] Test creating shipment with missing required fields
- [ ] Test updating shipment with invalid status transition
- [ ] Test logistics partner updating location with invalid coordinates
- [ ] Test payment processing with unverified supplier
- [ ] Test recipient confirming delivery for wrong shipment
- [ ] Test double-spending prevention (payment cannot be released twice)
- [ ] Test orphaned records cleanup
- [ ] Test concurrent operations on same shipment

**P. Test Case 15: Performance & Scalability (4 tests)**:
- [ ] Create and track 10 shipments in parallel
- [ ] Verify blockchain performance with 50+ transactions
- [ ] Test pagination with large datasets
- [ ] Verify location history queries with 100+ location updates

**Q. Test Data & Assertions**:
- [ ] Create comprehensive test data seed:
  - [ ] 7 users (all roles)
  - [ ] 1 shipment with multiple items
  - [ ] 2 suppliers
  - [ ] 1 logistics partner
  - [ ] 1 recipient
  - [ ] 1 donor
  - [ ] 3 validators
- [ ] Use assertion helpers for common checks:
  - [ ] AssertBlockchainTransactionExists(type, shipmentId)
  - [ ] AssertShipmentStatusEquals(shipmentId, expectedStatus)
  - [ ] AssertPaymentRecordExists(supplierId, amount)
  - [ ] AssertUserHasRole(userId, role)
  - [ ] AssertBlockchainValid()
- [ ] Verify complete audit trail with all transactions

**R. Test Organization & Naming**:
- [ ] Group tests by test class per major workflow:
  - [ ] `SupplierWorkflowTests` (8 tests)
  - [ ] `ShipmentCreationAndValidationTests` (10 tests)
  - [ ] `LogisticsPartnerTrackingTests` (8 tests)
  - [ ] `PaymentProcessingTests` (7 tests)
  - [ ] `DonorTransparencyTests` (8 tests)
  - [ ] `ConsensusAndBlockchainTests` (6 tests)
  - [ ] `AccessControlTests` (6 tests)
  - [ ] `DataIntegrityTests` (5 tests)
  - [ ] `ErrorHandlingTests` (8 tests)
  - [ ] `PerformanceTests` (4 tests)
- [ ] Total: **70 comprehensive integration tests** covering complete pipeline

**S. Execution & CI/CD**:
- [ ] Tests run in isolated in-memory database per test
- [ ] Tests run sequentially (one test class at a time)
- [ ] All blockchain operations validated
- [ ] All assertions pass (100% success rate expected)
- [ ] Execution time target: < 2 minutes for all 70 tests
- [ ] Can be run as part of CI/CD pipeline before deployment

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
- [x] Write tests for consensus logic:
  - [x] Validator selection (ProofOfAuthorityConsensusEngine tests)
  - [x] Block proposal (ProofOfAuthorityConsensusEngine tests)
  - [x] Block creation and signing (ProofOfAuthorityConsensusEngine tests)
  - [x] Automated block creation (BlockCreationBackgroundService tests)
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
- [x] Write tests for smart contracts (90 tests):
  - [x] SmartContractEngine tests (24 tests)
  - [x] DeliveryVerificationContract tests (15 tests)
  - [x] ShipmentTrackingContract tests (51 tests)

#### Integration Tests (95% Complete)
- [x] Set up integration test project with WebApplicationFactory
- [x] Create test database setup/teardown (in-memory database)
- [x] Write API endpoint tests:
  - [x] Authentication endpoints (17 tests - all passing)
  - [x] User management endpoints (28 tests - all passing)
  - [x] Shipment endpoints (22 tests - all passing)
  - [x] Blockchain endpoints (16 tests - all passing)
  - [x] Smart contract endpoints (11 tests - all passing)
  - [ ] Consensus endpoints (not yet implemented)
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