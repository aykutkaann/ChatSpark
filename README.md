# ChatSpark

A real-time chat and collaboration platform built with ASP.NET Core, following Clean Architecture principles. The system supports workspaces, channels, real-time messaging via SignalR, and includes presence tracking, typing indicators, and read receipts.

## Why This Project Exists

ChatSpark was built as an end-to-end learning project to explore the architecture behind modern chat platforms like Slack and Discord. The focus is on backend design decisions: how to structure a domain-driven codebase, how to handle authentication securely, how to split reads and writes for performance, how to push real-time updates without polling, and how to decouple side-effects using message queues.

Every architectural choice was made deliberately, not to demonstrate a technology, but to solve a specific problem that real-time collaborative applications face at scale.

## Architecture

```
ChatSpark/
|-- ChatSpark.Api              Minimal API endpoints, SignalR hub, Program.cs
|-- ChatSpark.Application      Abstractions (interfaces for auth, caching, events)
|-- ChatSpark.Domain           Entities, enums, domain invariants
|-- ChatSpark.Infrastructure   EF Core, Dapper, Redis, RabbitMQ, JWT implementations
|-- ChatSpark.Shared           DTOs and event contracts
```

The project follows Clean Architecture with an inward dependency rule:

- **Domain** has zero external dependencies. Entities use private setters, static factory methods, and guard clauses to enforce invariants at construction time. Invalid state is unrepresentable.
- **Application** defines abstractions (`IPasswordHasher`, `ITokenService`, `ICacheService`, `IEventPublisher`) without knowing their implementations.
- **Infrastructure** implements those abstractions using concrete technologies (BCrypt, JWT, Redis, RabbitMQ, PostgreSQL).
- **Api** wires everything together using fat Minimal API endpoints. No MediatR, no service classes, no abstraction layers between the endpoint and the work it performs. Each endpoint is a self-contained unit that reads top to bottom.
- **Shared** contains DTOs and event records that cross project boundaries.

## Technology Stack

| Component | Technology | Purpose |
|-----------|-----------|---------|
| Runtime | .NET 10 / ASP.NET Core | Web framework |
| Primary Database | PostgreSQL 16 | Durable storage for all entities |
| ORM (writes) | Entity Framework Core 10 | Change tracking, migrations, domain persistence |
| Query (reads) | Dapper | Raw SQL for read-heavy endpoints (CQRS-lite) |
| Real-Time | SignalR | WebSocket-based push for messages, presence, typing |
| Cache | Redis 7 | Channel list caching, presence sets, read receipt hashes |
| SignalR Backplane | Redis (StackExchangeRedis) | Cross-instance broadcast for horizontal scaling |
| Message Queue | RabbitMQ 3 | Async event processing (message-sent events) |
| Authentication | JWT (HS256) | Stateless access tokens (15 min) |
| Token Rotation | SHA-256 hashed refresh tokens | Secure rotation with replay detection |
| Password Hashing | BCrypt (work factor 12) | Adaptive cost function for credential storage |
| Logging | Serilog | Structured logging with correlation IDs |
| API Docs | Swashbuckle (Swagger) | Interactive API explorer with Bearer auth |
| Naming Convention | EFCore.NamingConventions | PostgreSQL snake_case column mapping |
| Containerization | Docker / Docker Compose | Full-stack local development environment |

## Key Design Decisions

### CQRS-Lite: EF Core for Writes, Dapper for Reads

Write operations go through Entity Framework Core, which provides change tracking, concurrency handling, and transactional guarantees. Read operations bypass EF entirely and use Dapper with hand-written SQL. This avoids the overhead of materializing full entity graphs for read-only queries.

The channel list endpoint, message history, and workspace list all use Dapper. Authorization checks use EF Core (small, indexed lookups where change tracking adds negligible cost).

### Keyset Pagination Over Offset Pagination

Message history uses cursor-based (keyset) pagination: `WHERE sent_at < @Before ORDER BY sent_at DESC LIMIT @Limit`. This provides stable pagination under concurrent writes and leverages the composite index `(channel_id, sent_at DESC)` for O(log n) seeks instead of O(n) offset scans.

### Refresh Token Rotation with Replay Detection

Each refresh token is single-use. On rotation, the old token is revoked and linked to its replacement via `ReplacedByTokenHash`. If a revoked token is presented again (indicating theft and replay), all active tokens for that user are revoked immediately, forcing re-authentication on every device.

Refresh tokens are stored as SHA-256 hashes, not BCrypt. The input is 64 bytes of CSPRNG output, which has sufficient entropy that a fast hash provides no advantage to an attacker, while avoiding the latency of BCrypt on every refresh.

### Fat Endpoints

Endpoints contain the complete request lifecycle: authentication, authorization, validation, persistence, and response mapping. There are no service classes, no mediator pattern, no repository abstraction. Each endpoint reads top to bottom as a single story. This trades reusability for readability and debuggability.

### Soft Deletes for Messages

Deleted messages are not removed from the database. A `DeletedAt` timestamp is set, and read queries filter with `WHERE deleted_at IS NULL`. This preserves audit history and enables potential undo functionality while appearing deleted to end users.

### Ephemeral Presence and Typing via Redis and SignalR

Presence (online/offline) is tracked in Redis Sets, one per channel. Typing indicators are pure SignalR broadcasts with no persistence. Neither feature touches PostgreSQL. If Redis restarts, users appear offline until they rejoin. If the server restarts, typing indicators simply vanish. Both are acceptable trade-offs for ephemeral data.

## API Endpoints

### Authentication

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/auth/register` | Register a new user |
| POST | `/api/auth/login` | Authenticate and receive tokens |
| POST | `/api/auth/refresh` | Rotate refresh token, receive new access token |

### Workspaces

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/workspaces` | Create a workspace (caller becomes Owner) |
| GET | `/api/workspaces` | List workspaces the caller belongs to |
| POST | `/api/workspaces/join` | Join a workspace by slug |
| POST | `/api/workspaces/{workspaceId}/leave` | Leave a workspace (Owners cannot leave) |

### Channels

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/workspaces/{workspaceId}/channels` | Create a channel (Admin/Owner only) |
| GET | `/api/workspaces/{workspaceId}/channels` | List visible channels (cached, respects privacy) |
| POST | `.../channels/{channelId}/archive` | Archive a channel (Admin/Owner only) |
| POST | `.../channels/{channelId}/unarchive` | Unarchive a channel (Admin/Owner only) |
| GET | `.../channels/{channelId}/presence` | Get online user IDs for a channel |
| GET | `.../channels/{channelId}/readreceipts` | Get read receipt state for a channel |

### Messages

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/channels/{channelId}/messages` | Send a message (broadcasts via SignalR) |
| GET | `/api/channels/{channelId}/messages` | Fetch message history (keyset pagination) |
| PATCH | `.../messages/{messageId}` | Edit a message (sender only) |
| DELETE | `.../messages/{messageId}` | Soft-delete a message (sender only) |

### Health Checks

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/health/db` | PostgreSQL connectivity check |
| GET | `/health/redis` | Redis connectivity and latency check |
| GET | `/health/rabbitmq` | RabbitMQ connectivity check |

### SignalR Hub

Endpoint: `/hubs/chat` (WebSocket, requires JWT via query string)

| Method | Direction | Description |
|--------|-----------|-------------|
| `JoinChannel(channelId)` | Client to Server | Subscribe to a channel's real-time feed |
| `LeaveChannel(channelId)` | Client to Server | Unsubscribe from a channel |
| `StartTyping(channelId)` | Client to Server | Broadcast typing indicator to others |
| `StopTyping(channelId)` | Client to Server | Clear typing indicator |
| `MarkAsRead(channelId, messageId)` | Client to Server | Update read receipt |
| `MessageReceived` | Server to Client | New message in a joined channel |
| `MessageEdited` | Server to Client | A message was edited |
| `MessageDeleted` | Server to Client | A message was soft-deleted |
| `UserOnline` | Server to Client | A user joined the channel |
| `UserOffline` | Server to Client | A user left or disconnected |
| `UserStartedTyping` | Server to Client | A user began typing |
| `UserStoppedTyping` | Server to Client | A user stopped typing |
| `MessageRead` | Server to Client | A user's read receipt was updated |

## Data Model

```
User
 |-- id, email, display_name, password_hash, avatar_url, created_at
 |
 |-- RefreshToken (1:N)
 |    |-- id, user_id, token_hash, expires_at, revoked_at, replaced_by_token_hash
 |
 |-- WorkspaceMember (M:N through join table)
 |    |-- id, workspace_id, user_id, role (Owner/Admin/Member), joined_at
 |
 |-- ChannelMember (M:N through join table)
      |-- id, channel_id, user_id, joined_at

Workspace
 |-- id, owner_id, name, slug, created_at
 |
 |-- Channel (1:N)
      |-- id, workspace_id, name, is_private, is_archived, created_at
      |
      |-- Message (1:N)
           |-- id, channel_id, sender_id, content, sent_at, edited_at, deleted_at
```

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)

### Option 1: Docker Compose (full stack)

```bash
git clone https://github.com/aykutkaann/ChatSpark.git
cd ChatSpark
docker compose up --build
```

The API will be available at `http://localhost:8080`. Swagger UI at `http://localhost:8080/swagger`.

### Option 2: Local development

Start the infrastructure services:

```bash
docker compose up postgres redis rabbitmq -d
```

Apply database migrations:

```bash
dotnet ef database update -p ChatSpark.Infrastructure -s ChatSpark.Api
```

Run the API:

```bash
cd ChatSpark.Api
dotnet run
```

The API will be available at `https://localhost:7034`. Swagger UI at `https://localhost:7034/swagger`.

### Verify

```bash
curl http://localhost:8080/health/db
curl http://localhost:8080/health/redis
curl http://localhost:8080/health/rabbitmq
```

All three should return a 200 response with an "up" status.

## Configuration

Configuration is environment-based:

| File | Environment | Host references |
|------|-------------|-----------------|
| `appsettings.Development.json` | Local (`dotnet run`) | `localhost` |
| `appsettings.Docker.json` | Docker Compose | Container service names (`postgres`, `redis`, `rabbitmq`) |

JWT settings, connection strings, and token lifetimes are all configured in these files. The signing key in the repository is a placeholder and must be replaced in any non-local deployment.

## Project Structure

```
ChatSpark.Domain/Entities/         Rich domain models with factory methods and invariants
ChatSpark.Domain/Enum/             Role enum (Owner, Admin, Member)
ChatSpark.Application/Abstractions Interfaces for auth, caching, events
ChatSpark.Infrastructure/Auth/     BCrypt hasher, JWT token service, JWT options
ChatSpark.Infrastructure/Caching/  Redis cache service implementation
ChatSpark.Infrastructure/Messaging RabbitMQ publisher and consumer
ChatSpark.Infrastructure/Persistence/
  |-- AppDbContext.cs              EF Core context with 7 DbSets
  |-- Configurations/              Per-entity EF Core configurations
  |-- Migrations/                  Database migration history
  |-- IDbConnectionFactory.cs      Dapper connection abstraction
  |-- NpgsqlConnectionFactory.cs   Npgsql implementation
ChatSpark.Api/Endpoints/           Fat Minimal API endpoint files
ChatSpark.Api/Hubs/                SignalR ChatHub with presence tracking
ChatSpark.Shared/Dtos/             Request/response records
ChatSpark.Shared/Events/           Async event contracts
```

## License

This project was built for educational purposes.
