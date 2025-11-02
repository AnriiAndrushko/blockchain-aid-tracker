# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a .NET 9.0 console application for blockchain-based aid tracking. The project is in early stages with Docker containerization support.

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
- **Output Type**: Console executable
- **Nullable Reference Types**: Enabled
- **Implicit Usings**: Enabled
- **Docker Base Image**: mcr.microsoft.com/dotnet/runtime:9.0
- **Docker SDK Image**: mcr.microsoft.com/dotnet/sdk:9.0