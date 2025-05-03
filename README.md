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
- Auth0 secret must stored in a plain text file `m2m_secret.txt` at the root of the project (should contain ONLY the Auth0 M2M client secret value).

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

---

## Notes

- Do **not** commit any secret files (like `secret.properties` or `m2m_secret.txt`) to version control.
- NGROK can be used for secure HTTPS development tunnel.
