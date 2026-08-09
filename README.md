# LogiTrack - Secure Inventory & Order Management API

A .NET 10 ASP.NET Core Web API built to manage warehouse inventory and customer orders while demonstrating persistent data, secure access control, input validation, application caching, and Entity Framework Core query optimization.

---

## What Was Required

| Criterion | LogiTrack Approach |
|---|---|
| **Key Features** | JWT account access, inventory management, and order management with nested items |
| **Development Challenges** | Resolved JSON reference cycles, authentication-scheme conflicts, foreign-key input errors, and framework/package compatibility issues |
| **Business Logic, Caching & State** | Controller-based workflows, EF Core persistence, stateless JWTs, and an invalidated 30-second inventory cache |
| **Security** | ASP.NET Core Identity, signed JWTs, `User`/`Manager` roles, protected routes, and validated request models |
| **Performance** | No-tracking reads, eager loading, split queries, asynchronous database calls, and measured cache hits |

---

## The Application

**Stack:** .NET 10 · ASP.NET Core Web API · Entity Framework Core · SQLite · ASP.NET Core Identity · JWT Bearer · Swagger · In-Memory Cache

The application stores inventory, orders, order items, users, and roles in SQLite. EF Core migrations maintain the schema, while Swagger provides an interactive development interface that can authenticate with the JWT returned by the login endpoint.

### 1 - Authentication and User Access

Users register through `POST /api/auth/register` and are assigned the `User` role rather than choosing their own privileges. Login through `POST /api/auth/login` verifies the password with `SignInManager` and returns a signed JWT containing the user ID, username, unique token ID, and role claims. Tokens expire after one hour.

This feature provides:

- Identity-managed password hashing and account storage
- Lockout-aware password verification
- Role claims embedded in JWTs
- Swagger bearer-token authorization for protected endpoint testing

### 2 - Inventory Management

Authenticated users can retrieve the inventory through `GET /api/inventory`. Managers can add items through `POST /api/inventory` and remove them through `DELETE /api/inventory/{id}`.

Inventory creation uses a dedicated request model so callers provide only `Name`, `Quantity`, and `Location`. Quantity must be non-negative, and database-managed IDs and order relationships are not accepted from the client. Missing delete targets return a descriptive `404 ProblemDetails` response.

### 3 - Order Management

Authenticated users can create orders containing one or more nested inventory items, retrieve all orders, and retrieve an individual order with its items. Managers can delete orders.

Order creation uses `CreateOrderRequest` and `CreateOrderItemRequest` instead of binding the EF entities directly. The request requires a customer name, placement date, at least one item, and positive item quantities. EF Core models the relationship as one order to many inventory items and applies cascade deletion when an order is removed.

---

## Business Logic, Caching & State Management

### Business Logic

The API is organized around three controllers with distinct responsibilities:

| Component | Responsibility |
|---|---|
| `AuthController` | Registers users, verifies credentials, assigns the default role, and creates JWTs |
| `InventoryController` | Applies inventory read/write rules, manages cache entries, and returns item-specific errors |
| `OrderController` | Maps validated requests to persisted orders, loads nested items, and enforces manager-only deletion |

Controllers receive `LogiTrackContext`, Identity services, and `IMemoryCache` through dependency injection. Request models handle API-boundary validation, controllers coordinate the workflow, and EF Core handles persistence and relationships. This keeps client input separate from database-managed entity fields.

### Caching

Inventory reads use `IMemoryCache` with the key `inventory-items` and a 30-second absolute lifetime.

1. A request first checks the in-process cache.
2. A cache miss queries SQLite with `AsNoTracking()` and stores the result.
3. A cache hit returns the stored inventory without another database query.
4. Successful inventory creation or deletion removes the cache entry.
5. The next read repopulates the cache from current database state.

The API exposes `X-Cache: HIT|MISS` and `X-Elapsed-Milliseconds` response headers. These headers make cache behavior and elapsed time directly observable rather than relying on an assumed performance improvement.

### State Management

Durable application state is stored in `logitrack.db`. Inventory, orders, nested items, users, roles, and role assignments survive application restarts. `LogiTrackContext` inherits from `IdentityDbContext<ApplicationUser>`, allowing business and Identity records to share one configured database and migration history.

Authentication is stateless between requests: each protected request supplies a JWT, and the API validates its signature, issuer, audience, lifetime, and role claims. The inventory cache is temporary process state only; cache invalidation and the 30-second expiration ensure SQLite remains the source of truth.

---

## Security Approach

### Authentication

ASP.NET Core Identity manages user records and password hashes. Registration never accepts a role from the caller; every public registration receives the `User` role. Development manager credentials are read from .NET user-secrets, and startup seeding creates the `User` and `Manager` roles when necessary.

JWT bearer authentication is explicitly configured as the default authenticate and challenge scheme. Token validation requires:

- A valid HMAC signing key
- The configured issuer and audience
- An unexpired token
- A valid signature
- Recognized ASP.NET Core role claims

Invalid credentials return `401 Unauthorized` without revealing whether the username or password was incorrect.

### Role-Based Access

| Operation | Anonymous | User | Manager |
|---|---:|---:|---:|
| Read inventory | `401` | Allowed | Allowed |
| Add/delete inventory | `401` | `403` | Allowed |
| Read/create orders | `401` | Allowed | Allowed |
| Delete orders | `401` | `403` | Allowed |

Class-level `[Authorize]` attributes protect inventory and order routes. Manager-only operations add `[Authorize(Roles = "Manager")]`, so authorization is enforced by the API even when a caller bypasses Swagger or another client interface.

### Input Validation

`[ApiController]` automatically validates request models before controller actions execute. Data annotations enforce required usernames, valid email addresses, minimum password length, required inventory fields, non-negative inventory quantities, required order dates, at least one order item, and positive order-item quantities.

Dedicated creation models prevent over-posting of entity IDs, foreign keys, and navigation properties. The order-to-item navigation back-reference is ignored during JSON serialization, preventing circular object graphs from producing server errors.

---

## Performance Optimization

### Query Improvements

Read-only inventory and order queries use `AsNoTracking()`, avoiding EF Core change-tracking allocations for objects that will not be updated. Order queries use `Include(order => order.Items)` so related items are loaded with the order instead of triggering per-order lookups.

The order-list endpoint adds `AsSplitQuery()`. EF Core can therefore load the parent orders and child collection with separate SQL queries instead of producing one large joined result containing repeated order columns. All database access uses asynchronous EF Core methods so request threads are not blocked during I/O.

### Cache Strategy and Measurement

Inventory data is frequently read and changes less often, making it a suitable cache target. A 30-second lifetime limits stale-data risk, while explicit invalidation after successful writes preserves correctness.

Final workflow measurements showed an average uncached inventory read of **3.048 ms** and an average cache hit of **0.005 ms** in the local development environment. These numbers are environment-specific, but the diagnostic headers allow the same comparison to be reproduced on another machine.

The final cache workflow was also verified as:

| Action | Observed Cache State |
|---|---|
| Read after inventory creation | `MISS` |
| Immediate second read | `HIT` |
| Read after inventory deletion | `MISS` |

---

## Development Challenges

### Identity Overriding JWT Authentication

**Problem:** Identity registered its cookie scheme as the default. A valid JWT was created successfully, but protected API routes returned `401` and attempted to use the account login flow.

**Resolution:** JWT bearer was explicitly assigned as the default scheme, authenticate scheme, and challenge scheme after Identity registration. Protected controller routes then authenticated bearer tokens correctly while continuing to use Identity for users, passwords, and roles.

### Circular JSON References

**Problem:** An order contains inventory items, and each item has a navigation property back to its order. Serializing both directions created an object cycle and caused order responses to fail.

**Resolution:** The `InventoryItem.Order` back-reference was marked with `[JsonIgnore]`. Order responses still contain their item collections, but each item no longer attempts to serialize the parent order again.

### Foreign-Key Errors From API Input

**Problem:** Swagger initially exposed the full `InventoryItem` entity for creation, including `OrderId`. Its generated example submitted `OrderId: 0`, which does not reference a valid order and caused a SQLite foreign-key failure.

**Resolution:** `CreateInventoryItemRequest` was introduced to accept only fields that clients are allowed to set. The same boundary was later applied to nested order creation, preventing clients from submitting database-managed IDs or navigation properties.

### Framework and Package Compatibility

**Problem:** EF Core and Identity 10 packages were not compatible with the project's original .NET 9 target, and mixed package versions prevented a coherent build.

**Resolution:** The project was upgraded consistently to .NET 10, EF Core 10, Identity 10, and JWT Bearer 10. The SQLite native bundle was also pinned to a patched version after dependency auditing. The final package audit reported no known vulnerable direct or transitive packages.

---

## Validation Results

The final end-to-end workflow produced the expected outcomes:

| Scenario | Result |
|---|---:|
| Anonymous inventory request | `401` |
| Registration | `201` |
| Valid login | `200` |
| Invalid login | `401` |
| User inventory read | `200` |
| User inventory write | `403` |
| Invalid nested order | `400` |
| Create and retrieve nested order | `201` / `200` |
| User order deletion | `403` |
| Manager inventory creation/deletion | `201` / `204` |
| Manager order deletion | `204` |
| Retrieve deleted order | `404` |

SQLite persistence was verified by creating an order with a nested item, restarting the API, and retrieving the same data afterward. The final project build completed with no warnings or errors, and `dotnet ef database update` confirmed that the database is current.

---

## Run Locally

The project requires the .NET 10 SDK. Configure the development manager with .NET user-secrets:

```powershell
dotnet user-secrets set "SeedManager:Username" "manager"
dotnet user-secrets set "SeedManager:Email" "manager@logitrack.local"
dotnet user-secrets set "SeedManager:Password" "<development-password>"
```

Apply migrations and start the API:

```powershell
dotnet ef database update
dotnet run --launch-profile http
```

Swagger is available in Development at `http://localhost:5071/swagger`.
