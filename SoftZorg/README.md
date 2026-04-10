# SoftZorg - Healthcare Care Management System Backend

**SoftZorg** is a backend API for a healthcare/care management system, built with ASP.NET Core 10 and designed to support a React frontend. It provides authentication, reporting, and AI-powered features for managing healthcare-related reports (meldingen).

## Overview

This is the backend component of a hackathon project for a care management system. The API handles:
- User authentication and authorization with JWT tokens
- Report (melding) management and tracking
- AI-powered report analysis
- Role-based access control

## Tech Stack

- **Framework**: ASP.NET Core 10
- **Database**: SQL Server with Entity Framework Core
- **Authentication**: JWT Bearer with ASP.NET Core Identity
- **API Documentation**: Swagger/OpenAPI
- **AI Integration**: Google AI API
- **Language**: C#

## Features

### Authentication
- User registration and login
- JWT token generation and validation
- Role-based authorization (Identity Roles)
- Secure password management with Identity

### Report Management (Meldingen)
- Create, read, update, and delete reports
- Track reports in the database
- RESTful API endpoints for report operations

### AI Integration
- AI-powered analysis of reports
- Integration with Google AI API
- Real-time processing capabilities

### API Documentation
- Swagger UI for interactive API exploration
- OpenAPI specifications

## Prerequisites

- .NET 10 SDK
- SQL Server (local or remote)
- Google AI API key

## Project Structure

```
SoftZorg/
├── Controllers/           # API endpoint controllers
│   ├── AuthController.cs  # Authentication endpoints
│   ├── AiController.cs    # AI integration endpoints
│   ├── MeldingenController.cs  # Report management endpoints
│   └── TestController.cs  # Testing endpoints
├── Models/               # Data models
│   └── Melding.cs        # Report model
├── Data/                 # Database context and configuration
│   └── ApplicationDbContext.cs
├── Migrations/           # Entity Framework migrations
├── Program.cs            # Application startup configuration
├── appsettings.json      # Configuration settings
└── appsettings.Development.json  # Development-specific settings
```

## Configuration

### Environment Variables

Set the following environment variable for Google AI API integration:
```
GOOGLEAIKEY=<your-google-ai-api-key>
```

### appsettings.json

Configure your database connection and JWT settings:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=<server>;Database=<database>;Trusted_Connection=true;"
  },
  "Jwt": {
    "Key": "<your-jwt-secret-key>",
    "Issuer": "<your-issuer>",
    "Audience": "<your-audience>"
  }
}
```

## Setup & Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/<repository-url>
   cd SoftZorg
   ```

2. **Install dependencies**
   ```bash
   dotnet restore
   ```

3. **Configure the database**
   - Update the connection string in `appsettings.json`
   - Run migrations:
     ```bash
     dotnet ef database update
     ```

4. **Set environment variables**
   - Set `GOOGLEAIKEY` with your Google AI API key

5. **Run the application**
   ```bash
   dotnet run
   ```

   The API will be available at `https://localhost:5001` (or the configured port)

## API Endpoints

### Authentication
- `POST /api/auth/login` - Login with email and password
- `POST /api/auth/register` - Register a new user

### Reports (Meldingen)
- `GET /api/meldingen` - Get all reports
- `GET /api/meldingen/{id}` - Get a specific report
- `POST /api/meldingen` - Create a new report
- `PUT /api/meldingen/{id}` - Update a report
- `DELETE /api/meldingen/{id}` - Delete a report

### AI
- `POST /api/ai/analyze` - Analyze a report using AI

## CORS Configuration

The API is configured to allow requests from React frontend applications (including Vercel deployments). CORS policy is set to allow any origin with appropriate HTTP methods.

## Authentication Flow

1. User registers or logs in via the `/api/auth/login` endpoint
2. Server generates a JWT token containing user claims and role information
3. Client includes the token in the `Authorization: Bearer <token>` header for subsequent requests
4. Server validates the token for protected endpoints

## Database

The application uses Entity Framework Core with SQL Server for data persistence. Identity management is handled through ASP.NET Core Identity, providing secure user authentication.

### Migrations

Database schema updates are managed through Entity Framework migrations. To create a new migration:

```bash
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

## Development

### Build
```bash
dotnet build
```

### Run Tests
```bash
dotnet test
```

### View API Documentation
Navigate to `/swagger` to view interactive API documentation once the application is running.

## Deployment

The application can be deployed to various platforms:
- Azure App Service
- Docker containers
- IIS
- Other ASP.NET Core-compatible hosting environments

## Contributing

This is a hackathon project. For contributions:
1. Create a feature branch
2. Make your changes
3. Test thoroughly
4. Submit a pull request

## License

Check the repository for license information.

## Support

For issues and questions, please refer to the GitHub repository:
https://github.com/chrisG074/hackathon-zorg-systeem-backend

---

**Note**: This is a hackathon project for a healthcare care management system. Ensure proper security measures and compliance with healthcare regulations when deploying to production.
