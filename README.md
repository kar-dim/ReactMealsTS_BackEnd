# ReactMealsTS_BackEnd

This repository contains the **backend** implementation (in both .NET and Java) for my custom version of [Maximilian Schwarzmüller’s React Meals](https://www.udemy.com/course/react-the-complete-guide-incl-redux/) project (as of 2023).

**Frontend repo**: [ReactMealsTS_FrontEnd](https://github.com/kar-dim/ReactMealsTS_FrontEnd)

---

## Features

- **NGROK** support for secure HTTPS tunneling during development.
- **EF Core** with SQL Server for database access and persistence (.NET version).
- **Auth0 Integration** for authentication and authorization:
  - User registration and login
  - Claims and policies
  - M2M (machine-to-machine) access tokens

---

## Technologies

### 🔹 .NET Core (default)

- Uses EF Core and SQL Server.
- Set `Auth0__M2M_ClientSecret` as an environment secret, or place the secret value alone in `m2m_secret.txt` at the app content root for local development.

### 🔸 Java Spring Boot (branch: `spring`)

- Uses Spring Boot with JPA (Hibernate) + HikariCP, by default connects to the same local MS SQL Server as the .NET implementation (uses different database).
- Auth0 secret and its various properties should be stored in a `secret.properties` file.

**Note: for both technologies, the above Auth0 files are excluded from version control for security reasons, they must be created manually**

#### Auth0 Configuration (`secret.properties`) for Spring implementation

Place this file at:

```
C:\Users\{YourUsername}\.auth0
```

And define the following properties:

```properties
auth0.domain=...
auth0.audience=...
auth0.m2maudience=...
auth0.m2m_clientid=...
auth0.m2m_clientsecret=...
```

---

## Getting Started

1. Clone this repository.
2. Checkout the appropriate branch (`master` for .NET or `spring` for Java).
3. Configure your Auth0 secrets as described above.
4. Start the backend server.
5. Launch the frontend described at [ReactMealsTS_FrontEnd](https://github.com/kar-dim/ReactMealsTS_FrontEnd).

Before starting an existing database with this version, apply the `PreserveOrderItems` EF Core migration. It copies current dish details into existing order items, changes the dish foreign key so a dish with orders cannot be deleted, and removes previously stored Management API bearer tokens. New Management API tokens are kept in memory. Details already lost through earlier dish deletions cannot be restored by the migration.

With the EF Core CLI installed, run `dotnet ef database update --project ReactMeals_WebApi/ReactMeals_WebApi.csproj` from the repository root.

The `CreateUser` endpoint accepts only tokens whose `azp` or `client_id` claim matches `Auth0:M2M_RegistrationClientID` (or `Auth0:M2M_ClientID` when that setting is absent). Configure this to the Auth0 Action client ID before deploying. Set `Images:Directory` to a writable, persistent directory for uploaded dish images; it defaults to `Images` under the app content root. Copy existing dish images into that directory when changing it.

---

## Notes

- Do **not** commit any secret files (like `secret.properties` or `m2m_secret.txt`) to version control.
- NGROK can be used for secure HTTPS development tunnel.
