# Secure Online Voting Application

## Running the Application

The Secure Online Voting application is developed using **ASP.NET Core .NET 10, Blazor WebAssembly, Entity Framework Core and SQLite**.

There are two ways to run the application:

1. **Docker (Recommended)** — Builds and runs the application in a container with a fresh SQLite database.
2. **Conventional .NET Setup** — Runs the application directly using the .NET 10 SDK without Docker.

Both methods support registration, two-factor authentication (TOTP), login and the application's voting functionality.

---

# Option 1 — Running with Docker (Recommended)

Docker provides a convenient way to run the application without manually configuring the database or installing the application's NuGet dependencies.

## Prerequisites

Before starting, ensure the following software is installed:

- **Docker Desktop** — must be running.
- **.NET 10 SDK** — required to generate the local HTTPS development certificate.
- **Windows PowerShell** — used to execute the startup script.
- **An authenticator application** — Google Authenticator or Microsoft Authenticator.

## Installation and Startup

### Step 1 — Download the project

Clone the GitHub repository or download it as a ZIP file and extract it.

### Step 2 — Start Docker Desktop

Open Docker Desktop and ensure that the Docker engine is running.

### Step 3 — Open PowerShell

Navigate to the outer `SecureVotingApp` folder containing:

- `Dockerfile`
- `compose.yaml`
- `start-docker.ps1`

### Step 4 — Start the application

Run:

```powershell
.\start-docker.ps1
```

The startup script will:

1. Create a local HTTPS development certificate.
2. Ask the user to choose a certificate password.
3. Request permission to trust the certificate.
4. Remove any previous application container.
5. Build and start the Docker container.
6. Initialise the SQLite database using Entity Framework Core migrations.

The certificate password is chosen locally by the person running the application. No password needs to be shared.

### Step 5 — Open the application

Once Docker has started successfully, open:

**https://localhost:8443**

## Testing the Application

After opening the application:

1. Register a new account using an email address and password.
2. Scan the displayed QR code using an authenticator app.
3. Enter the six-digit TOTP code to confirm registration.
4. Log in using the registered credentials and a new authenticator code.
5. Explore the available voting functionality.

## Resetting the Application

The startup script removes the previous container and creates a fresh application environment.

When the container is recreated, the previous test accounts, voting records, RSA signing keys and temporary encryption keys are discarded.

After resetting, open the application in a **new private/incognito browser window** to avoid errors caused by old authentication or antiforgery cookies.

## Stopping Docker

Press `Ctrl + C` in the terminal running the application.

Alternatively, stop the container through Docker Desktop.

---

# Option 2 — Running Without Docker (Conventional .NET Setup)

The application can also be run directly using the .NET SDK without Docker.

## Prerequisites

Ensure the following software is installed:

- **.NET 10 SDK**
- **Visual Studio Code**, Visual Studio or JetBrains Rider
- **Git**, if cloning the repository
- **Google Authenticator or Microsoft Authenticator**

## Installation and Startup

### Step 1 — Download the project

Clone the GitHub repository or download the project ZIP file and extract it.

### Step 2 — Open the project

Open the outer `SecureVotingApp` folder in your preferred IDE.

The project contains:

- `SecureVotingApp` — ASP.NET Core backend.
- `SecureVotingApp.Client` — Blazor frontend.
- `SecureVotingApp.Tests` — xUnit automated tests.

### Step 3 — Restore dependencies

Open a terminal in the outer `SecureVotingApp` folder and run:

```powershell
dotnet restore .\SecureVotingApp\SecureVotingApp.csproj
```

This downloads the required NuGet packages.

### Step 4 — Trust the HTTPS development certificate

Run:

```powershell
dotnet dev-certs https --trust
```

Approve the certificate trust prompt if one appears.

### Step 5 — Initialise the SQLite database

The application is configured to automatically apply existing Entity Framework Core migrations during startup using `Database.Migrate()`.

Therefore, manual database creation is normally unnecessary.

If automatic migration is unavailable, install the EF Core command-line tool:

```powershell
dotnet tool install --global dotnet-ef --version 10.*
```

Then apply the migrations:

```powershell
dotnet ef database update --project .\SecureVotingApp\SecureVotingApp.csproj --startup-project .\SecureVotingApp\SecureVotingApp.csproj
```

If `dotnet-ef` is already installed, the installation command can be skipped.

### Step 6 — Run the application

Start the backend and hosted Blazor frontend:

```powershell
dotnet run --project .\SecureVotingApp\SecureVotingApp.csproj --launch-profile https
```

Wait until the terminal displays the application URL.

### Step 7 — Open the application

Open the HTTPS address displayed in the terminal.

The exact port is configured in `SecureVotingApp/Properties/launchSettings.json`.

## Testing the Application

Once running:

1. Register a new account.
2. Scan the QR code using an authenticator app.
3. Confirm registration with the six-digit TOTP code.


