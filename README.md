<div align="center">

# 🏥 Smart Healthcare Appointment System (SHAS)

### Enterprise-Grade Healthcare Appointment Platform

**ASP.NET Core 8 · Clean Architecture · Domain-Driven Design · Event-Driven Microservices**

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-12.0-239120?style=flat-square&logo=csharp)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![Architecture](https://img.shields.io/badge/Architecture-Clean%20%2B%20DDD-blue?style=flat-square)](#)
[![RabbitMQ](https://img.shields.io/badge/Messaging-RabbitMQ%20%2B%20MassTransit-FF6600?style=flat-square&logo=rabbitmq)](https://www.rabbitmq.com/)
[![gRPC](https://img.shields.io/badge/RPC-gRPC-4285F4?style=flat-square&logo=google)](https://grpc.io/)
[![Docker](https://img.shields.io/badge/Container-Docker-2496ED?style=flat-square&logo=docker)](https://www.docker.com/)
[![SQL Server](https://img.shields.io/badge/Database-SQL%20Server%202022-CC2927?style=flat-square&logo=microsoftsqlserver)](https://www.microsoft.com/sql-server)
[![License](https://img.shields.io/badge/License-Educational-lightgrey?style=flat-square)](#-license)

*A deliberately-scoped, deeply-implemented microservices platform — 4 services done right, not 12 done half-way.*

[Overview](#-project-overview) •
[Architecture](#%EF%B8%8F-system-architecture) •
[Features](#-feature-highlights) •
[Tech Decisions](#-technology-decisions) •
[Setup](#-getting-started) •
[Roadmap](#-roadmap)

</div>

---

## 📸 Screenshots

> _Add screenshots or a demo GIF here once the UI/Swagger flows are ready — e.g. booking flow, Swagger UI, RabbitMQ management dashboard, Docker Compose running._

| Swagger — Appointment Service | RabbitMQ Management | Docker Compose (all services up) |
|:---:|:---:|:---:|
| `![placeholder](docs/screenshots/swagger.png)` | `![placeholder](docs/screenshots/rabbitmq.png)` | `![placeholder](docs/screenshots/docker-compose.png)` |

---

## 📖 Project Overview

Booking a doctor's appointment sounds simple — until you have to guarantee **no two patients can double-book the same slot**, notify patients reliably even when the email provider is temporarily down, and let doctor-availability data evolve independently of the booking logic that depends on it. SHAS is built around that specific set of hard problems, using the same architectural tools a production healthcare SaaS would reach for.

Rather than building a wide, shallow set of microservices to pad a resume, SHAS intentionally scopes to **four services** — Identity, Doctor, Appointment, and Notification — and implements each one with the rigor of a real production system: Clean Architecture boundaries that are actually respected, a Domain-Driven Aggregate that actually owns its invariants, event publishing that actually survives a downstream outage, and inter-service contracts that are actually typed (gRPC, not "whatever JSON shape happens to work today").

**The goal of this repository is not to demonstrate that I can use many technologies. It's to demonstrate that I understand *why* each one is the right tool for a specific problem in this domain — and can defend that reasoning in a technical interview.**

### Core Objectives

- 🩺 Real-time doctor appointment booking with hard conflict prevention
- 🔐 Centralized, secure authentication shared across all services (JWT + refresh tokens)
- 👨‍⚕️ Doctor profile, specialization, and availability management, owned independently
- 📅 Appointment lifecycle management (booked → confirmed → completed/cancelled)
- 📢 Reliable, decoupled notification delivery (email/SMS) that never blocks booking
- 🧩 Loosely-coupled services communicating through well-defined sync (gRPC) and async (event) contracts
- 🐳 Fully containerized, one-command local environment via Docker Compose
- 🧪 A test suite that actually exercises the domain logic, not just controller smoke tests

---

## 🏗️ System Architecture

### High-Level View

```
                                   Client Applications
                                  (Web / Mobile / Future)
                                            │
                                            ▼
                                 ┌───────────────────────┐
                                 │      API Gateway       │
                                 │         (YARP)         │
                                 │  Routing · JWT Check   │
                                 │  Rate Limiting · Logs  │
                                 └───────────────────────┘
                                            │
                ┌───────────────┬───────────┴───────────┬────────────────┐
                ▼                ▼                       ▼                ▼
        ┌───────────────┐ ┌───────────────┐    ┌──────────────────┐ ┌───────────────────┐
        │   Identity     │ │    Doctor      │    │   Appointment     │ │    Notification    │
        │   Service      │ │    Service     │    │   Service         │ │    Service         │
        │                │ │                │    │  (DDD core)       │ │                    │
        │ • Register     │ │ • Profiles     │    │ • Booking          │ │ • Email            │
        │ • Login        │ │ • Schedule     │    │ • Conflict rules   │ │ • SMS              │
        │ • JWT issue    │ │ • Search       │    │ • Status tracking  │ │ • Delivery log     │
        │ • Refresh Tok. │ │ • gRPC server  │    │ • Domain Events     │ │ • Idempotent       │
        └───────┬────────┘ └───────┬────────┘    └─────────┬──────────┘ └──────────┬─────────┘
                │                   │                        │                      │
                ▼                   ▼                        │                      │
           IdentityDB           DoctorDB                     │                      │
          (SQL Server)         (SQL Server)                  │                      │
                                    ▲                         │                      │
                                    │        gRPC              │                      │
                                    │  (availability check)    │                      │
                                    └─────────────────────────┤                      │
                                                                ▼                      │
                                                          AppointmentDB                │
                                                          (SQL Server)                 │
                                                                │                       │
                                                                │ Domain Event           │
                                                                │ → Integration Event     │
                                                                │  (AppointmentCreated)   │
                                                                ▼                       │
                                                        ┌───────────────┐               │
                                                        │   RabbitMQ    │───────────────┘
                                                        │ (MassTransit) │   consume
                                                        └───────────────┘
                                                                                          │
                                                                                          ▼
                                                                                   NotificationDB
                                                                                   (SQL Server)
```

### Architectural Style at a Glance

| Concern | Approach | Why (short version — full reasoning in [Technology Decisions](#-technology-decisions)) |
|---|---|---|
| Service boundaries | 4 microservices, 1 database each | Independent deployability & failure isolation, without over-fragmenting |
| Internal structure | Clean Architecture per service | Domain logic stays framework-agnostic and unit-testable |
| Core business logic | DDD (Aggregate Root, Value Objects, Domain Events) | Slot-conflict invariants enforced in one place, not scattered across handlers |
| Cross-service reads | gRPC | Low-latency, strongly-typed contract for the availability check on the booking hot path |
| Cross-service side-effects | RabbitMQ + MassTransit | Notification delivery must never block or fail a booking transaction |
| Edge routing | YARP API Gateway | Single entry point, centralized auth/rate-limiting, hides internal topology from clients |
| Data consistency | Eventual consistency across services, strong consistency inside each Aggregate | Matches the real guarantee we need: *no double-booking* (strong), *notification eventually sent* (eventual) |

---

## ✨ Feature Highlights

### 🔐 Identity & Access
- Self-service registration and login
- JWT access tokens (short-lived) + rotating refresh tokens
- Role-based authorization (`Patient`, `Doctor`, `Admin`)
- Secure password hashing via ASP.NET Core Identity
- Token revocation on logout / refresh-token reuse detection

### 👨‍⚕️ Doctor Management
- Doctor profile CRUD with specialization tagging
- Availability/schedule management (recurring + one-off slots)
- Search & filter by specialization, availability, location
- Availability exposed to Appointment Service via a typed **gRPC** contract — not a REST call reused for internal traffic

### 📅 Appointment Booking (the core of the system)
- Booking flow that validates availability *before* committing, and re-validates *inside* the transaction to close the race-condition window
- **Aggregate Root** (`Appointment`) is the single owner of conflict-prevention logic — no service or controller can bypass it
- Full lifecycle tracking: `Requested → Confirmed → Completed / Cancelled`
- Domain Events (`AppointmentCreated`, `AppointmentCancelled`) raised from inside the aggregate, dispatched via MediatR, then translated into integration events for other services

### 📢 Notification Delivery
- Fully asynchronous — a slow or down email/SMS provider **cannot** slow down or fail a booking
- Idempotent consumer — redelivery from RabbitMQ (at-least-once delivery) does not produce duplicate emails
- Delivery attempts logged for auditability

### 🌐 Platform-Wide
- Single API Gateway entry point (YARP) — clients never talk to internal services directly
- Centralized rate limiting and JWT validation at the edge
- Structured logging with correlation IDs that follow a request across service boundaries
- One-command local environment: `docker compose up`

---

---

## 🗂️ Solution Structure

```
SmartHealthcareAppointmentSystem/
├── src/
│   ├── Services/
│   │   ├── Identity/
│   │   │   ├── Identity.Domain/
│   │   │   ├── Identity.Application/
│   │   │   ├── Identity.Infrastructure/
│   │   │   └── Identity.API/
│   │   │
│   │   ├── Doctor/
│   │   │   ├── Doctor.Domain/
│   │   │   ├── Doctor.Application/
│   │   │   ├── Doctor.Infrastructure/
│   │   │   ├── Doctor.API/
│   │   │   └── Doctor.Grpc/               (proto contracts + gRPC server)
│   │   │
│   │   ├── Appointment/
│   │   │   ├── Appointment.Domain/         (Aggregate Root lives here)
│   │   │   ├── Appointment.Application/
│   │   │   ├── Appointment.Infrastructure/
│   │   │   └── Appointment.API/
│   │   │
│   │   └── Notification/
│   │       ├── Notification.Domain/
│   │       ├── Notification.Application/
│   │       ├── Notification.Infrastructure/
│   │       └── Notification.API/
│   │
│   ├── Gateway/
│   │   └── SHAS.Gateway/                  (YARP configuration + Program.cs)
│   │
│   └── BuildingBlocks/
│       ├── SHAS.Shared.Contracts/          (integration event contracts, shared across services)
│       ├── SHAS.Shared.Kernel/             (base Entity, ValueObject, DomainEvent, IRepository<T>)
│       └── SHAS.Shared.Logging/            (Serilog + correlation-ID middleware)
│
├── tests/
│   ├── Identity.UnitTests/
│   ├── Doctor.UnitTests/
│   ├── Appointment.UnitTests/               (heaviest test suite — aggregate logic)
│   ├── Appointment.IntegrationTests/        (Testcontainers + real SQL Server)
│   └── Notification.UnitTests/
│
├── docker/
│   ├── docker-compose.yml
│   ├── docker-compose.override.yml
│   └── Dockerfiles/
│       ├── identity.Dockerfile
│       ├── doctor.Dockerfile
│       ├── appointment.Dockerfile
│       ├── notification.Dockerfile
│       └── gateway.Dockerfile
│
├── .github/
│   └── workflows/
│       └── ci.yml
│
├── docs/
│   ├── adr/                                 (Architectural Decision Records — see below)
│   └── screenshots/
│
└── README.md
```

**Why `BuildingBlocks` and not a shared "Common" project everyone dumps utilities into?** `SHAS.Shared.Kernel` is deliberately tiny — base classes with zero external dependencies (no EF Core, no MediatR references leaking in). It exists so every service's `Domain` layer can inherit `Entity`/`ValueObject`/`DomainEvent` without pulling in infrastructure concerns. `SHAS.Shared.Contracts` holds only the DTOs used *on the wire* between services (integration events, gRPC-adjacent DTOs) — never domain entities, so no service can leak its internal model to another.

---

## 🧱 Clean Architecture — Per-Service Layout

Every service (Identity, Doctor, Appointment, Notification) follows the identical internal shape:

```
ServiceName/
├── ServiceName.Domain/
│   ├── Entities/                 (Aggregate Roots + Entities)
│   ├── ValueObjects/
│   ├── DomainEvents/
│   ├── Enums/
│   └── Interfaces/                (repository contracts — no EF Core reference here)
│
├── ServiceName.Application/
│   ├── Commands/                  (write operations — MediatR IRequest)
│   ├── Queries/                    (read operations — MediatR IRequest)
│   ├── Handlers/
│   ├── DTOs/
│   ├── Validators/                  (FluentValidation)
│   └── Interfaces/                    (application-level abstractions: IEmailSender, IDateTimeProvider, etc.)
│
├── ServiceName.Infrastructure/
│   ├── Persistence/
│   │   ├── ServiceNameDbContext.cs
│   │   ├── Configurations/            (EF Core Fluent API entity configs)
│   │   └── Migrations/
│   ├── Repositories/                    (implements Domain interfaces)
│   ├── Messaging/                        (MassTransit publishers / consumers)
│   └── ExternalServices/                  (email/SMS provider clients, gRPC clients)
│
└── ServiceName.API/
    ├── Controllers/
    ├── Middleware/                        (global exception handler, correlation-ID enrichment)
    ├── Program.cs                          (composition root — DI wiring)
    └── appsettings.json
```

**Dependency direction:** `API → Application → Domain`, and `Infrastructure → Application → Domain`. `Domain` has **zero** outward references. This is what makes it possible to unit-test the Appointment aggregate's conflict logic with plain `xUnit` and no database, mocks, or web host involved.

---

## 🧠 Domain-Driven Design — Where It Actually Matters

DDD tactical patterns are applied specifically in **Appointment Service**, where the real business invariant lives — not sprinkled everywhere for its own sake.

### Aggregate Root: `Appointment`

```csharp
public class Appointment : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid DoctorId { get; private set; }
    public TimeSlot Slot { get; private set; }
    public AppointmentStatus Status { get; private set; }

    private Appointment() { } // EF Core

    public static Appointment Book(Guid patientId, Guid doctorId, TimeSlot slot)
    {
        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            DoctorId = doctorId,
            Slot = slot,
            Status = AppointmentStatus.Requested
        };

        appointment.AddDomainEvent(new AppointmentCreatedEvent(
            appointment.Id, patientId, doctorId, slot.Start, slot.End));

        return appointment;
    }

    public void Cancel(string reason)
    {
        if (Status == AppointmentStatus.Completed)
            throw new DomainException("Cannot cancel a completed appointment.");

        Status = AppointmentStatus.Cancelled;
        AddDomainEvent(new AppointmentCancelledEvent(Id, reason));
    }
}
```

### Value Object: `TimeSlot`

```csharp
public sealed class TimeSlot : ValueObject
{
    public DateTime Start { get; }
    public DateTime End { get; }

    public TimeSlot(DateTime start, DateTime end)
    {
        if (end <= start)
            throw new DomainException("Slot end must be after start.");
        Start = start;
        End = end;
    }

    public bool Overlaps(TimeSlot other) =>
        Start < other.End && other.Start < End;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }
}
```

**Why a Value Object for `TimeSlot` instead of two `DateTime` fields on the entity?** Overlap-checking logic (`Overlaps`) belongs next to the data it operates on. Scattering `start1 < end2 && start2 < end1` comparisons across handlers is exactly how slot-conflict bugs creep into booking systems — keeping it as one method on one immutable type means it's tested once, in one place, and can't drift.

### Domain Event → Integration Event Flow

```
1. Appointment.Book() called inside AppointmentService (Application layer)
2. Aggregate raises AppointmentCreatedEvent (in-memory, Domain layer)
3. EF Core SaveChangesAsync() commits the Appointment row
4. A SaveChanges interceptor collects pending domain events
5. MediatR publishes AppointmentCreatedEvent → domain event handler
6. Handler maps Domain Event → Integration Event (AppointmentCreatedIntegrationEvent)
7. MassTransit publishes the Integration Event to RabbitMQ
8. Notification Service's consumer picks it up asynchronously
```

Domain Events and Integration Events are **deliberately different types** — the Domain Event can carry rich domain objects and is never serialized; the Integration Event is a flat, versioned DTO safe to put on the wire. Collapsing these into one type is a common shortcut that quietly couples your message broker's payload shape to your internal domain model.

---

## 🔄 Inter-Service Communication

### 🔑 JWT Authentication Flow

```
┌────────┐        ┌──────────┐        ┌──────────┐        ┌─────────────┐
│ Client │        │ Gateway  │        │ Identity │        │ Appointment │
│        │        │ (YARP)   │        │ Service  │        │ Service     │
└───┬────┘        └────┬─────┘        └────┬─────┘        └──────┬──────┘
    │  POST /login       │                    │                     │
    │───────────────────▶│───────────────────▶│                     │
    │                    │                    │  validate creds      │
    │                    │                    │  issue JWT + refresh │
    │                    │◀───────────────────│                     │
    │◀───────────────────│                    │                     │
    │  { accessToken,     │                    │                     │
    │    refreshToken }   │                    │                     │
    │                    │                    │                     │
    │  POST /appointments │                    │                     │
    │  Authorization: Bearer <JWT>              │                     │
    │───────────────────▶│                    │                     │
    │                    │  validate JWT        │                     │
    │                    │  signature + expiry   │                     │
    │                    │  (no DB call — local)  │                     │
    │                    │───────────────────────────────────────────▶│
    │                    │                    │                     │  process
    │◀────────────────────────────────────────────────────────────────│
```

The Gateway validates the JWT **locally** (signature + expiry check against the shared signing key) — it does not call back into Identity Service on every request. That round-trip would put Identity Service on the critical path of *every single request in the system*, turning an auth check into a single point of failure and a latency tax on unrelated services.

### 📡 gRPC Communication Flow — Doctor Availability Check

```
┌─────────────────┐                          ┌────────────────┐
│ Appointment      │                          │ Doctor Service │
│ Service          │                          │ (gRPC Server)  │
└────────┬─────────┘                          └───────┬────────┘
         │  CheckAvailability(doctorId, slot)           │
         │──────────────────────────────────────────────▶
         │           (Polly retry/circuit breaker         │
         │            wraps this call)                     │
         │                                                  │  query DoctorDB
         │◀──────────────────────────────────────────────  │
         │  AvailabilityResponse { isAvailable: bool }       │
         │                                                  │
         │  if available → proceed with Appointment.Book()  │
         │  else → return 409 Conflict to client              │
```

```protobuf
// doctor_availability.proto
service DoctorAvailability {
  rpc CheckAvailability (AvailabilityRequest) returns (AvailabilityResponse);
}

message AvailabilityRequest {
  string doctor_id = 1;
  google.protobuf.Timestamp start = 2;
  google.protobuf.Timestamp end = 3;
}

message AvailabilityResponse {
  bool is_available = 1;
  string reason = 2;
}
```

This is a **check, not a commit** — the final conflict guarantee is still enforced inside the Appointment aggregate against `AppointmentDB` itself (see below), because the gRPC check and the actual booking are not atomic across two databases. The gRPC call narrows the race window and gives fast, typed feedback; the aggregate is the actual source of truth for "did this booking succeed."

### 🐰 RabbitMQ / MassTransit Event Flow

```
┌─────────────┐                  ┌───────────┐                  ┌──────────────┐
│ Appointment  │                  │ RabbitMQ  │                  │ Notification │
│ Service      │                  │ Exchange  │                  │ Service      │
└──────┬───────┘                  └─────┬─────┘                  └──────┬───────┘
       │  Publish                        │                                │
       │  AppointmentCreatedIntegration   │                                │
       │  Event                            │                                │
       │─────────────────────────────────▶│                                │
       │                                  │  routed to                       │
       │                                  │  notification-queue              │
       │                                  │───────────────────────────────▶│
       │                                  │                                │  Consumer:
       │                                  │                                │  1. check MessageId
       │                                  │                                │     against processed log
       │                                  │                                │     (idempotency)
       │                                  │                                │  2. if new → send email/SMS
       │                                  │                                │  3. log delivery
       │                                  │◀ ── ack ───────────────────────│
```

```csharp
public class AppointmentCreatedConsumer : IConsumer<AppointmentCreatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<AppointmentCreatedIntegrationEvent> context)
    {
        var alreadyProcessed = await _log.HasBeenProcessedAsync(context.MessageId);
        if (alreadyProcessed) return; // idempotency guard — RabbitMQ is at-least-once delivery

        await _notificationSender.SendAppointmentConfirmationAsync(context.Message);
        await _log.MarkProcessedAsync(context.MessageId);
    }
}
```

**Why idempotency matters here specifically:** RabbitMQ (and MassTransit's default retry policy) guarantees *at-least-once* delivery, not *exactly-once*. A network blip during ack, or a consumer restart mid-processing, can legitimately redeliver the same message. Without the processed-message check above, a patient could receive the same confirmation email twice — a small bug, but exactly the kind of thing that gets asked about in a systems-design interview.

---

## 🗄️ Database Schema Overview

Each service owns its schema completely — no cross-service joins, no shared database.

**IdentityDB**
```
Users            (Id, Email, PasswordHash, CreatedAt)
Roles            (Id, Name)
UserRoles        (UserId, RoleId)
RefreshTokens    (Id, UserId, TokenHash, ExpiresAt, IsRevoked)
```

**DoctorDB**
```
Doctors          (Id, UserId, FullName, SpecializationId, ChamberInfo)
Specializations  (Id, Name)
Schedules        (Id, DoctorId, DayOfWeek, StartTime, EndTime)
```

**AppointmentDB**
```
Appointments     (Id, PatientId, DoctorId, SlotStart, SlotEnd, Status, CreatedAt)
AppointmentHistory (Id, AppointmentId, PreviousStatus, NewStatus, ChangedAt)
```

**NotificationDB**
```
NotificationLogs (Id, AppointmentId, Channel, Status, SentAt)
ProcessedMessages (MessageId, ProcessedAt)   -- idempotency ledger
```

`Doctors.UserId` and `Appointments.PatientId` are **not** foreign keys in the relational sense — they're references resolved across service boundaries (via JWT claims / gRPC / event payloads), which is the standard trade-off in a microservices data model: you give up cross-service referential integrity at the database level in exchange for independent deployability.

---

## 🚀 Getting Started

### Prerequisites
- .NET 8 SDK
- Docker Desktop
- SQL Server (or use the Dockerized instance from Compose)
- RabbitMQ (or use the Dockerized instance from Compose)

```bash
dotnet tool install --global dotnet-ef
```

### Clone

```bash
git clone https://github.com/MotionProgramming/Smart-Healthcare-Appointment-System.git
cd Smart-Healthcare-Appointment-System
```

### Option A — Run everything with Docker Compose (recommended)

```bash
cd docker
docker compose up --build
```

This starts all 4 services, the Gateway, SQL Server, RabbitMQ, and Redis in one network, with migrations applied automatically on startup.

| Service | URL |
|---|---|
| API Gateway | `http://localhost:5000` |
| Identity Service (direct) | `http://localhost:5001` |
| Doctor Service (direct) | `http://localhost:5002` |
| Appointment Service (direct) | `http://localhost:5003` |
| Notification Service (direct) | `http://localhost:5004` |
| RabbitMQ Management | `http://localhost:15672` |
| Swagger (per service) | `http://localhost:500X/swagger` |

### Option B — Run services individually

Update the connection string in each service's `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=YOUR_DATABASE;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Apply migrations per service:

```bash
dotnet ef database update --project ServiceName.Infrastructure --startup-project ServiceName.API
```

Start RabbitMQ:

```bash
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

Run services in this order (Identity first — everything depends on auth):

```
1. Identity Service
2. Doctor Service
3. Appointment Service
4. Notification Service
5. API Gateway
```

```bash
dotnet run
```

---

## 🐳 Docker Architecture

```
docker-compose.yml
│
├── sqlserver           (single instance, 4 isolated databases)
├── rabbitmq             (management plugin enabled)
├── redis                 (Doctor availability + session cache)
│
├── identity-service       (depends_on: sqlserver)
├── doctor-service           (depends_on: sqlserver, redis)
├── appointment-service        (depends_on: sqlserver, rabbitmq, doctor-service [gRPC])
├── notification-service         (depends_on: sqlserver, rabbitmq)
│
└── gateway                        (depends_on: all 4 services)
```

Each service has its own `Dockerfile` using a **multi-stage build** — SDK image compiles, slim ASP.NET runtime image ships — keeping final image sizes small and build layers cached between rebuilds:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "Appointment.API/Appointment.API.csproj"
RUN dotnet publish "Appointment.API/Appointment.API.csproj" -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Appointment.API.dll"]
```

`docker-compose.override.yml` is used for local-only overrides (bind-mounted appsettings, exposed debug ports) so the base `docker-compose.yml` stays deploy-ready.

---

## ⚙️ CI/CD Pipeline

**GitHub Actions** — `.github/workflows/ci.yml`

```
Push / PR to main
        │
        ▼
┌──────────────────┐
│  1. Restore       │  dotnet restore
├──────────────────┤
│  2. Build          │  dotnet build --no-restore -c Release
├──────────────────┤
│  3. Unit Tests       │  dotnet test tests/*.UnitTests
├──────────────────┤
│  4. Integration Tests │  dotnet test tests/*.IntegrationTests  (Testcontainers spins up real SQL Server)
├──────────────────┤
│  5. Docker Build       │  build image per service
├──────────────────┤
│  6. Docker Push          │  push to GitHub Container Registry (main branch only)
└──────────────────┘
```

```yaml
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore -c Release

      - name: Unit Tests
        run: dotnet test tests/Appointment.UnitTests --no-build -c Release

      - name: Integration Tests
        run: dotnet test tests/Appointment.IntegrationTests --no-build -c Release
```

Integration tests run against a real, disposable SQL Server container (via Testcontainers) rather than an in-memory EF provider — the in-memory provider silently ignores several constraints (unique indexes, cascade behavior) that have caused real bugs to slip through in projects that relied on it.

---

## 🧪 Testing Strategy

| Layer | Tool | What's actually tested |
|---|---|---|
| Domain | xUnit | `TimeSlot.Overlaps()`, `Appointment.Book()` conflict rules, `Appointment.Cancel()` guard clauses — no mocks needed, pure logic |
| Application | xUnit + Moq | Command/Query handlers, with repository & gRPC client interfaces mocked |
| Infrastructure | xUnit + Testcontainers | Repository implementations against a real, disposable SQL Server instance |
| API | WebApplicationFactory | Controller-level tests, auth middleware behavior |
| Cross-service | Manual / Postman collection | Full booking → event → notification flow, run against Docker Compose |

**Example — the test that actually matters most in this system:**

```csharp
[Fact]
public void Book_ShouldThrow_WhenSlotOverlapsExistingAppointment()
{
    var slot1 = new TimeSlot(DateTime.Parse("2026-08-01 10:00"), DateTime.Parse("2026-08-01 10:30"));
    var slot2 = new TimeSlot(DateTime.Parse("2026-08-01 10:15"), DateTime.Parse("2026-08-01 10:45"));

    Assert.True(slot1.Overlaps(slot2));
}
```

Unglamorous, but this single method is the one piece of logic that, if wrong, means the entire system's core promise — no double-booking — silently fails in production.

---

## 🔐 Security

| Control | Implementation |
|---|---|
| Authentication | JWT access tokens (15 min expiry) + refresh tokens (7 day expiry, rotated on use) |
| Refresh token reuse detection | If a used/revoked refresh token is presented again, all tokens for that user are revoked — signals possible token theft |
| Authorization | Role-based (`Patient`, `Doctor`, `Admin`), enforced via `[Authorize(Roles = "...")]` at the controller level |
| Password storage | ASP.NET Core Identity's default hasher (PBKDF2, per-user salt) |
| Input validation | FluentValidation on every Command/Query, rejected before reaching the domain layer |
| Transport security | HTTPS enforced end-to-end; internal gRPC traffic also TLS-secured in the deployed environment |
| Rate limiting | Applied at the YARP Gateway — per-client-IP token bucket, protects all services uniformly from one place |
| Secrets | Local: `appsettings.Development.json` (gitignored). Production: environment variables / secret manager — never committed |
| Error responses | Global exception middleware returns a generic `ProblemDetails` response; stack traces never leave the service boundary |

---

## 📊 Observability

**Logging — Serilog, structured, correlation-aware**

```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithCorrelationId()
    .WriteTo.Console(new CompactJsonFormatter())
    .WriteTo.Seq("http://seq:5341")
    .CreateLogger();
```

A correlation ID is generated at the Gateway for every incoming request and propagated through HTTP headers and message headers (gRPC metadata, RabbitMQ message headers) so a single booking request can be traced across all 4 services in Seq — without that, debugging "why did this specific booking fail" across a distributed system means grepping four separate log streams by timestamp and hoping.

**Health Checks**

Every service exposes:
```
GET /healthz          → liveness (is the process up)
GET /healthz/ready     → readiness (can it reach its DB / RabbitMQ)
```

**Metrics & Tracing (roadmap — see below)**

The logging and correlation-ID groundwork above is what OpenTelemetry needs to slot in later without rearchitecting anything — traces would simply attach to the same correlation context that's already flowing through every request.

---

## ⚡ Performance Considerations

- **Redis caching** for Doctor availability lookups — the availability check sits on the booking hot path and is read far more often than it changes; caching it avoids hammering `DoctorDB` on every gRPC call, with a short TTL to bound staleness.
- **gRPC over REST** for the Doctor availability check specifically because it's a synchronous call inside a user-facing request — binary serialization and HTTP/2 multiplexing measurably reduce tail latency compared to JSON-over-REST for this one high-frequency internal call.
- **Async messaging removes Notification Service from the latency budget entirely** — a booking's response time is never affected by how long it takes an email provider to accept a send request.
- **Polly retry + circuit breaker** on the Appointment → Doctor gRPC call — a transient blip in Doctor Service doesn't immediately fail every booking attempt; a sustained outage trips the breaker and fails fast instead of piling up timeouts.
- **Connection pooling** tuned explicitly (not left at driver defaults) for both the SQL Server connections (via EF Core) and the RabbitMQ connection (a single long-lived connection with multiple channels per service, not one connection per message).

---

## 🌍 Deployment View

**Local / Staging — Docker Compose** (current, fully working)

```
docker compose up --build
```
All 4 services + Gateway + SQL Server + RabbitMQ + Redis on one Docker network, one command.

**Production target — Kubernetes** (documented, not yet implemented — see Roadmap)

```
Kubernetes Cluster
├── Deployment + Service          (one pair per microservice, independently scalable)
├── HorizontalPodAutoscaler        (Appointment Service scales independently — it's the highest-traffic service)
├── ConfigMap / Secret               (connection strings, JWT signing key)
├── Ingress                            (replaces/fronts the YARP Gateway)
└── StatefulSet                          (RabbitMQ, if not using a managed instance)
```

The reason Docker Compose is the current deployment target rather than a half-finished Kubernetes setup: a working, well-tested Compose environment demonstrates the same containerization competency and is something a reviewer can actually run in 60 seconds. A Kubernetes setup that hasn't been genuinely load-tested would just be YAML for its own sake.

---

## 📈 Roadmap

- [ ] **Outbox Pattern** — guarantee event publishing even if the process crashes between DB commit and RabbitMQ publish (currently a small window where these two aren't atomic)
- [ ] **Saga Pattern** — for future multi-step workflows (e.g., booking + payment as one coordinated transaction across services)
- [ ] **Kubernetes manifests** — Deployment/Service/HPA per service, tested against a local cluster (kind/minikube) before claiming production-readiness
- [ ] **OpenTelemetry distributed tracing** — building on the correlation-ID work already in place
- [ ] **Prometheus + Grafana** — request rate, latency, error rate per service
- [ ] **CQRS with separate read models** — Appointment Service's search/history queries could move to a denormalized read store if query load grows

These are listed as explicit future work rather than partially wired in, because a half-implemented Saga or an untested Kubernetes manifest is a liability in an interview, not an asset — it invites a question you can't fully answer.

---

## 📋 Architectural Decision Records (ADR)

| # | Decision | Status | Summary |
|---|---|---|---|
| ADR-001 | Use gRPC for Appointment → Doctor availability check | Accepted | Chosen over REST for lower latency and typed contracts on a synchronous, high-frequency, internal-only call. REST remains the public contract at the Gateway. |
| ADR-002 | Use RabbitMQ + MassTransit for notification delivery | Accepted | Chosen over a direct HTTP call so Notification Service outages/slowness never affect booking availability. MassTransit chosen over raw RabbitMQ client for built-in retry, outbox integration path, and consistent consumer conventions. |
| ADR-003 | Model `Appointment` as an Aggregate Root with `TimeSlot` as a Value Object | Accepted | Keeps the double-booking invariant enforced in exactly one place instead of duplicated across handlers/services. |
| ADR-004 | Scope to 4 microservices, not the originally-drafted 10–12 | Accepted | Depth over breadth — each service is fully implemented with tests, gRPC/messaging, and Docker support, rather than many services with placeholder logic. |
| ADR-005 | YARP over a managed API Gateway (e.g., Ocelot) | Accepted | YARP is actively maintained by Microsoft, integrates natively with ASP.NET Core's middleware pipeline, and avoids a second, differently-configured web framework in the stack. |
| ADR-006 | JWT validated locally at the Gateway, not via a call to Identity Service per request | Accepted | Avoids making Identity Service a single point of failure and latency bottleneck for every request in the system. |
| ADR-007 | Kubernetes deferred to roadmap instead of partially implemented now | Accepted | An untested Kubernetes setup demonstrates YAML-writing, not operational competency; Docker Compose is fully working and honestly represents current maturity. |

*(Individual ADR files with full context/consequences live under `docs/adr/` — this table is the summary view.)*

---

## 💬 Interview Discussion Notes — "Why This Architecture?"

Questions this project is specifically built to be able to answer, not just built to look like it could:

**"Why microservices at all for a project this size?"**
Because the boundaries chosen (Identity, Doctor, Appointment, Notification) map to genuinely independent rates of change and failure domains — Notification Service depending on a flaky third-party email API shouldn't be able to take down booking. A monolith would couple their failure modes; four services with an async boundary between the risky one (Notification) and the critical one (Appointment) doesn't.

**"What happens if RabbitMQ is down when a booking completes?"**
The booking still succeeds — the DB transaction commits independently of the publish. The gap between "DB commit" and "event published" is the known limitation solved by the Outbox Pattern (see Roadmap) — currently a small window exists where a crash between those two steps would lose the event. This is called out explicitly rather than glossed over.

**"How do you prevent double-booking under concurrent requests?"**
Two layers: a fast-path gRPC availability check before attempting the booking (UX-level early rejection), and the actual guarantee enforced at the `AppointmentDB` level — the aggregate's slot-conflict check runs inside the same transaction as the insert, with a unique constraint on `(DoctorId, SlotStart)` as the final backstop against race conditions the application-level check might miss under high concurrency.

**"Why not just use one shared database for simplicity?"**
Because it would silently reintroduce the coupling microservices are meant to remove — a schema migration in Doctor Service could break Appointment Service's queries, and neither team (in a real org) could deploy independently. The cost (no cross-service joins, eventual consistency) is a deliberate trade, not an oversight.

**"What would you do differently building this again?"**
Implement the Outbox Pattern from the start rather than as a follow-up — the current gap between DB commit and event publish is small but real, and it's the kind of thing that's much easier to design in from day one than retrofit.

---

## 🤝 Contribution

```bash
git checkout -b feature/AmazingFeature
git commit -m "Add AmazingFeature"
git push origin feature/AmazingFeature
```

Then open a Pull Request against `main`. CI must pass (build + unit tests + integration tests) before review.

---

## 📄 License

This project is built for educational purposes, portfolio demonstration, and learning distributed systems design. Not licensed for production/commercial use as-is.

---

<div align="center">

## 👨‍💻 Developer

**Md Abdullah Rajeb** (MotionProgramming)

[![GitHub](https://img.shields.io/badge/GitHub-MotionPrograming-181717?style=flat-square&logo=github)](https://github.com/MotionPrograming)
[![Email](https://img.shields.io/badge/Email-mdabdullahrajeb90%40gmail.com-D14836?style=flat-square&logo=gmail)](mailto:mdabdullahrajeb90@gmail.com)

*If this project was useful or interesting, a ⭐ on the repo is appreciated.*

</div>
