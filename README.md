# DriveFlow-CRM-API

API for Drive Flow App

## Overview

DriveFlow-CRM-API is a .NET 8.0 web API project designed to manage CRM functionalities for the Drive Flow application. It uses MySQL as the database, with Entity Framework Core for data access.

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- MySQL database is hosted (no local installation required)
- [Visual Studio Code](https://code.visualstudio.com/), [Cursor](https://cursor.so/), or [Visual Studio](https://visualstudio.microsoft.com/)
- [Git](https://git-scm.com/)

## Environment Setup

### .env File

Create a `.env` file in the root directory of the project with the following content:

```plaintext
# .env (only for local development)
ASPNETCORE_ENVIRONMENT=Production
JAWSDB_URL=mysql://<username>:<password>@<host>:<port>/<database>
```

Replace `<username>`, `<password>`, `<host>`, `<port>`, and `<database>` with your JawsDB credentials.

### appsettings.json

Ensure your `appsettings.json` is configured correctly for your environment. You may need to adjust connection strings or other settings based on your deployment.

## Getting Started

1. **Clone the Repository**

   ```bash
   git clone https://github.com/DriveFlow-CRM/DriveFlow-CRM-API.git
   cd DriveFlow-CRM-API
   ```

2. **Restore Dependencies**

   ```bash
   dotnet restore
   ```

3. **Apply Migrations**

   Ensure your database is running and apply any pending migrations:

   ```bash
   dotnet ef database update
   ```

4. **Run the Application**

   Start the application:

   ```bash
   dotnet run
   ```

   The API will be available at `http://localhost:5000`.

## Collaboration

- **Branching**: Use feature branches for new features and bug fixes.
- **Pull Requests**: Submit pull requests for code review before merging into the main branch.
- **Code Style**: Follow the project's coding standards and conventions.

## Dependencies

- **Entity Framework Core**: Version 8.0.0
- **Pomelo.EntityFrameworkCore.MySql**: Version 8.0.0
- **DotNetEnv**: Version 3.1.1
- **Swashbuckle.AspNetCore**: Version 6.6.2

## Contact

For any questions or issues, please contact admin or open an issue on GitHub.
