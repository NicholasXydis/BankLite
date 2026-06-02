# BankLiteAPI

Banking REST API built with ASP.NET Core 8 and Clean Architecture.

_API REST bancaire construite avec ASP.NET Core 8 et Clean Architecture._

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet)
![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?style=flat&logo=microsoftsqlserver)
![JWT](https://img.shields.io/badge/JWT-Auth-000000?style=flat&logo=jsonwebtokens)
![xUnit](https://img.shields.io/badge/xUnit-Tests-512BD4?style=flat)
![GitHub Actions](https://img.shields.io/badge/CI/CD-GitHub_Actions-2088FF?style=flat&logo=githubactions)

---

## Purpose / Objectif

Built to demonstrate a structured backend API with authentication, financial transactions, and separation of concerns
across multiple layers.

_Conçu pour démontrer une API backend structurée avec authentification, transactions financières et séparation des
responsabilités sur plusieurs couches._

---

## Features / Fonctionnalités

- JWT authentication with BCrypt password hashing — passwords are never stored in plain text
- User registration and login with email normalization (stored in lowercase)
- Chequing and savings account creation with auto-generated account numbers
- Deposit and withdrawal with transaction details returned to client
- Atomic transfers between accounts using Unit of Work — with rollback on failure
- Transaction descriptions stored on every deposit, withdrawal and transfer
- Paginated transaction history ordered by newest first
- FluentValidation on all endpoints — input sanitization before hitting the service layer
- Repository pattern — clean data access abstraction across all entities
- Unit of Work pattern — atomic database operations with commit and rollback
- Rate limiting — 5 attempts/min on login for brute force protection, 30/min globally
- Health check endpoint at `/health` for monitoring
- Audit logging on all financial operations and authentication events
- Structured logging with Serilog (console and file)
- Global exception middleware — 400 for business errors, 500 for server errors
- Swagger UI with JWT authorization button for easy API testing
- Seed data for local testing
- CI/CD pipeline with GitHub Actions — runs all tests on every push

---

## Architecture

BankLiteAPI follows Clean Architecture with separation of concerns across four layers:

**Domain** — Core entities (`User`, `Account`, `Transaction`, `AuditLog`) and repository/service interfaces. Zero
dependencies on other layers.

**Application** — Business logic, DTOs, FluentValidation validators, and service implementations (`AuthService`,
`AccountService`, `TransactionService`).

**Infrastructure** — EF Core repository implementations, `BankLiteDbContext`, `UnitOfWork`, and `SeedData`.

**API** — ASP.NET Core controllers , `ExceptionMiddleware`, and `Program.cs` configuration.

_BankLiteAPI applique une Clean Architecture avec une séparation des responsabilités sur quatre couches : Domain,
Application, Infrastructure et API._

---

## Tech Stack / Technologies

| Layer             | Technology                                             |
| ----------------- | ------------------------------------------------------ |
| Framework         | ASP.NET Core 8                                         |
| Database          | SQL Server + Entity Framework Core 8                   |
| Authentication    | JWT Bearer + BCrypt.Net                                |
| Validation        | FluentValidation                                       |
| Testing           | xUnit + Moq                                            |
| Logging           | Serilog (Console + File)                               |
| API Documentation | Swagger / Swashbuckle                                  |
| CI/CD             | GitHub Actions                                         |
| Security          | Rate Limiting + BCrypt + JWT                           |
| Architecture      | Clean Architecture + Repository Pattern + Unit of Work |

---

## Getting Started / Démarrage

### Prerequisites / Prérequis

- .NET 8 SDK
- SQL Server or SQL Server Express
- EF Core CLI (dotnet-ef)

### Setup / Configuration

**1. Clone the repository / Cloner le dépôt**

```bash
git clone https://github.com/NicholasXydis/BankLiteAPI.git
cd BankLiteAPI/backend/BankLiteAPI
```

**2. Install EF Core CLI / Installer EF Core CLI**

```bash
dotnet tool install --global dotnet-ef
```

**3. Configure your settings / Configurez vos paramètres**

Copy `appsettings.example.json` to `appsettings.json` and fill in your values:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=BankLiteDB;Trusted_Connection=True;"
  },
  "JwtSettings": {
    "Secret": "YOUR_SECRET_KEY_MIN_32_CHARACTERS",
    "Issuer": "BankLiteAPI",
    "Audience": "BankLiteClient",
    "ExpiryMinutes": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

**4. Apply migrations / Appliquer les migrations**

```bash
dotnet ef database update --project ../BankLite.Infrastructure --startup-project BankLite.API
```

**5. Run the API / Lancer l'API**

```bash
dotnet run --project BankLite.API
```

**6. Open Swagger / Ouvrir Swagger**

Navigate to `https://localhost:7205/swagger`

---

## Running Tests / Lancer les tests

```bash
dotnet test
```

13 tests covering:

- Authentication — login and register success and failure paths
- Transactions — deposit, withdraw and transfer
- Authorization — account ownership checks on all transaction operations
- Edge cases — account not found, insufficient funds, wrong password

---

## Example Requests / Exemples de requêtes

**POST /api/auth/login**

```json
{
  "email": "test@banklite.com",
  "password": "Password123"
}
```

Response:

```json
{
  "token": "eyJhbGci...",
  "userId": "3fa85f64-..."
}
```

---

**POST /api/transaction/transfer**

```json
{
  "fromAccountId": "3fa85f64-...",
  "toAccountId": "8fb96g75-...",
  "amount": 250.0
}
```

Response:

```json
{
  "message": "Transfer successful",
  "amount": 250.0
}
```

---

## API Endpoints

### Auth — `/api/auth`

| Method | Endpoint    | Description                 | Auth Required |
| ------ | ----------- | --------------------------- | ------------- |
| POST   | `/register` | Register a new user         | No            |
| POST   | `/login`    | Login and receive JWT token | No            |

### Accounts — `/api/account`

| Method | Endpoint  | Description                             | Auth Required |
| ------ | --------- | --------------------------------------- | ------------- |
| POST   | `/create` | Create a chequing or savings account    | Yes           |
| GET    | `/`       | Get all accounts for authenticated user | Yes           |

### Transactions — `/api/transaction`

| Method | Endpoint                          | Description                                  | Auth Required |
| ------ | --------------------------------- | -------------------------------------------- | ------------- |
| POST   | `/deposit`                        | Deposit funds — returns transaction receipt  | Yes           |
| POST   | `/withdraw`                       | Withdraw funds — returns transaction receipt | Yes           |
| POST   | `/transfer`                       | Atomic transfer between accounts             | Yes           |
| GET    | `/{accountId}?page=1&pageSize=10` | Get paginated transaction history            | Yes           |

### Health

| Method | Endpoint  | Description               |
| ------ | --------- | ------------------------- |
| GET    | `/health` | Returns API health status |

---

## Seed Data / Données de test

On first run, the database is automatically seeded with a test user and two accounts:

| Field            | Value             |
| ---------------- | ----------------- |
| Email            | test@banklite.com |
| Password         | Password123       |
| Chequing Balance | $1,000.00         |
| Savings Balance  | $5,000.00         |

---

## Security / Sécurité

- Passwords hashed with BCrypt — never stored in plain text
- JWT tokens expire after 60 minutes
- Login endpoint rate limited to 5 attempts per minute — brute force protection
- All other endpoints rate limited to 30 requests per minute
- Email addresses normalized to lowercase before storage
- Unit of Work with rollback on transfer failure
- All sensitive configuration stored as environment variables in production
- `appsettings.json` is excluded from version control

---

## Project Structure / Structure du projet

```
BankLiteAPI/
├── backend/
│   ├── BankLite.Domain/
│   │   ├── Entities/          # User, Account, Transaction, AuditLog
│   │   └── Interfaces/        # Repository and service interfaces
│   ├── BankLite.Application/
│   │   ├── DTOs/              # Data transfer objects
│   │   ├── Interfaces/        # Service interfaces
│   │   ├── Services/          # AuthService, AccountService, TransactionService
│   │   └── Validators/        # FluentValidation validators
│   ├── BankLite.Infrastructure/
│   │   ├── Data/              # BankLiteDbContext, UnitOfWork, SeedData
│   │   ├── Migrations/        # EF Core migrations
│   │   └── Repositories/      # Repository implementations
│   ├── BankLiteAPI/
│   │   └── BankLite.API/
│   │       ├── Controllers/       # AuthController, AccountController, TransactionController
│   │       ├── Middleware/        # ExceptionMiddleware
│   │       └── Program.cs         # App configuration
│   └── BankLite.Tests/
│       └── Services/          # AuthServiceTests, TransactionServiceTests
└── Frontend/
    └── Frontend.Tests/
```

---

## Author / Auteur

**Nicholas Xydis**  
GitHub: [NicholasXydis](https://github.com/NicholasXydis)
