# Start the Secure Online Voting application using Docker.

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

# Check required software.
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw "Docker is not installed."
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw ".NET SDK is required to create the HTTPS certificate."
}

# Create the certificate folder.
New-Item -ItemType Directory -Force ".docker/https" | Out-Null

# Ask for a local certificate password.
$securePassword = Read-Host "Choose an HTTPS certificate password" -AsSecureString
$env:DEV_CERT_PASSWORD = [System.Net.NetworkCredential]::new(
    "",
    $securePassword
).Password

$certPath = ".docker/https/devcert.pfx"

# Export a development certificate using the chosen password.
if (Test-Path $certPath) {
    Remove-Item $certPath
}

dotnet dev-certs https -ep $certPath -p $env:DEV_CERT_PASSWORD

if ($LASTEXITCODE -ne 0) {
    throw "HTTPS certificate export failed."
}

# Trust the development certificate.
dotnet dev-certs https --trust

if ($LASTEXITCODE -ne 0) {
    throw "HTTPS certificate trust failed."
}

# Remove the previous container for a fresh demonstration.
docker compose down

if ($LASTEXITCODE -ne 0) {
    throw "Could not remove the previous Docker container."
}

Write-Host "Starting Secure Online Voting..." -ForegroundColor Green

docker compose up --build --force-recreate

# Clear the password from this PowerShell session.
Remove-Item Env:DEV_CERT_PASSWORD -ErrorAction SilentlyContinue