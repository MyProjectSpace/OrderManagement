# OrderProcessing

E-commerce order-processing system implementing the [`handson.md`](handson.md) take-home spec. Four .NET 10 microservices coordinated by an Order Orchestrator, communicating sync over HTTP and async over RabbitMQ via MassTransit. Design notes and spec deviations live in [`TECH_NOTES.md`](TECH_NOTES.md).

---

## Prerequisites

- **.NET 10 SDK** (`dotnet --version` → `10.x`)
- **Docker** (for RabbitMQ; the four services run on the host with `dotnet run`)
- Optional: `curl` for the smoke tests at the bottom

---

## Services and ports

| Service                | Project                  | Port                             |
| ---------------------- | ------------------------ | -------------------------------- |
| OMS (stub)             | `Oms.Api`                | `5001`                           |
| Order Orchestrator     | `Order.Orchestrator.Api` | `5002`                           |
| Inventory Service      | `Inventory.Service.Api`  | `5003`                           |
| Payment Gateway (stub) | `Payment.Gateway.Api`    | `5004`                           |
| RabbitMQ               | docker-compose           | `5672` (AMQP), `15672` (mgmt UI) |

Each service exposes `/health/live` (process) and `/health/ready` (DB + downstream).

---

## Build

```bash
dotnet build OrderProcessing.slnx
```

All six projects (4 services + `Shared.Contracts` + `Order.Orchestrator.Tests`) should compile clean.

---

## Run

### 1. Start RabbitMQ

```bash
docker compose up -d rabbitmq
```

Confirm it's reachable: management UI at <http://localhost:15672> (login `guest` / `guest`). The Orchestrator and Payment Gateway will not start cleanly without it (they connect at host startup).

If you want to run **without Docker** for a quick smoke test, set `MessageBroker:UseInMemory=true` for both services — see [Run without RabbitMQ](#run-without-rabbitmq) below.

### 2. Start the four services (one terminal each)

```bash
dotnet run --project Oms.Api                --urls http://localhost:5001
dotnet run --project Order.Orchestrator.Api --urls http://localhost:5002
dotnet run --project Inventory.Service.Api  --urls http://localhost:5003
dotnet run --project Payment.Gateway.Api    --urls http://localhost:5004
```

On first run each service:

- Creates its SQLite DB next to the project (`oms.db`, `orchestrator.db`, `inventory.db`, `paymentgateway.db`) via `EnsureCreated()`.
- Seeds reference data via an `IHostedService`:
  - **OMS** seeds 5 `Pending` + 1 `Picked` + 1 `Completed` orders (`ORD-1001`..`ORD-1007`) using SKUs `ITEM-A..E`.
  - **Inventory** seeds `ITEM-A..F` with stock `100/50/25/200/10/500`.

Re-runs are idempotent — seeders no-op if data already exists.

## Test

```bash
dotnet test Order.Orchestrator.Tests
```

Skip the stress test in routine runs:

```bash
dotnet test Order.Orchestrator.Tests --filter "Category!=Stress"
```

Run only the stress test:

```bash
dotnet test Order.Orchestrator.Tests --filter "Category=Stress"
```

Test layers (see `Order.Orchestrator.Tests/{Unit,Consumers,Integration,Stress}`):

| Layer       | Notes                                                                                                                |
| ----------- | -------------------------------------------------------------------------------------------------------------------- |
| Unit        | xUnit + Moq + FluentAssertions — handlers, behaviours, idempotency store.                                            |
| Consumers   | `MassTransit.TestFramework.ITestHarness` against in-memory transport.                                                |
| Integration | `WebApplicationFactory<Program>` — boots all four services in-process, uses MassTransit InMemory transport + SQLite. |
| Stress      | 100-message Flow 2 enqueue, target documented in [`TECH_NOTES.md`](TECH_NOTES.md) §4.                                |

---

## Smoke tests (curl)

### Flow 1 — OMS ping → pull → allocate

```bash
curl -X POST "http://localhost:5002/orders/pending-ping" -H "Content-Type: application/json" -d "{\"correlationId\":\"ping-123\"}"
```

Expect `202 Accepted` immediately. Watch the Inventory log for five `Allocate` calls (one per seeded `Pending` order).

### Flow 2 — Payment confirmed → reserve

```bash
curl -X POST http://localhost:5004/payment-confirmed \
  -H "Content-Type: application/json" \
  -d '{"orderId":"ORD-123","customerId":"CUST-1","items":["ITEM-A"],"total":99.99,"paidAt":"2025-01-15T10:30:00Z"}'
```

Expect `202 Accepted` from Payment Gateway. The event is published, consumed by the Orchestrator, and turned into a `POST /inventory/reserve` against Inventory.

### Health probes

```bash
curl http://localhost:5002/health/live    # process — fast
curl http://localhost:5002/health/ready   # DB + dependencies — JSON body
```

Repeat with ports `5001`, `5003`, `5004` for the other services.

---

## Reset state

The SQLite files persist between runs. To wipe and re-seed:

```bash
rm Oms.Api/oms.db Order.Orchestrator.Api/orchestrator.db* \
   Inventory.Service.Api/inventory.db Payment.Gateway.Api/paymentgateway.db
```

(`orchestrator.db*` covers the `-shm` / `-wal` companion files SQLite creates.)

To wipe the broker state too:

```bash
docker compose down -v
docker compose up -d rabbitmq
```

---

## Where things live

```
OrderProcessing/
├── Oms.Api/                   # POST /orders/pending-ping caller, GET /orders/pending
├── Order.Orchestrator.Api/    # main focus — both flows
├── Inventory.Service.Api/     # POST /inventory/{allocate,reserve}
├── Payment.Gateway.Api/       # POST /payment-confirmed (Flow 2 trigger)
├── Shared.Contracts/          # message records (PendingPingRequest, PaymentConfirmedEvent, …)
├── Order.Orchestrator.Tests/  # Unit + Consumers + Integration + Stress
├── docker-compose.yml         # RabbitMQ
├── handson.md                 # source-of-truth spec
├── TECH_NOTES.md              # design rationale + deviations + AI usage
└── CLAUDE.md                  # working notes for the Claude Code agent
```
