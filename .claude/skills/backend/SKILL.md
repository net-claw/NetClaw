---
name: backend
description: Use when working on the NetClaw backend in `backend/` including ASP.NET Core minimal API endpoints, CQRS handlers/queries, FluentValidation validators, EF Core infra, domain entities, runtime services, and integration tests.
---

# NetClaw Backend

Use this skill for backend changes under `backend/`.

## Stack

- ASP.NET Core minimal APIs on `net10.0`
- Layered architecture: `Api` → `Application` → `Domains` → `Infra`
- **CQRS via Wolverine** (`IMessageBus`) — no MediatR
- EF Core with PostgreSQL in `backend/Src/NetClaw.Infra`
- FluentResults for command return types
- FluentValidation for input validation (auto-discovered by Wolverine)
- Mapster / MapsterMapper for projection and mapping
- xUnit integration tests in `backend/Tests/NetClaw.IntegrationTests`

## Repo Map

```
backend/Src/
├── NetClaw.Api/
│   ├── Program.cs                         # App wiring, endpoint registration
│   └── Endpoints/<Feature>Endpoints.cs    # HTTP endpoint definitions (IEndpoint)
├── NetClaw.Application/
│   └── Features/<Feature>/
│       ├── Handlers/<Command>Handler.cs   # Command handlers (write side)
│       ├── Queries/<Query>Handler.cs      # Query handlers (read side)
│       └── Validators/<Command>Validator.cs
├── NetClaw.Contracts/
│   └── <Feature>/
│       ├── <Command>.cs                   # Command records (input)
│       ├── <Query>.cs                     # Query records (input)
│       ├── <Resource>Request.cs           # HTTP request contracts
│       └── <Resource>Response.cs          # HTTP response contracts
├── NetClaw.Domains/
│   ├── Entities/                          # Aggregates and entities
│   └── Repos/                             # Repository interfaces
└── NetClaw.Infra/
    ├── Contexts/AppDbContext.cs
    ├── Repos/                             # Repository implementations
    └── Migrations/
```

## CQRS Pattern (Wolverine)

### Command Handler
Plain `sealed class` — no interface, no constructor injection. Wolverine injects all dependencies as `Handle` method parameters. Returns `Result<TResponse>` or `Result` (FluentResults).

```csharp
// Contract: backend/Src/NetClaw.Contracts/<Feature>/CreateFoo.cs
public record CreateFoo(string Name, ...);

// Handler: backend/Src/NetClaw.Application/Features/<Feature>/Handlers/CreateFooHandler.cs
public sealed class CreateFooHandler
{
    public async Task<Result<FooResponse>> Handle(
        CreateFoo command,
        IFooRepo repo,
        IMapper mapper,
        CancellationToken ct)
    {
        // validation guard, domain logic, repo.AddAsync + SaveChangesAsync
        return Result.Ok(mapper.Map<FooResponse>(entity))
            .WithSuccess(new Success("Foo created.").WithMetadata("StatusCode", 200));
    }
}
```

### Query Handler
Returns `TResponse?` directly (no `Result` wrapper). Uses Mapster `ProjectToType<T>` for EF projections.

```csharp
// Contract: backend/Src/NetClaw.Contracts/<Feature>/GetFooById.cs
public record GetFooById(Guid FooId);

// Handler: backend/Src/NetClaw.Application/Features/<Feature>/Queries/GetFooByIdHandler.cs
public sealed class GetFooByIdHandler
{
    public async Task<FooResponse?> Handle(
        GetFooById query,
        IFooRepo repo,
        IMapper mapper,
        CancellationToken ct)
    {
        return await repo.Query()
            .AsNoTracking()
            .Where(x => x.Id == query.FooId && x.DeletedAt == null)
            .ProjectToType<FooResponse>(mapper.Config)
            .FirstOrDefaultAsync(ct);
    }
}
```

### Validator
`AbstractValidator<TCommand>` — Wolverine auto-discovers and runs it before the handler.

```csharp
public sealed class CreateFooValidator : AbstractValidator<CreateFoo>
{
    public CreateFooValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}
```

## Endpoint Pattern

Implements `IEndpoint`, registered in `Program.cs`. Dispatches via `IMessageBus.InvokeAsync<T>(command/query)`. Maps results through `ApiResults.Ok/Error` helpers.

```csharp
public sealed class FooEndpoints : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        // Query — null means 404
        group.MapGet("/foos/{fooId:guid}", async (Guid fooId, HttpContext ctx, IMessageBus bus, CancellationToken ct) =>
        {
            var result = await bus.InvokeAsync<FooResponse?>(new GetFooById(fooId), ct);
            return result is null
                ? ApiResults.Error(ctx, StatusCodes.Status404NotFound, "Foo not found.")
                : ApiResults.Ok(ctx, result);
        }).RequireAuthorization();

        // Command — Result<T> mapped through static helper
        group.MapPost("/foos", async (CreateFooRequest req, HttpContext ctx, IMessageBus bus, CancellationToken ct) =>
        {
            var result = await bus.InvokeAsync<Result<FooResponse>>(new CreateFoo(req.Name, ...), ct);
            return FooEndpointMappings.ToApiResult(result, ctx);
        }).RequireAuthorization();
    }
}
```

## Working Rules

1. **Read before editing**: locate the endpoint, contracts, handlers/queries, validator, domain entity, repo, and test before touching anything.
2. **Separation of concerns**: HTTP details stay in `Api`; orchestration/business logic in `Application`; persistence in `Infra`.
3. **CQRS split**: commands (write side) go in `Handlers/`, queries (read side) go in `Queries/`, validators in `Validators/` — all under `Features/<Feature>/`.
4. **Never inject scoped services into singletons** — runtime/background services must be `Singleton` + use `IServiceScopeFactory`.
5. **Soft deletes**: always filter `DeletedAt == null` in queries.
6. **Contracts only over the wire**: never expose EF or domain types to HTTP — use `NetClaw.Contracts` records.

## Typical Change Paths

### New or changed API endpoint

1. Add/update command or query record in `NetClaw.Contracts/<Feature>/`.
2. Add/update `*Handler.cs` (command) or `*Handler.cs` (query) under `Features/<Feature>/`.
3. Add/update validator under `Features/<Feature>/Validators/` if the command takes user input.
4. Update domain entity, repo interface, repo implementation if data model changes.
5. Add/update endpoint mapping in `NetClaw.Api/Endpoints/`.
6. Add/update integration tests.

### Persistence change

1. Update entity in `NetClaw.Domains/Entities/`.
2. Update `AppDbContext.cs`.
3. Update repo interface + implementation.
4. Add EF migration: `dotnet ef migrations add <Name> --project backend/Src/NetClaw.Infra`.

## Validation

```bash
dotnet build backend/backend.sln -v minimal
dotnet test backend/Tests/NetClaw.IntegrationTests/NetClaw.IntegrationTests.csproj --no-restore
dotnet test backend/Tests/NetClaw.IntegrationTests/NetClaw.IntegrationTests.csproj --filter <FeatureName>
```

If full validation is too expensive, state explicitly what was skipped and why.
