# DriveFlow-CRM-API

REST API for the DriveFlow Customer Relationship Management System

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0.407-5C2D91)](https://dotnet.microsoft.com/download/dotnet/8.0)

## Overview

DriveFlow-CRM-API is a robust .NET 8.0 web API project designed to provide backend services for the DriveFlow Customer Relationship Management system. It leverages Entity Framework Core for data access and is built with a clean, maintainable architecture.

## Features

- RESTful API endpoints for managing customer data
- Secure authentication and authorization
- Database migrations and seeding
- Comprehensive error handling
- Swagger/OpenAPI documentation
- Heroku deployment support
- JawsDB MySQL integration

## Prerequisites

- [.NET SDK 8.0.407](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Git](https://git-scm.com/)
- IDE options:
  - [Visual Studio](https://visualstudio.microsoft.com/)
  - [Visual Studio Code](https://code.visualstudio.com/)
  - [Cursor](https://cursor.so/)
- JawsDB MySQL account (for database hosting)

## Installation Guide

### 1. Clone the Repository

```bash
git clone https://github.com/DriveFlow-CRM/DriveFlow-CRM-API.git
cd DriveFlow-CRM-API
```

### 2. Install .NET SDK 8.0.407

#### Windows
```powershell
winget install Microsoft.DotNet.SDK.8 --version 8.0.407
```
Or download and run the installer from the [.NET download page](https://dotnet.microsoft.com/download/dotnet/8.0).

#### macOS
```bash
# Using Homebrew
brew install --cask dotnet-sdk

# Or download the installer from the .NET download page
```

#### Linux (Ubuntu/Debian)
```bash
# Add Microsoft package repository
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Install .NET SDK
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0
```

### 3. Install Entity Framework Core Tools

Run this command in your terminal or command prompt:

```bash
dotnet tool install --global dotnet-ef
```

### 4. Environment Setup

Create a `.env` file in the root directory of the project:

```plaintext
# .env (only for local development)
ASPNETCORE_ENVIRONMENT=Production
JAWSDB_URL=mysql://<username>:<password>@<host>:<port>/<database>
```

Replace the placeholder values with your actual JawsDB credentials.

### 5. Restore Dependencies

```bash
dotnet restore
```

### 6. Apply Database Migrations

```bash
dotnet ef database update
```

### 7. Run the Application

```bash
dotnet run
```

The API will be available at `http://localhost:5000`.

## API Documentation

Comprehensive API documentation is available in the [API_DOCS.yaml](./docs/API_DOCS.yaml) file, which follows the OpenAPI 3.0 specification.

To view the documentation in a user-friendly format:
1. Visit [Swagger Editor](https://editor.swagger.io/)
2. Import the API_DOCS.yaml file

Or access the Swagger UI by running the application and navigating to:
```
http://localhost:5000/swagger
```

## Database Schema

The application uses JawsDB MySQL as its database provider. The schema is managed through Entity Framework Core migrations.

## Project Structure

```
├── Controllers/           # API Controllers
├── Models/                # Data models and DTOs
├── Services/              # Business logic
├── Middleware/            # Custom middleware
├── Data/                  # Data access layer
├── Migrations/            # EF Core migrations
├── docs/                  # Documentation
└── Properties/            # Project properties
```

## Deployment

The application is configured for deployment to Heroku with JawsDB MySQL add-on.

## Development Workflow

### Branching Strategy

- `main`: Production-ready code
- `develop`: Integration branch for feature development
- `feature/*`: Feature branches
- `bugfix/*`: Bug fix branches

### Pull Request Process

1. Create a branch from `develop`
2. Make your changes
3. Submit a pull request to `develop`
4. Ensure tests pass
5. Obtain code review approval
6. Merge into `develop`

## Dependencies

- **Entity Framework Core**: Version 8.0.0
- **Pomelo.EntityFrameworkCore.MySql**: Version 8.0.0
- **DotNetEnv**: Version 3.1.1
- **Swashbuckle.AspNetCore**: Version 6.6.2

## Contributing

Please read [CONTRIBUTING.md](CONTRIBUTING.md) for details on our code of conduct and the process for submitting pull requests.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contact

For questions or support, please open an issue on GitHub or contact the project maintainers.
