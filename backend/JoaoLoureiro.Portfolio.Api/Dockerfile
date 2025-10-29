# Stage 1: Build the application
# Use the .NET SDK image which contains all the tools to build and publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy all .csproj files and the .sln file first. This is a key optimization.
# Docker will cache this layer, and will only re-run `dotnet restore` if a project file changes.
COPY ["JoaoLoureiro.Portfolio.Core/JoaoLoureiro.Portfolio.Core.csproj", "JoaoLoureiro.Portfolio.Core/"]
COPY ["JoaoLoureiro.Portfolio.Infrastructure/JoaoLoureiro.Portfolio.Infrastructure.csproj", "JoaoLoureiro.Portfolio.Infrastructure/"]
COPY ["JoaoLoureiro.Portfolio.Api/JoaoLoureiro.Portfolio.Api.csproj", "JoaoLoureiro.Portfolio.Api/"]
COPY ["JoaoLoureiro.Portfolio.sln", "."]

# Restore all NuGet packages for the entire solution
RUN dotnet restore "JoaoLoureiro.Portfolio.sln"

# Copy the rest of the source code
COPY . .

# Publish the API project, creating the release-ready artifacts
WORKDIR "JoaoLoureiro.Portfolio.Api"
RUN dotnet publish "JoaoLoureiro.Portfolio.Api.csproj" -c Release -o /app/publish --no-restore

# Stage 2: Create the final, minimal runtime image
# Use the lightweight ASP.NET runtime image, which is much smaller than the SDK
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# The new .NET 8 images run as a non-root 'app' user by default for better security.
# We expose port 8080, as the default internal port 80 is often a privileged port.
EXPOSE 8080

# Copy the published output from the build stage
COPY --from=build /app/publish .

# Set the entrypoint for the container
ENTRYPOINT ["dotnet", "JoaoLoureiro.Portfolio.Api.dll"]