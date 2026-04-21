# ChatSpark

A full-stack real-time chat platform built from scratch — Discord/Slack style. The backend is ASP.NET Core with SignalR, PostgreSQL, Redis, and RabbitMQ. The frontend is React with TypeScript. The whole stack runs in Docker with a single command.

> Built as an end-to-end learning project. Every decision was made deliberately — not to show off a technology, but to solve a real problem that chat platforms face.

---

## What It Does

- Create workspaces and invite people with a slug or invite code
- Create public and private channels inside a workspace
- Send text messages, images, and voice recordings in real time
- See who is online, who is typing, and who has read a message
- Edit and soft-delete your own messages
- Upload a profile picture, set a bio and website
- Click any user's avatar or name to view their public profile
- Full JWT authentication with secure refresh token rotation

---

## Technology Stack

### Backend

| Component | Technology | Why |
|-----------|-----------|-----|
| Runtime | .NET 10 / ASP.NET Core | Web framework |
| Database | PostgreSQL 16 | Durable relational storage |
| ORM (writes) | Entity Framework Core 10 | Change tracking, migrations |
| Queries (reads) | Dapper | Raw SQL for read-heavy endpoints |
| Real-Time | SignalR | WebSocket push for messages, presence, typing |
| Cache | Redis 7 | Channel list cache, presence sets, read receipts |
| SignalR Backplane | Redis (StackExchangeRedis) | Cross-instance broadcast |
| Message Queue | RabbitMQ 3 | Async event processing |
| Auth | JWT HS256 + BCrypt | Stateless tokens, secure password hashing |
| Logging | Serilog | Structured logs with correlation IDs |
| API Docs | Swagger / Swashbuckle | Interactive API explorer |
| Containerization | Docker Compose | Full local environment |

### Frontend

| Component | Technology | Why |
|-----------|-----------|-----|
| Framework | React 19 + TypeScript | UI layer |
| Build Tool | Vite | Fast dev server and bundler |
| Routing | React Router v7 | Client-side navigation |
| HTTP Client | Axios | API calls with token interceptors |
| Real-Time | SignalR JS client | WebSocket connection to the hub |
| Styling | Plain CSS with variables | Discord-dark design system, no CSS framework |
| Voice Recording | MediaRecorder API | Browser-native audio capture |
| File Uploads | FormData + Fetch | Images and voice messages |

---

## Architecture

### Backend Layers

```
ChatSpark/
├── ChatSpark.Api              Minimal API endpoints, SignalR hub, Program.cs
├── ChatSpark.Application      Abstractions (IPasswordHasher, ITokenService, ICacheService, IEventPublisher)
├── ChatSpark.Domain           Entities, enums, domain invariants (no external dependencies)
├── ChatSpark.Infrastructure   EF Core, Dapper, Redis, RabbitMQ, JWT implementations
└── ChatSpark.Shared           DTOs and event contracts shared across layers
```

The dependency rule goes inward: Api → Application/Infrastructure → Domain. Domain knows nothing about ASP.NET, EF Core, or Redis. Entities use private setters, static factory methods, and guard clauses so invalid state is impossible to construct.

### Frontend Structure

```
chatspark-client/src/
├── api/           Axios wrappers for each API resource (auth, workspace, channel, message, profile)
├── components/
│   ├── auth/      Login and register forms
│   ├── channel/   ChannelSidebar (workspace nav, channel list, settings)
│   ├── chat/      MessageList, MessageItem, MessageInput, TypingIndicator, PresenceBar
│   ├── layout/    App shell and protected routes
│   ├── profile/   ProfileSettingsModal (editable), UserProfileModal (read-only view)
│   └── workspace/ MembersPanel (online/offline list)
├── context/       AuthContext (user state, token management), SignalRContext (hub connection)
├── hooks/         useMessages, usePresence, useTyping, useReadReceipts
├── pages/         LoginPage, RegisterPage, WorkspacesPage, ChatPage
├── styles/        CSS files per concern (global, layout, channel, chat, auth, workspace)
├── types/         TypeScript interfaces (message, channel, workspace, profile)
└── utils/         dateFormat, tokenStorage
```

---

## Key Design Decisions

### CQRS-Lite: EF Core for Writes, Dapper for Reads

All writes go through Entity Framework Core for change tracking, concurrency, and transactional guarantees. Reads that return lists (messages, channels, workspaces, members) bypass EF and use Dapper with hand-written SQL. This avoids materializing full entity graphs for data that will be immediately serialized to JSON.

### Keyset Pagination for Messages

Message history uses `WHERE sent_at < @Before ORDER BY sent_at DESC LIMIT @Limit` instead of `OFFSET`. This is stable under concurrent inserts (no rows jumping between pages) and uses the composite index `(channel_id, sent_at DESC)` for O(log n) seeks regardless of how many messages exist.

### Refresh Token Rotation with Replay Detection

Each refresh token is single-use. On rotation, the old token is revoked and linked to its replacement via `ReplacedByTokenHash`. If a revoked token is presented again, it means a replay attack — all tokens for that user are immediately revoked, forcing re-login on every device. Tokens are stored as SHA-256 hashes (not BCrypt) because the 64-byte random input already has sufficient entropy.

### Fat Endpoints, No Service Classes

Each endpoint contains its full lifecycle: authorization, validation, persistence, response mapping. No mediator, no repository abstraction, no separate service layer. Each endpoint reads top to bottom as a single story. This trades reusability for readability — you can understand exactly what a request does without jumping between files.

### Soft Deletes for Messages

`DELETE` sets `deleted_at` instead of removing the row. Read queries filter with `WHERE deleted_at IS NULL`. This preserves history and keeps the door open for undo. The domain method `message.Delete(userId)` enforces that only the sender can delete, and that a message cannot be deleted twice.

### Ephemeral Presence via Redis Sets

Online users are tracked in Redis Sets (one per channel). When a user joins a channel via SignalR, their user ID is added. When they disconnect, it is removed. Workspace presence is the union of all channel Sets. Nothing touches PostgreSQL. If Redis restarts, presence resets — an acceptable trade-off for ephemeral data.

### Media Messages via Multipart Upload

Images and voice messages follow the same pattern as text: they get a `Message` row in the database and are broadcast via SignalR so all clients update in real time. The file is saved to `wwwroot/uploads/` and the relative path is stored as `file_url`. `MessageType` (Text/Image/Voice) tells the frontend how to render it. Content-type validation strips codec suffixes (e.g., `audio/webm;codecs=opus` → `audio/webm`) before matching.

### Frontend Real-Time via Hooks

SignalR is wrapped in a React context so any component can access the hub connection. The `useMessages` hook manages message state, subscribes to `MessageReceived`/`MessageEdited`/`MessageDeleted` events, and exposes `sendMessage`, `editMessage`, `deleteMessage`, and `uploadMedia`. The hook uses a `ref` to track the active channel ID so SignalR callbacks always read the latest value without re-subscribing.

---

## Features

### Workspaces
- Create a workspace (creator becomes Owner)
- Join by public slug or private invite code
- Leave a workspace (Owners cannot leave)
- Delete a workspace (Owner only)
- See all members with online/offline status

### Channels
- Public and private channels
- Private channels require an invite code to join
- Admin/Owner can create and delete channels
- Owners/Admins can archive/unarchive channels

### Messaging
- Real-time text messages via WebSocket
- Send images (JPEG, PNG, GIF, WebP — max 5 MB)
- Send voice messages recorded in-browser (max 10 MB)
- Edit your own text messages
- Soft-delete your own messages (any type)
- Infinite scroll with keyset pagination (load older messages)
- Date dividers between message groups
- Typing indicator shows who is currently typing
- Read receipts tracked per user per channel

### User Profiles
- Display name, bio, website URL, avatar
- Upload avatar from file (JPEG/PNG, max 2 MB)
- Click any avatar or username to view a read-only profile card
- Edit your own profile via the settings modal (⚙ button)

### Authentication
- Register and login with email + password
- Access tokens valid for 15 minutes
- Refresh tokens rotate automatically (single-use, replay detection)
- Auto-refresh on 401 — users stay logged in silently

---

## API Reference

### Authentication

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/auth/register` | Register a new user |
| POST | `/api/auth/login` | Login and receive access + refresh tokens |
| POST | `/api/auth/refresh` | Rotate refresh token |

### Workspaces

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/workspaces` | Create a workspace |
| GET | `/api/workspaces` | List workspaces the caller is a member of |
| POST | `/api/workspaces/join` | Join by slug |
| GET | `/api/workspaces/{id}/members` | List all members with presence |
| POST | `/api/workspaces/{id}/leave` | Leave (Owners cannot leave) |
| DELETE | `/api/workspaces/{id}` | Delete workspace (Owner only) |

### Channels

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/workspaces/{workspaceId}/channels` | Create a channel |
| GET | `/api/workspaces/{workspaceId}/channels` | List visible channels (cached) |
| DELETE | `/api/workspaces/{workspaceId}/channels/{channelId}` | Delete a channel |
| POST | `.../channels/{channelId}/archive` | Archive |
| POST | `.../channels/{channelId}/unarchive` | Unarchive |
| POST | `.../channels/{channelId}/join` | Join a private channel with invite code |
| GET | `.../channels/{channelId}/presence` | Get online user IDs |
| GET | `.../channels/{channelId}/readreceipts` | Get read receipt state |

### Messages

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/channels/{channelId}/messages` | Send a text message |
| GET | `/api/channels/{channelId}/messages` | Fetch history (keyset pagination) |
| PATCH | `.../messages/{messageId}` | Edit a message (sender only) |
| DELETE | `.../messages/{messageId}` | Soft-delete (sender only) |
| POST | `.../messages/upload` | Upload an image or voice message |

### Profile

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/profile` | Get own full profile |
| PATCH | `/api/profile` | Update display name, bio, website |
| POST | `/api/profile/avatar` | Upload avatar image |
| GET | `/api/users/{userId}` | Get any user's public profile (no email) |

### Health

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/health/redis` | Redis ping and latency |
| GET | `/health/rabbitmq` | RabbitMQ connectivity |

### SignalR Hub — `/hubs/chat`

JWT is passed as the `access_token` query parameter (required by SignalR WebSocket).

**Client → Server**

| Method | Description |
|--------|-------------|
| `JoinChannel(channelId)` | Subscribe to a channel, mark user online |
| `LeaveChannel(channelId)` | Unsubscribe, mark user offline |
| `StartTyping(channelId)` | Broadcast typing start to others |
| `StopTyping(channelId)` | Broadcast typing stop |
| `MarkAsRead(channelId, messageId)` | Update read receipt |

**Server → Client**

| Event | Payload | Description |
|-------|---------|-------------|
| `MessageReceived` | `MessageResponse` | New message (text, image, or voice) |
| `MessageEdited` | `MessageResponse` | Message was edited |
| `MessageDeleted` | `Guid` | Message was soft-deleted |
| `UserOnline` | `string (userId)` | A user connected to the channel |
| `UserOffline` | `string (userId)` | A user disconnected |
| `UserStartedTyping` | `string (userId)` | A user started typing |
| `UserStoppedTyping` | `string (userId)` | A user stopped typing |
| `MessageRead` | `{ userId, messageId }` | A user's read receipt updated |

---

## Data Model

```
User
 ├── id, email, display_name, password_hash, avatar_url, bio, website_url, created_at
 ├── RefreshToken (1:N)
 │    └── id, user_id, token_hash, expires_at, revoked_at, replaced_by_token_hash
 ├── WorkspaceMember (M:N)
 │    └── id, workspace_id, user_id, role (Owner/Admin/Member), joined_at
 └── ChannelMember (M:N)
      └── id, channel_id, user_id, joined_at

Workspace
 ├── id, owner_id, name, slug, invite_code, created_at
 └── Channel (1:N)
      ├── id, workspace_id, name, is_private, is_archived, invite_code, created_at
      └── Message (1:N)
           └── id, channel_id, sender_id, content, message_type (0=Text/1=Image/2=Voice),
               file_url, sent_at, edited_at, deleted_at
```

---

## Getting Started

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for the quick start)
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (only for local dev without Docker)
- [Node.js 20+](https://nodejs.org/) (only for local frontend dev without Docker)

### Quick Start — Docker (recommended)

This runs the full stack: PostgreSQL, Redis, RabbitMQ, the API, and the React frontend.

```bash
git clone https://github.com/aykutkaann/ChatSpark.git
cd ChatSpark
docker compose up --build
```

| Service | URL |
|---------|-----|
| Frontend | http://localhost |
| API | http://localhost:8080 |
| Swagger UI | http://localhost:8080/swagger |
| RabbitMQ UI | http://localhost:15672 (user: `chatspark`, pass: `chatspark_dev`) |

Migrations run automatically on startup. Open http://localhost, register an account, and start chatting.

> **If you make code changes**, rebuild without cache so Docker picks up the new code:
> ```bash
> docker compose build --no-cache api client && docker compose up -d
> ```

### Local Development (without Docker)

**1. Start infrastructure services only:**

```bash
docker compose up postgres redis rabbitmq -d
```

**2. Apply migrations and run the API:**

```bash
dotnet ef database update -p ChatSpark.Infrastructure -s ChatSpark.Api
cd ChatSpark.Api
dotnet run
```

API runs at `https://localhost:7034`. Swagger at `https://localhost:7034/swagger`.

**3. Run the frontend:**

```bash
cd chatspark-client
npm install
npm run dev
```

Frontend runs at `http://localhost:5173`.

The `.env` file points `VITE_API_URL` to `https://localhost:7034` for local dev. The `.env.docker` file points it to `http://localhost:8080` for Docker.

### Verify Everything Is Running

```bash
curl http://localhost:8080/health/redis
curl http://localhost:8080/health/rabbitmq
```

Both should return `{ "..": "up" }`.

---

## Configuration

| File | Used When |
|------|-----------|
| `appsettings.Development.json` | `dotnet run` locally — connects to `localhost` |
| `appsettings.Docker.json` | Docker Compose — uses container service names (`postgres`, `redis`, `rabbitmq`) |
| `chatspark-client/.env` | Vite dev server — points to `https://localhost:7034` |
| `chatspark-client/.env.docker` | Docker client build — points to `http://localhost:8080` |

The JWT signing key in the repo is a placeholder. Replace it with a strong random key in any non-local deployment.

---

## Project Structure

```
ChatSpark/
│
├── ChatSpark.Domain/
│   ├── Entities/              User, Workspace, Channel, Message, WorkspaceMember,
│   │                          ChannelMember, RefreshToken
│   └── Enum/                  Role (Owner, Admin, Member), MessageType (Text, Image, Voice)
│
├── ChatSpark.Application/
│   └── Abstractions/          IPasswordHasher, ITokenService, ICacheService, IEventPublisher
│
├── ChatSpark.Infrastructure/
│   ├── Auth/                  BCryptPasswordHasher, JwtTokenService
│   ├── Caching/               RedisCacheService
│   ├── Messaging/             RabbitMqPublisher, MessageSentConsumer
│   └── Persistence/
│       ├── AppDbContext.cs
│       ├── Configurations/    Per-entity EF Core table configs
│       ├── Migrations/        Migration history
│       └── NpgsqlConnectionFactory.cs
│
├── ChatSpark.Api/
│   ├── Endpoints/             AuthEndpoints, WorkspaceEndpoints, ChannelEndpoints, MessageEndpoints
│   ├── Hubs/                  ChatHub (SignalR — presence, typing, read receipts)
│   └── Program.cs             DI wiring, middleware, profile endpoints
│
├── ChatSpark.Shared/
│   ├── Dtos/                  Request/response records (Messages, Users, Channels, Workspaces)
│   └── Events/                MessageSentEvent (RabbitMQ contract)
│
└── chatspark-client/
    └── src/
        ├── api/               axios.ts, authApi, channelApi, messageApi, profileApi, workspaceApi
        ├── components/
        │   ├── chat/          MessageList, MessageItem, MessageInput, TypingIndicator, PresenceBar
        │   ├── channel/       ChannelSidebar
        │   ├── profile/       ProfileSettingsModal, UserProfileModal
        │   └── workspace/     MembersPanel
        ├── context/           AuthContext, SignalRContext
        ├── hooks/             useMessages, usePresence, useTyping, useReadReceipts
        ├── pages/             LoginPage, RegisterPage, WorkspacesPage, ChatPage
        ├── styles/            global.css, layout.css, chat.css, channel.css, auth.css, workspace.css
        └── types/             message.ts, channel.ts, workspace.ts, profile.ts
```

---

## License

Built for educational purposes.
