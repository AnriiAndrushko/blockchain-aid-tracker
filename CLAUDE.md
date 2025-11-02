# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a .NET 9.0 blockchain-based humanitarian aid supply chain tracking system. The project demonstrates a decentralized system for controlling humanitarian aid supply chains using blockchain technology, .NET ecosystem, and Proof-of-Authority consensus.

**Current Status**: Early development stage with Docker containerization support. Core features are marked as TODO items below and will be implemented step by step.

## Build and Run Commands

### Local Development
```bash
# Build the solution
dotnet build blockchain-aid-tracker.sln

# Run the application
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

## Project Structure

- **blockchain-aid-tracker/** - Main console application project
  - `blockchain-aid-tracker.csproj` - .NET 9.0 console app with Docker support
  - `Program.cs` - Application entry point
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

## Implementation Roadmap (TODO)

All features below are planned for step-by-step implementation. Each section represents a major component of the system.

### 1. Core Architecture Setup

#### TODO: Project Structure Reorganization
- [ ] Convert console app to ASP.NET Core Web API project
- [ ] Add Blazor Server project for web interface
- [ ] Create class library projects for:
  - [ ] Core domain models and interfaces
  - [ ] Blockchain engine
  - [ ] Data access layer
  - [ ] Business logic services
  - [ ] Cryptography utilities
- [ ] Set up solution folder structure:
  - [ ] `src/` - Source code
  - [ ] `tests/` - Test projects
  - [ ] `docs/` - Documentation

#### TODO: Technology Stack Setup
- [ ] Add NuGet packages:
  - [ ] ASP.NET Core Web API
  - [ ] Blazor Server
  - [ ] Entity Framework Core
  - [ ] SQLite/PostgreSQL provider
  - [ ] JWT authentication libraries
  - [ ] QR code generation library (QRCoder or similar)
  - [ ] BCrypt.NET or similar for password hashing
- [ ] Configure dependency injection container
- [ ] Set up appsettings.json configuration structure
- [ ] Configure HTTPS and CORS policies

#### TODO: Database Infrastructure
- [ ] Design Entity Framework Core data models
- [ ] Create database context class
- [ ] Implement repository pattern interfaces
- [ ] Set up migrations system
- [ ] Create initial database schema migration
- [ ] Configure connection string management
- [ ] Implement caching mechanism (in-memory cache)

---

### 2. User Management System

#### TODO: User Authentication & Authorization
- [ ] Implement user entity model with roles (Donor, Coordinator, Logistics Partner, Recipient)
- [ ] Create cryptographic key pair generation service (ECDSA)
- [ ] Implement password hashing with bcrypt or PBKDF2
- [ ] Build private key encryption/decryption with user passwords
- [ ] Create JWT token generation and validation service
- [ ] Implement multi-factor authentication framework
- [ ] Build role-based access control (RBAC) middleware
- [ ] Create authentication API endpoints:
  - [ ] POST /api/auth/register
  - [ ] POST /api/auth/login
  - [ ] POST /api/auth/refresh-token
  - [ ] POST /api/auth/logout

#### TODO: User Profile Management
- [ ] Create user profile entity and repository
- [ ] Implement user profile CRUD operations
- [ ] Build secure credential storage
- [ ] Create user management API endpoints:
  - [ ] GET /api/users/profile
  - [ ] PUT /api/users/profile
  - [ ] GET /api/users/{id}
  - [ ] POST /api/users/assign-role
- [ ] Implement user role assignment workflow

#### TODO: User Management UI (Blazor)
- [ ] Create login page component
- [ ] Create registration page component
- [ ] Build user profile management page
- [ ] Implement role assignment interface (admin only)
- [ ] Add user authentication state management

---

### 3. Blockchain Core Implementation

#### TODO: Blockchain Data Structures
- [ ] Create Block class with properties:
  - [ ] Index
  - [ ] Timestamp
  - [ ] Transactions list
  - [ ] Previous hash
  - [ ] Current hash
  - [ ] Nonce (if needed)
  - [ ] Validator signature
- [ ] Create Transaction class with properties:
  - [ ] Transaction ID
  - [ ] Type (SHIPMENT_CREATED, STATUS_UPDATED, DELIVERY_CONFIRMED)
  - [ ] Timestamp
  - [ ] Sender public key
  - [ ] Payload data
  - [ ] Digital signature
- [ ] Create Blockchain class to manage chain operations

#### TODO: Cryptographic Functions
- [ ] Implement SHA-256 hashing for blocks
- [ ] Implement ECDSA digital signature generation
- [ ] Implement ECDSA signature verification
- [ ] Create hash calculation for blocks
- [ ] Build merkle tree implementation (optional for prototype)

#### TODO: Blockchain Operations
- [ ] Implement add transaction to pending pool
- [ ] Implement block creation logic
- [ ] Implement block validation logic
- [ ] Implement chain validation (verify all hashes and signatures)
- [ ] Create genesis block initialization
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

#### TODO: Shipment Data Model
- [ ] Create Shipment entity with properties:
  - [ ] Shipment ID
  - [ ] Item descriptions and quantities
  - [ ] Origin point
  - [ ] Destination point
  - [ ] Expected delivery timeframe
  - [ ] Assigned recipient
  - [ ] Current status
  - [ ] QR code data
  - [ ] Created timestamp
  - [ ] Updated timestamp
- [ ] Create ShipmentStatus enum (Created, Validated, InTransit, Delivered, Confirmed)
- [ ] Create ShipmentItem entity for item details

#### TODO: Shipment Service Layer
- [ ] Create ShipmentService with business logic
- [ ] Implement shipment creation workflow:
  - [ ] Validate user permissions (Coordinator role)
  - [ ] Create shipment record
  - [ ] Generate blockchain transaction (SHIPMENT_CREATED)
  - [ ] Broadcast transaction to validators
- [ ] Implement shipment status update workflow
- [ ] Implement delivery confirmation workflow
- [ ] Build shipment validation logic

#### TODO: QR Code System
- [ ] Integrate QR code generation library
- [ ] Create QR code generation service
- [ ] Generate unique QR codes for shipments
- [ ] Implement QR code scanning simulation
- [ ] Build QR code validation logic

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

#### TODO: Blockchain Security
- [ ] Implement transaction tampering detection
- [ ] Build double-spending prevention
- [ ] Create signature verification for all transactions
- [ ] Implement block validation before adding to chain
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

#### TODO: Unit Tests
- [ ] Set up xUnit test project
- [ ] Create test fixtures and helpers
- [ ] Write tests for cryptographic functions:
  - [ ] SHA-256 hashing
  - [ ] ECDSA signature generation
  - [ ] ECDSA signature verification
  - [ ] Key pair generation
- [ ] Write tests for blockchain operations:
  - [ ] Block creation
  - [ ] Block validation
  - [ ] Chain validation
  - [ ] Transaction creation
  - [ ] Transaction validation
- [ ] Write tests for consensus logic:
  - [ ] Validator selection
  - [ ] Block proposal
  - [ ] Consensus threshold
  - [ ] Fork resolution
- [ ] Write tests for smart contracts:
  - [ ] Shipment tracking state transitions
  - [ ] Delivery verification logic
- [ ] Write tests for services:
  - [ ] UserService
  - [ ] ShipmentService
  - [ ] BlockchainService
  - [ ] AuthenticationService
- [ ] Write tests for repositories (with in-memory database)

#### TODO: Integration Tests
- [ ] Set up integration test project with TestServer
- [ ] Create test database setup/teardown
- [ ] Write API endpoint tests:
  - [ ] Authentication endpoints
  - [ ] User management endpoints
  - [ ] Shipment endpoints
  - [ ] Blockchain endpoints
  - [ ] Consensus endpoints
- [ ] Write workflow integration tests:
  - [ ] Complete shipment lifecycle
  - [ ] Multi-node consensus
  - [ ] Delivery confirmation workflow
- [ ] Test authentication and authorization
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