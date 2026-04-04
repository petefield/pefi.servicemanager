# pefi.servicemanager

A RESTful microservice for managing the lifecycle of Docker containers. It provides a simple HTTP API to create, retrieve, update, restart, and delete containerised services, backed by MongoDB for persistence and RabbitMQ for event messaging.

---

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Technology Stack](#technology-stack)
- [Prerequisites](#prerequisites)
- [Configuration](#configuration)
- [Running Locally](#running-locally)
- [Running with Docker](#running-with-docker)
- [API Reference](#api-reference)
- [Events](#events)
- [CI/CD](#cicd)

---

## Overview

`pefi.servicemanager` acts as a management layer between API consumers and a remote Docker daemon. When a service is created or deleted, lifecycle events are published to a RabbitMQ message broker so that other components of the platform can react accordingly.

**Core capabilities:**

- Pull Docker images and create containers on a remote Docker host
- Start, stop, restart, and remove containers
- Persist service metadata in MongoDB
- Publish `service.created` and `service.deleted` events to RabbitMQ
- Expose a Swagger UI for interactive exploration of the API

---

## Architecture

```
┌──────────────┐     HTTP      ┌─────────────────────────┐
│   API Client │ ────────────► │   pefi.servicemanager   │
└──────────────┘               │  (ASP.NET Core Web API) │
                               └────────┬────────┬────────┘
                                        │        │
                              ┌─────────▼──┐  ┌──▼──────────┐
                              │  MongoDB   │  │  RabbitMQ   │
                              │ (services) │  │  (events)   │
                              └────────────┘  └─────────────┘
                                        │
                              ┌─────────▼──────────┐
                              │  Docker Daemon     │
                              │  (remote host)     │
                              └────────────────────┘
```

---

## Technology Stack

| Concern              | Technology                          |
|----------------------|-------------------------------------|
| Language / Runtime   | C# / .NET 9                         |
| Web Framework        | ASP.NET Core Minimal APIs           |
| Database             | MongoDB 3.4                         |
| Container Management | Docker.DotNet 3.125                 |
| Message Broker       | RabbitMQ (via `pefi.messaging.rabbit`) |
| Observability        | OpenTelemetry (via `pefi.observability`) |
| API Documentation    | Swagger / Swashbuckle 6.6           |
| Container Runtime    | Docker (linux/amd64 and linux/arm64)|

---

## Prerequisites

| Requirement    | Details                                                  |
|----------------|----------------------------------------------------------|
| .NET 9 SDK     | [Download](https://dotnet.microsoft.com/download)        |
| MongoDB        | A running instance accessible from the service           |
| RabbitMQ       | A running instance accessible from the service           |
| Docker daemon  | A Docker host with the remote API enabled (TCP port 2375)|
| NuGet source   | Access to the `petefield` GitHub Packages feed           |

> **NuGet authentication** – The private packages (`pefi.messaging.rabbit` and `pefi.observability`) are hosted on GitHub Packages. Add a NuGet source before restoring:
>
> ```bash
> dotnet nuget add source \
>   --username <github-username> \
>   --password <github-pat> \
>   --store-password-in-clear-text \
>   --name petefield \
>   "https://nuget.pkg.github.com/petefield/index.json"
> ```

---

## Configuration

All settings are supplied via environment variables (or `appsettings.json` for local development).

| Environment Variable              | Description                                       | Example                        |
|-----------------------------------|---------------------------------------------------|--------------------------------|
| `Persistance__ConnectionString`   | MongoDB connection string                         | `mongodb://localhost:27017`    |
| `Messaging__Username`             | RabbitMQ username                                 | `guest`                        |
| `Messaging__Password`             | RabbitMQ password                                 | `guest`                        |
| `Messaging__Address`              | RabbitMQ host address                             | `localhost`                    |

> **Note:** The `Persistance` prefix (note the spelling) matches the configuration section name used in the application code and must be written exactly as shown above.

> **Note:** The Docker daemon URL and the OpenTelemetry collector endpoint are currently hardcoded in `DockerManager.cs` and `Program.cs` respectively. Update these values for your environment before building.

---

## Running Locally

```bash
# 1. Restore dependencies (requires NuGet source configured above)
dotnet restore

# 2. Set required environment variables
export Persistance__ConnectionString="mongodb://localhost:27017"
export Messaging__Username="guest"
export Messaging__Password="guest"
export Messaging__Address="localhost"

# 3. Run the application
dotnet run

# The API is now available at http://localhost:5247
# Swagger UI: http://localhost:5247/swagger
```

---

## Running with Docker

The repository includes a multi-stage `Dockerfile`. The built image listens on port **9090** inside the container.

```bash
# Build the image (requires the GitHub token for private NuGet packages)
docker build \
  --secret id=github_token,env=GITHUB_TOKEN \
  -t pefi.servicemanager .

# Run the container
docker run -d \
  -p 9090:9090 \
  -e Persistance__ConnectionString="mongodb://192.168.1.87:27017" \
  -e Messaging__Username="guest" \
  -e Messaging__Password="guest" \
  -e Messaging__Address="192.168.1.87" \
  pefi.servicemanager
```

Alternatively, pull the latest image from GitHub Container Registry (published automatically by the CI pipeline):

```bash
docker pull ghcr.io/petefield/pefi.servicemanager:latest
```

---

## API Reference

All endpoints are also available via the **Swagger UI** at `/swagger`.

### GET `/services`

Returns all registered services.

**Response `200 OK`**
```json
[
  {
    "serviceName": "my-api",
    "hostName": "myhost",
    "containerPortNumber": "3000",
    "hostPortNumber": "3000",
    "dockerImageUrl": "ghcr.io/my-org/my-api:latest",
    "networkName": "bridge"
  }
]
```

---

### GET `/services/{serviceName}`

Returns a single service by name.

| Status | Meaning                      |
|--------|------------------------------|
| `200`  | Service found and returned   |
| `404`  | No service with that name    |

---

### POST `/services`

Creates a new service: pulls the Docker image, creates and starts the container, persists the record, and publishes a `service.created` event.

**Request body**
```json
{
  "serviceName": "my-api",
  "hostName": "myhost",
  "containerPortNumber": "3000",
  "hostPortNumber": "3000",
  "dockerImageUrl": "ghcr.io/my-org/my-api:latest",
  "networkName": "bridge",
  "environmentVariables": {
    "NODE_ENV": "production"
  }
}
```

| Field                  | Required | Description                                      |
|------------------------|----------|--------------------------------------------------|
| `serviceName`          | ✅        | Unique name; also used as the container name     |
| `hostName`             |          | Hostname to associate with the service           |
| `containerPortNumber`  |          | Port exposed inside the container                |
| `hostPortNumber`       |          | Port mapped on the host                          |
| `dockerImageUrl`       |          | Full image URL to pull and run                   |
| `networkName`          |          | Docker network to attach the container to        |
| `environmentVariables` |          | Key/value pairs passed as container env vars     |

| Status  | Meaning                                       |
|---------|-----------------------------------------------|
| `201`   | Service created; body contains new service    |
| `409`   | A container with that name already exists     |

---

### POST `/services/{serviceName}/update`

Pulls a fresh copy of the image, stops and removes the existing container, then recreates and starts it.

| Status  | Meaning                                        |
|---------|------------------------------------------------|
| `204`   | Service updated successfully                   |
| `404`   | Service not found                              |
| `422`   | Service record has no Docker image URL         |

---

### POST `/services/{serviceName}/restart`

Stops and then starts the existing container without recreating it.

| Status | Meaning               |
|--------|-----------------------|
| `204`  | Service restarted     |
| `404`  | Service not found     |

---

### DELETE `/services/{serviceName}`

Stops and removes the container, then deletes the service record from the database and publishes a `service.deleted` event.

| Status | Meaning               |
|--------|-----------------------|
| `204`  | Service deleted       |

---

## Events

The service publishes events to the **`Events`** RabbitMQ topic exchange.

| Routing Key               | Trigger                  | Payload fields               |
|---------------------------|--------------------------|------------------------------|
| `events.service.created`  | `POST /services`         | `ServiceName`                |
| `events.service.deleted`  | `DELETE /services/{name}`| `ServiceName`                |

---

## CI/CD

The GitHub Actions workflow (`.github/workflows/docker-image.yml`) runs on every push to `main` and on pull requests targeting `main`.

**Pipeline steps:**
1. Check out the repository
2. Set up Docker Buildx for multi-platform builds
3. Authenticate with GitHub Container Registry (GHCR)
4. Build and push a multi-platform image (`linux/amd64`, `linux/arm64`) to `ghcr.io/petefield/pefi.servicemanager`

The workflow uses `secrets.GITHUB_TOKEN` for both GHCR authentication and the private NuGet package restore.
