# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository status

.NET 10 take-home defined by [handson.md](handson.md). All four service projects, `Shared.Contracts`, and `Order.Orchestrator.Tests` are scaffolded. Treat `handson.md` as the source of truth for endpoints, domain models, and acceptance criteria. The diagrams in [diagarms.html](diagarms.html) mirror those in the spec.

## Target architecture

**Stack:** .NET 10, ASP.NET Core Minimal API, EF Core (SQLite), MassTransit + RabbitMQ, MediatR, FluentValidation, `Microsoft.Extensions.Http.Resilience` (Polly v8).

**Microservices** — four services, each owns its own SQLite DB. Sync over HTTP, async over RabbitMQ. `Shared.Contracts` is schema-only.

| Project | Notes | Port |
|---|---|---|
| `Order.Orchestrator.Api` | Single project, clean-architecture folders inside (`Domain/`, `Application/`, `Infrastructure/`, `Endpoints/`, `Middleware/`, `ExceptionHandling/`). Main focus. | 5002 |
| `Inventory.Service.Api` | Single project with same internal layering — has real allocation rules. | 5003 |
| `Oms.Api` | Single project, spec-stub with internal `Domain/Application/Infrastructure/Endpoints` folders. | 5001 |
| `Payment.Gateway.Api` | Single project, spec-stub with internal `Domain/Application/Infrastructure/Endpoints` folders. | 5004 |
| `Shared.Contracts` | Message records only. | — |
| `Order.Orchestrator.Tests` | Unit / Consumers / Integration / Stress. | — |

Layering inside each service is by folder + namespace, not project: `Api → Infrastructure → Application → Domain`. Single assembly per service keeps the layout aligned with `handson.md`.

**Two flows, both queue-backed** (deviation — see Conventions):
- *Flow 1*: `POST /orders/pending-ping` → enqueue `PendingSyncRequested` → consumer calls `GET /orders/pending` → fan-out one `AllocateOrderCommand` per order → Inventory `/allocate`.
- *Flow 2*: `POST /payment-confirmed` → publish `PaymentConfirmedEvent` → consumer → Inventory `/reserve`.

**MediatR for separation:** endpoints and MassTransit consumers are thin adapters that `ISender.Send(command)`. Commands per business action: `EnqueuePendingSyncCommand`, `SyncPendingOrdersCommand`, `AllocateOrderCommand`, `PublishPaymentConfirmedCommand`, `ReserveInventoryCommand`. Pipeline behaviors: `LoggingBehavior`, `ValidationBehavior`, `IdempotencyBehavior`.

## Spec-mandated conventions

These bind future code — they come from the spec or from approved deviations. Do not re-derive.

- **Ping handler returns 202 synchronously.** Spec calls for fire-and-forget `Task`; we deviate by enqueuing instead so Flow 1 inherits durability, retries, DLQ, and idempotency. Recorded in `TECH_NOTES.md`.
- **Two-layer DB-enforced idempotency:** Orchestrator `ProcessedMessages` unique idx `(OrderId, OperationType)`; Inventory `InventoryOperations` unique idx `(OrderId, OperationType, Sku)` as safety net.
- **Ping coalescing** — concurrent same-correlation pings collapse to one in-flight sync.
- **Per-order fan-out** in Flow 1 — one bad order DLQs in isolation.
- **Poison vs transient split** — schema/parse errors bypass retries (`r.Ignore<ValidationException>()`); transient (`HttpRequestException`, timeouts) retry with exponential backoff + jitter.
- **Correlation propagation** — `CorrelationId` flows ping → queue header → `X-Correlation-Id` HTTP header → `ILogger` scope. One `Activity` per flow.
- **Health probes split** — `/health/live` (process), `/health/ready` (DB + RabbitMQ + downstream HTTP).
- **Graceful shutdown** — honour `CancellationToken` end-to-end; MassTransit `StopBusAsync`.
- **Domain contracts fixed** — `PendingPingRequest`, `PendingOrder`, `PaymentConfirmedEvent`, `InventoryAllocationRequest` shapes match `handson.md` §"Domain Models" exactly.
- **Stress target** — 100 messages within a documented local target (spec example: 60s); chosen target lives in `TECH_NOTES.md`.

### Resilience catalogue

| Pattern | Where | Mechanism |
|---|---|---|
| Retry (exp + jitter) on consumers | Queue | MassTransit `UseMessageRetry` |
| Retry + Timeout + Circuit Breaker on HTTP | `IOmsClient`, `IInventoryClient` | `Microsoft.Extensions.Http.Resilience` |
| Dead Letter Queue | Queue | MassTransit `_error` (auto) |
| Transactional Outbox | Payment Gateway, Orchestrator | MassTransit EF Core Outbox |
| Inbox (exactly-once) | Orchestrator | MassTransit EF Core Inbox |
| Bulkhead / bounded concurrency | Consumers | `PrefetchCount` + `ConcurrentMessageLimit` |
| Centralized errors → ProblemDetails | Api | .NET 10 `IExceptionHandler` |

### Licensing caveats (record in `TECH_NOTES.md`)

- **MassTransit** v8 free; v9 (~2026) commercial. Pin v8 in `csproj`.
- **MediatR** went commercial in 2024. Alternative: `Mediator` (Cysharp, MIT) or hand-rolled.
- **FluentAssertions** v8+ commercial. Pin v7.x or swap for **AwesomeAssertions** / Shouldly.
- **Moq** v4.20 SponsorLink — acceptable; NSubstitute is the alternative.

## Database schema

One DbContext per service. PK convention: **`INTEGER PRIMARY KEY`** (= ROWID alias in SQLite, no extra index) for purely-internal rows; **natural string PK** only when the value is externally meaningful (e.g. `OrderId` from OMS, `Sku`). Enum convention: **`TEXT` + `CHECK` constraint** mapped via EF `HasConversion<string>()` — no master tables (C# enum is the source of truth).

**`orchestrator.db`**
- `ProcessedMessages(Id INTEGER PK, CorrelationId TEXT?, OrderId TEXT, OperationType TEXT CHECK IN ('Allocate','Reserve'), ProcessedAtUtc DATETIME)` — unique idx `(OrderId, OperationType)`
- `InboxState`, `OutboxState`, `OutboxMessage` — MassTransit-owned

**`inventory.db`**
- `InventoryItems(Sku TEXT PK, Available INT, Reserved INT, RowVersion BLOB)` — seeded
- `InventoryOperations(Id INTEGER PK, OrderId TEXT, OperationType TEXT CHECK IN ('Allocate','Reserve'), Sku TEXT, Quantity INT, CorrelationId TEXT?, OccurredAtUtc DATETIME)` — unique idx `(OrderId, OperationType, Sku)`

**`oms.db`**
- `PendingOrders(OrderId TEXT PK, CustomerId TEXT, ItemsJson TEXT, Total NUMERIC, Status TEXT CHECK IN ('Pending','Picked','Completed'), CreatedAtUtc DATETIME)` — seeded; `GET /orders/pending` filters `Status = 'Pending'`

**`paymentgateway.db`** — `OutboxState`, `OutboxMessage` only (atomic accept + publish).

### Seed data (cross-consistent — SKUs in OMS orders must exist in Inventory)

Per-service `IHostedService` seeder, inserts only if table is empty. OMS: 5 Pending + 1 Picked + 1 Completed (`ORD-1001..1007`, items drawn from `ITEM-A..E`). Inventory: `ITEM-A..F` with stock 100/50/25/200/10/500. `ITEM-F` covers the Flow 2 curl example in `handson.md`.

## Testing strategy

Three layers in `Order.Orchestrator.Tests/{Unit,Consumers,Integration,Stress}`. MassTransit **InMemory transport** + SQLite `:memory:` (`Cache=Shared`, `EnsureCreated()`) in CI — no Docker required.

| Layer | Tooling | Maps to spec test |
|---|---|---|
| Unit | xUnit + Moq + FluentAssertions; mock `IPublishEndpoint`, `IInventoryClient` | `PingHandler_ReturnsImmediately` |
| Consumers | xUnit + `MassTransit.TestFramework` (`ITestHarness`) | `QueueProcessor_ProcessesOrder_Success`, `QueueProcessor_Fails3Times_DeadLetters` |
| Integration | xUnit + `WebApplicationFactory<TProgram>` (all 4 services in-process) | `Flow1_OmsPing_InventoryReceives`, `Flow2_PaymentConfirmed_InventoryReceives` |
| Stress | Same as integration; `[Trait("Category","Stress")]` to skip via `--filter` | `Queue_StressTest_100MessagesProcessedWithinTargetTime` |

Use `Microsoft.Extensions.Time.Testing.FakeTimeProvider` for deterministic time. `WireMock.Net` for HTTP stubbing where needed. Coverlet + ReportGenerator for coverage. Optional Testcontainers-RabbitMQ smoke test for real-broker wiring (not gated).

## Build / test / run commands

Deferred — added here once the solution is scaffolded. Reference shape from `handson.md`:

```bash
dotnet build
dotnet test Order.Orchestrator.Tests
dotnet run --project Order.Orchestrator.Api --urls "http://localhost:5002"
```

Four services run on ports 5001–5004 in separate terminals. RabbitMQ via `docker-compose` (`rabbitmq:3-management`, ports 5672 + 15672). Curl examples for both flow triggers live in `handson.md` §"Sample README Commands".

## Deliverables

`README.md` and `TECH_NOTES.md` are required (see `handson.md` checklist). `TECH_NOTES.md` records: queue choice, AI-tool usage notes, the listed deviations (queue-backed Flow 1, MassTransit-as-`IOrderQueue`, SQLite persistence), and licensing caveats above.
