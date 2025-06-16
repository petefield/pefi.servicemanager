# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM  --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM  --platform=$BUILDPLATFORM  mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["pefi.servicemanager.csproj", "."]


RUN --mount=type=secret,id=github_token,env=GITHUB_TOKEN dotnet nuget add source --username petefield --password $GITHUB_TOKEN --store-password-in-clear-text --name petefield "https://nuget.pkg.github.com/petefield/index.json"

RUN dotnet restore  "./pefi.servicemanager.csproj"  -a $TARGETARCH
COPY . .
WORKDIR "/src/."
RUN dotnet build "./pefi.servicemanager.csproj"  -a $TARGETARCH -c Debug -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./pefi.servicemanager.csproj" -c Debug -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "pefi.servicemanager.dll", "--urls=http://*:9090"]