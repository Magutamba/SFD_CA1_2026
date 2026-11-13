#  Security Fundamentals and Development CA1 2026

## Running the Secure Online Voting Application with Docker

The Secure Online Voting application can be run locally using Docker. The Docker configuration builds the ASP.NET Core .NET 10 backend and Blazor frontend, and automatically initialises a fresh SQLite database.

### Prerequisites

Before starting, ensure the following software is installed:

- **Docker Desktop** — must be running.
- **.NET 10 SDK** — required to generate the local HTTPS development certificate.
- **Windows PowerShell** — used to run the startup script.

### Installation and Startup

**Step 1 — Download the project**

Clone the GitHub repository or download it as a ZIP file and extract it.

**Step 2 — Start Docker Desktop**

Open Docker Desktop and make sure the Docker engine is running.

**Step 3 — Open PowerShell**

Navigate to the project's outer `SecureVotingApp` folder, which contains `Dockerfile`, `compose.yaml` and `start-docker.ps1`.

**Step 4 — Start the application**

Run the following command:

```powershell
.\start-docker.ps1
```

The startup script will:

1. Create a local HTTPS development certificate.
2. Ask the user to choose a certificate password.
3. Request permission to trust the development certificate.
4. Remove any previous application container.
5. Build and start the Docker container.
6. Initialise the SQLite database using Entity Framework Core migrations.

The certificate password is chosen locally by the person running the application. No password needs to be shared.

**Step 5 — Open the application**

Once Docker has started successfully, open a browser and visit:

**https://localhost:8443**

### Testing the Application

After opening the application:

1. Register a new account using an email address and password.
2. Scan the displayed QR code using an authenticator application, such as Google Authenticator or Microsoft Authenticator.
3. Enter the generated six-digit TOTP code to confirm registration.
4. Log in using the registered credentials and a new authenticator code.
5. Explore the available voting functionality.

### Resetting the Application

The startup script removes the previous container and creates a fresh application environment.

This means that previous test accounts, voting records and temporary application keys are discarded whenever the script recreates the container.

After resetting, open the application in a **new private/incognito browser window** to avoid errors caused by old authentication or antiforgery cookies.

### Stopping the Application

In the PowerShell terminal where Docker is running, press:

`Ctrl + C`

Alternatively, stop the container using Docker Desktop.

### Important Notes

- The Docker setup is intended for **local demonstration and testing**, not production deployment.
- A fresh SQLite database is created when the previous container is removed.
- HTTPS is required for the application's secure authentication cookies.
- The lecturer does not need to install SQLite or configure the database manually.
- The startup script is designed for Windows PowerShell. macOS and Linux require different certificate setup instructions.
- An internet connection may be required on the first run to download Docker images and application dependencies.
