# Build the ASP.NET Core server and Blazor client.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src
COPY . .

RUN dotnet restore SecureVotingApp/SecureVotingApp.csproj
RUN dotnet publish SecureVotingApp/SecureVotingApp.csproj \
    -c Release -o /publish --no-restore

# Run the published application.
FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app
COPY --from=build /publish .

RUN mkdir -p /app/data /app/Keys /app/appLogs

EXPOSE 8080
EXPOSE 8443

ENTRYPOINT ["dotnet", "SecureVotingApp.dll"]