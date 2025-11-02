# Blockchain Aid Tracker

A .NET 9.0 blockchain-based humanitarian aid supply chain tracking system demonstrating decentralized control, transparency, and Proof-of-Authority consensus.

## Project Status

Currently in early development stage. The project has been restructured following clean architecture principles with separate projects for core logic, blockchain implementation, data access, services, API, and web UI.

## Quick Start

### Prerequisites
- .NET 9.0 SDK
- Docker (optional, for containerized deployment)
- SQLite or PostgreSQL

### Build and Run

```bash
# Build the entire solution
dotnet build blockchain-aid-tracker.sln

# Run the API
dotnet run --project src/BlockchainAidTracker.Api/BlockchainAidTracker.Api.csproj

# Run the Web UI
dotnet run --project src/BlockchainAidTracker.Web/BlockchainAidTracker.Web.csproj
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
│   ├── BlockchainAidTracker.Core/         # Domain models and interfaces
│   ├── BlockchainAidTracker.Blockchain/   # Blockchain engine
│   ├── BlockchainAidTracker.Cryptography/ # Cryptographic utilities
│   ├── BlockchainAidTracker.DataAccess/   # Entity Framework Core
│   ├── BlockchainAidTracker.Services/     # Business logic
│   ├── BlockchainAidTracker.Api/          # Web API
│   └── BlockchainAidTracker.Web/          # Blazor UI
├── tests/                                  # Test projects (to be added)
├── docs/                                   # Documentation
└── CLAUDE.md                               # Detailed implementation roadmap
```

See [PROJECT_STRUCTURE.md](docs/PROJECT_STRUCTURE.md) for detailed architecture information.

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

The project follows a comprehensive implementation roadmap detailed in [CLAUDE.md](CLAUDE.md). Major milestones include:

1. Core Architecture Setup ✓
2. User Management System
3. Blockchain Core Implementation
4. Proof-of-Authority Consensus
5. Supply Chain Operations
6. Smart Contracts
7. Web Application UI
8. Security Implementation
9. Testing Strategy
10. Documentation & Deployment

## Documentation

- [CLAUDE.md](CLAUDE.md) - Complete implementation roadmap and development guidelines
- [PROJECT_STRUCTURE.md](docs/PROJECT_STRUCTURE.md) - Architecture and project organization
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