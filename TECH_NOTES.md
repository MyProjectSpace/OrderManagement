# TECH_NOTES.md

Companion to [README.md](README.md) and [handson.md](handson.md). Records the queue choice, how AI tooling was used across the build, and every place this implementation deviates from the spec along with the reasoning.

---

## 1. Queue choice — RabbitMQ via MassTransit

The spec lists three queue options and asks for the choice to be swappable via DI. The trade-off:

| Option         | Durable | Real broker semantics | Local setup                          | Verdict                                                                                                             |
| -------------- | ------- | --------------------- | ------------------------------------ | ------------------------------------------------------------------------------------------------------------------- |
| `Channel<T>`   | No      | No (in-process only)  | None                                 | Fast to wire, but dies with the process — defeats the purpose of testing the resilience patterns the spec asks for. |
| **RabbitMQ**   | **Yes** | **Yes**               | **Docker (`rabbitmq:3-management`)** | **Chosen.** Real broker, free, mature, first-class DX in C#.                                                        |
| LocalStack SQS | Yes     | AWS shape, simulated  | Docker (LocalStack)                  | Same Docker overhead as RabbitMQ but no real broker insight; only useful if the target runtime is AWS.              |

**Why RabbitMQ:** production-grade semantics, MassTransit is the de-facto C# client and gives us inbox/outbox, retry middleware, dead-letter queues, and `ITestHarness` from one library. Adopting MassTransit means the application code never directly talks to RabbitMQ APIs — only to `IPublishEndpoint` / `IConsumer<T>`.

**Swappability:** the same `IPublishEndpoint` / `IConsumer<T>` surface stays in place regardless of transport. `Order.Orchestrator.Api/Infrastructure/DependencyInjection.cs` and `Payment.Gateway.Api/Infrastructure/DependencyInjection.cs` both read `MessageBroker:UseInMemory` from configuration:

- `false` (default) → `UsingRabbitMq(...)` against the host configured in `appsettings.json`.
- `true` (tests, local-only dev) → `UsingInMemory(...)` — no Docker required.

This is what the spec deliverable "`IOrderQueue` implementation (your choice with configuration)" reduces to in practice — see deviation 3.2 below.

---

## 2. AI agent usage

Claude Code (Anthropic) was used throughout the build.

---

## 3. Deviations from spec, with justifications

The spec explicitly invites deviations: _"Deviations from this spec are welcome — if you have a better design decision, make it and document your reasoning in `TECH_NOTES.md`."_ What follows is every place this codebase diverges.

### 3.1 Ping handler enqueues instead of fire-and-forget `Task`

**Spec:** ping handler should "Start `SyncPendingOrdersAsync` as best-effort background work without blocking the HTTP response."
**This codebase:** the handler still returns `202 Accepted` immediately, but the work it kicks off is an enqueued `PendingSyncRequested` message instead of a detached `Task`.
**Why:** the queued path inherits durability (survives a crash mid-sync), retry with exponential back-off + jitter, dead-letter quarantine for poison messages, and the two-layer idempotency described in 3.7. A bare `Task.Run` has none of these and silently swallows failures across process restarts.

### 3.2 MassTransit replaces the `IOrderQueue` abstraction

**Spec deliverable:** "`IOrderQueue` implementation (your choice with configuration)."
**This codebase:** no `IOrderQueue` interface exists. Producers use `IPublishEndpoint`; consumers implement `IConsumer<T>`.
**Why:** MassTransit already provides a swappable transport surface (RabbitMQ ↔ InMemory) behind these interfaces. A bespoke `IOrderQueue` wrapper would re-invent the same indirection and add a translation layer that does no work of its own.

### 3.3 MassTransit `IConsumer<T>` instead of literal `BackgroundService`

**Spec:** "Implement `BackgroundService` that continuously dequeues `PaymentConfirmedEvent`."
**This codebase:** `PaymentConfirmedConsumer` (and the two Flow 1 consumers) implement `IConsumer<T>`.
**Why:** MassTransit hosts consumers via its own internal `IHostedService`, so the spec's literal requirement is met functionally. Using `IConsumer<T>` lets us layer middleware (retry, outbox, idempotency) declaratively rather than re-coding it inside each `BackgroundService.ExecuteAsync` loop.

### 3.4 SQLite for OMS + Payment Gateway

**Spec:** "OMS and Payment Gateway are stub services — they return hardcoded/in-memory data; no real DB required."
**This codebase:** both services have a SQLite database.
**Why:** Payment Gateway needs durable state to host a transactional outbox (atomic `accept payment → publish event`). OMS needs a real backing store so the `GET /orders/pending` filter `Status = 'Pending'` is exercised against real query semantics, not a static list. Both DBs are seeded once via `IHostedService`-based seeders that no-op on second start.

### 3.5 Clean-architecture folder layering inside single Api projects

**Spec:** lists `Oms.Api/`, `Order.Orchestrator.Api/`, `Inventory.Service.Api/`, `Payment.Gateway.Api/` as single projects.
**This codebase:** matches that layout exactly. Internally, each `*.Api/` project contains `Domain/`, `Application/`, `Infrastructure/`, `Endpoints/` (and `Middleware/`, `ExceptionHandling/` where applicable) folders with the corresponding namespaces.
**Why:** preserves the Domain/Application/Infrastructure separation that clean architecture is actually about (dependency direction in source, not csproj count) while leaving the project list spec-aligned.

### 3.6 Split health endpoints (`/health/live` + `/health/ready`)

**Spec:** "Expose a simple mechanism to report service health and dependency status."
**This codebase:** every service exposes `/health/live` (process-only) and `/health/ready` (DB connectivity, plus broker once health-checks for that are wired).
**Why:** the standard Kubernetes-style probe split is both simple and correct. A single `/health` endpoint that mixes liveness and readiness causes orchestrators to recycle a pod that's just slow to reach a downstream — the worst possible reaction.

### 3.7 Two-layer DB-enforced idempotency

**Beyond spec:** Orchestrator owns `ProcessedMessages` with a unique index on `(OrderId, OperationType)`. Inventory owns `InventoryOperations` with a unique index on `(OrderId, OperationType, Sku)`.
**Why:** MassTransit's inbox/outbox already gives exactly-once delivery within the message pipeline, but a duplicate HTTP retry from Orchestrator → Inventory bypasses that. The Inventory-side unique index is defence in depth that catches duplicates regardless of what produced them.

---

## 4. Out of scope / open items

Honest list of things this codebase does _not_ do, so reviewers can reason about gaps:

- **Schema versioning.** DB schemas are currently created with `EnsureCreated()` at startup. Migrating to versioned EF Core migrations is an open decision (and a separate conversation about apply-strategy: startup auto-apply vs. CI-applied SQL scripts).
- **Observability beyond logs.** Structured Serilog console output is wired (the spec accepts this: _"Structured logging to console is sufficient; no external observability platform needed"_). No OpenTelemetry exporter, no metrics, no traces shipped anywhere.
- **AuthN/AuthZ between services.** Spec: _"All services run locally; no auth/TLS required between services."_ This codebase honours that.
