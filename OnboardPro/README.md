# OnboardPro — Employee Onboarding

OnboardPro is an ASP.NET Core MVC onboarding application using .NET 8, Entity Framework Core 8, SQL Server, and ASP.NET Core Identity.

## Requirements

- .NET 8 SDK
- SQL Server or SQL Server LocalDB

## Configure a local SQL Server connection

The checked-in configuration uses Windows SQL Server LocalDB:

```text
Server=(localdb)\MSSQLLocalDB;Database=OnboardPro;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```

Override it locally with User Secrets when using another instance. Never put production passwords or credentials in `appsettings*.json` or source control:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=OnboardPro;Trusted_Connection=True;TrustServerCertificate=True"
```

For SQL authentication, keep the username and password in User Secrets or environment variables, for example:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=OnboardPro;User Id=<user>;Password=<password>;Encrypt=True;TrustServerCertificate=True"
```

## Create the first HR account

Accounts are provisioned by HR and use ASP.NET Core Identity's password hashing and lockout support. There is no public registration endpoint, which prevents someone from claiming an employee email address before its owner.

Configure a development bootstrap administrator using User Secrets (choose a strong, unique password):

```powershell
dotnet user-secrets set "Onboarding:BootstrapAdminEmail" "hr@example.com"
dotnet user-secrets set "Onboarding:BootstrapAdminPassword" "<strong-password>"
```

On startup, the app creates this account with the HR role if it does not already exist. HR can create employee accounts from **Accounts**; the account receives the access role on the employee record. Share any initial password through a secure channel, and have the employee change it after signing in.

The Development environment seeds sample roles, employees, one checklist, tasks, and assets. The sample email addresses end in `example.test`; they are not login accounts. Set `Onboarding:SeedDemoData` to `false` in local configuration to disable this sample data.

## Migrations and database

EF Core packages and the repository-local EF CLI are pinned to 8.0.22 to match the .NET 8 application:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update
```

The initial migration is already included under `Migrations`; do not add `InitialCreate` again. To add a later schema change, run `dotnet tool run dotnet-ef migrations add <MigrationName>`, then run `dotnet tool run dotnet-ef database update`.

The Development startup path applies checked-in migrations with `Database.MigrateAsync()`. Production databases should be updated explicitly during deployment with `dotnet tool run dotnet-ef database update` or a reviewed migration bundle; the application does not use `EnsureCreated()`.

## Run and test

From this directory:

```powershell
dotnet run
dotnet test .\OnboardPro.Tests\OnboardPro.Tests.csproj
```

## M1 functionality

- HR manages employees, access roles, role-based checklist templates, reusable task definitions, task assignments, document review, buddy assignments, and employee accounts.
- Checklist assignment creates employee-specific task snapshots. Changes to reusable definitions or templates do not rewrite previously generated tasks.
- New Joiners can see and update only their own task statuses and documents.
- HR can review documents; IT Admin manages inventory and allocation/return history.
- Buddies can see only currently assigned joiners and their task progress.
- Document files are stored under `App_Data/documents`, outside `wwwroot`; uploads accept PDF, PNG, and JPEG up to 10 MB.
- The dashboard aggregates task completion, pending document reviews, and available inventory according to the signed-in role.

The application is organized into MVC controllers and view models, service interfaces/implementations for workflow rules, a generic repository for simple entity access, and `OnboardProDbContext` for the EF Core model and SQL Server constraints.
