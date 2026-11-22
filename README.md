# Defra Livestock API SDK (NuGet)
**Root Namespace**: *Defra.Livestock.Sdk.Api*

---

## Strategies

**Namespace**: *Defra.Livestock.Sdk.Api.Strategies*

This package provides fluent strategy builders, factories, and associated components for executing business logic and operations across:
- **Repository Interactions** (Create, Get, GetList, GetPaged, Update, Upsert)
- **SOAP API Interactions** (XML/SOAP payloads, XSD schema validations, transformations)
- **REST API Interactions** (HTTP methods, resource URLs, query parameters, JSON payloads, transformations)
- **Circuit Breaker Resilience** (Consecutive failure counts, failure ratio, sampling window, minimum throughput, half-open probing, fallback)

---

## Service Collection Extensions (Dependency Injection)

Register the framework and factory dependencies in your `IServiceCollection` (e.g. within `Program.cs` or `Startup.cs`).

### Framework Registration

```csharp
using Defra.Livestock.Sdk.Api.Strategies;

// Register core framework components:
// - IOperatorContext (Scoped)
// - Strategy validators from executing assembly
// - ICircuitBreakerPolicyFactory (Transient)
services.AddStrategyFramework();

// Or register individual framework components:
services.AddStrategyOperatorContext();
services.AddStrategyValidators();
services.AddCircuitBreakerPolicyFactory();
```

---

### Strategy Factory Registration Methods & Overloads

Strategy factories can be registered directly with parameterless methods or pre-configured with defaults during DI registration using delegates.

| Method Overload | Description |
|---|---|
| `AddRepoStrategyFactory<TService>()` | Registers `IRepoStrategyFactory<TService>` as transient. |
| `AddRepoStrategyFactory<TService>((factory, context) => ...)` | Registers and pre-configures `IRepoStrategyFactory<TService>` with access to `StrategyFactoryRegistrationContext<TService>`. |
| `AddRepoStrategyFactory<TService, TOptions>((factory, context, options) => ...)` | Registers and pre-configures `IRepoStrategyFactory<TService>` resolving `IOptions<TOptions>`. |
| `AddSoapStrategyFactory<TService>()` | Registers `ISoapStrategyFactory<TService>` and ensures `ISoapHttpClient` is registered. |
| `AddSoapStrategyFactory<TService>((factory, context) => ...)` | Registers and pre-configures `ISoapStrategyFactory<TService>` with access to `StrategyFactoryRegistrationContext<TService>`. |
| `AddSoapStrategyFactory<TService, TOptions>((factory, context, options) => ...)` | Registers and pre-configures `ISoapStrategyFactory<TService>` resolving `IOptions<TOptions>`. |
| `AddRestStrategyFactory<TService>()` | Registers `IRestStrategyFactory<TService>` and ensures `IRestHttpClient` is registered. |
| `AddRestStrategyFactory<TService>((factory, context) => ...)` | Registers and pre-configures `IRestStrategyFactory<TService>` with access to `StrategyFactoryRegistrationContext<TService>`. |
| `AddRestStrategyFactory<TService, TOptions>((factory, context, options) => ...)` | Registers and pre-configures `IRestStrategyFactory<TService>` resolving `IOptions<TOptions>`. |
| `AddCircuitBreakerStrategyFactory<TService>()` | Registers `ICircuitBreakerStrategyFactory<TService>` and ensures `ResiliencePipelineRegistry<string>` is registered. |
| `AddCircuitBreakerStrategyFactory<TService>((factory, context) => ...)` | Registers and pre-configures `ICircuitBreakerStrategyFactory<TService>` with access to `StrategyFactoryRegistrationContext<TService>`. |
| `AddCircuitBreakerStrategyFactory<TService>((factory, context, policyFactory) => ...)` | Registers and pre-configures `ICircuitBreakerStrategyFactory<TService>` with `ICircuitBreakerPolicyFactory` and `StrategyFactoryRegistrationContext<TService>`. |
| `AddCircuitBreakerStrategyFactory<TService, TOptions>((factory, context, policyFactory, options) => ...)` | Registers and pre-configures `ICircuitBreakerStrategyFactory<TService>` with `ICircuitBreakerPolicyFactory`, `StrategyFactoryRegistrationContext<TService>`, and `IOptions<TOptions>`. |

---

### `StrategyFactoryRegistrationContext<TService>`

When using registration overloads with a `configure` delegate, an instance of `StrategyFactoryRegistrationContext<TService>` is passed. It provides access to scoped/transient services resolved from the current service scope:

- **`context.ServiceProvider`** (`IServiceProvider`): The ambient `IServiceProvider` for resolving custom dependencies during factory instantiation.
- **`context.OperatorContext`** (`IOperatorContext`): The registered `IOperatorContext` for passing user authorization and audit identity.
- **`context.Logger`** (`ILogger<TService>`): The strongly-typed logger for `TService`.

---

### DI Registration Usage Examples

#### 1. Parameterless Registration

Use when factories will be configured in service constructors or strategy builders:

```csharp
services.AddRepoStrategyFactory<AnimalService>();
services.AddSoapStrategyFactory<ExternalSoapService>();
services.AddRestStrategyFactory<ExternalRestService>();
services.AddCircuitBreakerStrategyFactory<ExternalRestService>();
```

#### 2. Registration with `StrategyFactoryRegistrationContext`

Use to configure default factory settings (including logger, operator context, base URLs, timeouts, headers) at DI registration time:

```csharp
// REST Factory with registration context
services.AddRestStrategyFactory<MovementService>((factory, context) =>
{
    factory
        .WithDefaultLogger(context.Logger)
        .WithDefaultOperatorContext(context.OperatorContext)
        .WithDefaultBaseUrl("https://api.movements.service.gov.uk")
        .WithDefaultResourceUrl("/api/v1/movements")
        .WithDefaultTimeout(5000)
        .WithDefaultApiKey("api-key-12345");
});

// SOAP Factory with registration context
services.AddSoapStrategyFactory<AnimalVatService>((factory, context) =>
{
    factory
        .WithDefaultLogger(context.Logger)
        .WithDefaultOperatorContext(context.OperatorContext)
        .WithDefaultBaseUrl("https://soap.vat.service.gov.uk")
        .WithDefaultServiceUrl("/ws/vat.asmx")
        .WithDefaultSoapAction("http://tempuri.org/ValidateVat")
        .WithDefaultTimeout(10000);
});

// Repository Factory with registration context
services.AddRepoStrategyFactory<CustomerRepositoryService>((factory, context) =>
{
    factory
        .WithDefaultLogger(context.Logger)
        .WithDefaultOperatorContext(context.OperatorContext)
        .WithDefaultEntityDescription("Customer Record");
});
```

#### 3. Options Pattern Registration (`<TService, TOptions>`)

Use when defaults are defined in `appsettings.json` and bound to strongly-typed options models:

```csharp
// Options class
public class LivestockApiOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutMs { get; set; } = 5000;
}

// Startup / Program.cs
services.Configure<LivestockApiOptions>(configuration.GetSection("LivestockApi"));

services.AddRestStrategyFactory<LivestockService, LivestockApiOptions>((factory, context, options) =>
{
    var settings = options.Value;

    factory
        .WithDefaultLogger(context.Logger)
        .WithDefaultOperatorContext(context.OperatorContext)
        .WithDefaultBaseUrl(settings.BaseUrl)
        .WithDefaultApiKey(settings.ApiKey)
        .WithDefaultTimeout(settings.TimeoutMs);
});
```

#### 4. Circuit Breaker Factory Registration with Policy Factory

Use `ICircuitBreakerPolicyFactory` to apply reusable resilience policies (e.g., HTTP policy) during DI registration:

```csharp
// Direct registration with policy factory
services.AddCircuitBreakerStrategyFactory<PaymentIntegrationService>((factory, context, policyFactory) =>
{
    factory
        .WithDefaultLogger(context.Logger)
        .WithDefaultOperatorContext(context.OperatorContext)
        .WithDefaultCircuitKey("PaymentApi")
        .WithDefaultConsecutiveFailureThreshold(3)
        .WithDefaultDurationOfBreak(30000)
        .WithDefaultPolicy(policyFactory.CreateHttpPolicy());
});

// Registration with policy factory and options
public class ResilienceOptions
{
    public string CircuitKey { get; set; } = "DefaultCircuit";
    public int ConsecutiveFailures { get; set; } = 3;
    public double FailureRatio { get; set; } = 0.5;
    public int BreakDurationSeconds { get; set; } = 30;
}

services.Configure<ResilienceOptions>(configuration.GetSection("Resilience"));

services.AddCircuitBreakerStrategyFactory<PaymentIntegrationService, ResilienceOptions>(
    (factory, context, policyFactory, options) =>
    {
        var settings = options.Value;

        factory
            .WithDefaultLogger(context.Logger)
            .WithDefaultOperatorContext(context.OperatorContext)
            .WithDefaultCircuitKey(settings.CircuitKey)
            .WithDefaultConsecutiveFailureThreshold(settings.ConsecutiveFailures)
            .WithDefaultFailureRatio(settings.FailureRatio)
            .WithDefaultDurationOfBreak(settings.BreakDurationSeconds * 1000)
            .WithDefaultPolicy(policyFactory.CreateHttpPolicy());
    });
```

---

## Strategy Factories & Default Configuration

Each factory can be injected into your service and pre-configured with default values (such as loggers, operator context, base URLs, headers, entity descriptions, etc.) that will automatically propagate to all strategy instances created by that factory.

### Common Strategy Factory Methods (`StrategyFactoryBase<TService, TFactory>`)

- `WithDefaultLogger(ILogger<TService> logger)` - Sets the default logger for all strategies built by this factory.
- `WithDefaultOperatorContext(IOperatorContext operatorContext)` - Sets the default operator context for user authorization / auditing.
- `When(bool condition, Action<TFactory> configure, Action<TFactory>? configureElse = null)` - Conditionally executes configuration on the factory if the condition is `true` (and optional `configureElse` when `false`).
- `When(Func<bool> predicate, Action<TFactory> configure, Action<TFactory>? configureElse = null)` - Conditionally executes configuration on the factory if the predicate evaluates to `true` (and optional `configureElse` when `false`).

### Common HTTP Strategy Factory Methods (`HttpStrategyFactoryBase<TService, TFactory>`)

Shared by `SoapStrategyFactory` and `RestStrategyFactory`:
- `WithDefaultApiDescription(string entityDescription)` - Sets default API description.
- `WithDefaultBaseUrl(string baseUrl)` - Sets default base URL.
- `WithDefaultMediaType(string mediaType)` - Sets default media type.
- `WithDefaultApiKey(string apiKey)` - Sets default API key header (`x-api-key`).
- `WithDefaultApiKey(Func<bool> expression, string apiKey)` - Sets default API key header (`x-api-key`) conditionally if the expression evaluates to `true`.
- `WithDefaultBasicAuth(string username, string password)` - Sets default Basic Authentication credentials.
- `WithDefaultTimeout(int timeoutMs)` - Sets default request timeout in milliseconds (allowed limits: 1ms to 5mins / 1 to 300,000 ms). If not explicitly set, the .NET default timeout is used.
- `WithDefaultVerboseOutput(Action<string, string?> verboseOutputAction)` - Sets default verbose output handler for request/response logging.

---

## 1. Repository Strategies (`IRepoStrategyFactory<TService>`)

### Registering and Injecting Repo Strategy Factory

```csharp
public class AnimalService
{
    private readonly IRepoStrategyFactory<AnimalService> _strategyFactory;
    private readonly IAnimalRepository _repository;

    public AnimalService(
        IRepoStrategyFactory<AnimalService> strategyFactory,
        ILogger<AnimalService> logger,
        IOperatorContext operatorContext,
        IAnimalRepository repository)
    {
        _repository = repository;
        _strategyFactory = strategyFactory
            .WithDefaultLogger(logger)
            .WithDefaultOperatorContext(operatorContext)
            .WithDefaultEntityDescription("Animal");
    }
}
```

### Factory Defaults for Repository Strategies
- `WithDefaultEntityDescription(string entityDescription)` - Sets default entity description used in logging.

### Repository Strategy Builders

The factory provides builder creation methods:
- `BuildCreateStrategy<TEntity>()` -> `ICreateRepoStrategy<TService, TEntity>`
- `BuildGetStrategy<TEntity>()` -> `IGetRepoStrategy<TService, TEntity>`
- `BuildGetListStrategy<TEntity>()` -> `IGetListRepoStrategy<TService, TEntity>`
- `BuildGetPagedStrategy<TEntity>()` -> `IGetPagedRepoStrategy<TService, TEntity>`
- `BuildUpdateStrategy<TEntity>()` -> `IUpdateRepoStrategy<TService, TEntity>`
- `BuildUpsertStrategy<TEntity>()` -> `IUpsertRepoStrategy<TService, TEntity>`

---

### Common Strategy Fluent Methods (`StrategyBase`)

All strategy builders inherit the following fluent configuration methods:

| Method | Description |
|---|---|
| `WithLogger(ILogger<TService> logger)` | Sets the logger for the strategy. |
| `WithCancellationToken(CancellationToken cancellationToken)` | Passes the cancellation token for async operations. |
| `WithOperatorContext(IOperatorContext operatorContext)` | Sets the operator context for user authorization / auditing. |
| `WithRequiresAuthenticatedOperator()` | Enforces that the operator is authenticated before execution. |
| `WithActionDescription(string actionDescription)` | Description of the action (used in structured logging). |
| `WithEntityDescription(string entityDescription)` | *(Repo strategies)* Sets the descriptive name of the entity. |
| `WithBeforeExecute(Func<Task> beforeExecuteAction)` | Callback invoked prior to executing strategy logic. |
| `WithAfterExecute(Func<Task> afterExecuteAction)` | Callback invoked after completing strategy logic. |
| `WithRequestValidation(Func<Task<RequestValidationResult>> validateAction)` | Adds request validation logic that throws a `RequestValidationException` if invalid. |
| `WithRequestValidation(Func<Task<ValidationResult>> validateAction)` | *(StrategyExtensions)* Adds FluentValidation support by executing a FluentValidation validator and automatically mapping results to `RequestValidationResult`. |
| `When(bool condition, Action<TParent> configure, Action<TParent>? configureElse = null)` | Conditionally executes configuration on the builder if the condition is `true` (and optional `configureElse` when `false`). |
| `When(Func<bool> predicate, Action<TParent> configure, Action<TParent>? configureElse = null)` | Conditionally executes configuration on the builder if the predicate evaluates to `true` (and optional `configureElse` when `false`). |

---

### FluentValidation Integration (`StrategyExtensions`)

The package includes `StrategyExtensions` (in namespace `Defra.Livestock.Sdk.Api.Strategies.Operations`) providing first-class integration with [FluentValidation](https://docs.fluentvalidation.net/):

- `WithRequestValidation(Func<Task<ValidationResult>> validateAction)` - Extension method available on all strategies (`IStrategy<TService, TParent>`) accepting a delegate returning FluentValidation's `ValidationResult`.

#### How it works:
1. Executes your FluentValidation validator asynchronously.
2. Automatically maps `FluentValidation.Results.ValidationResult` to internal `RequestValidationResult`.
3. Converts all `ValidationFailure` items to `RequestValidationFailure`, preserving:
   - `PropertyName` and `ErrorMessage`
   - `ErrorCode` and `AttemptedValue`
   - `CustomState` and `FormattedMessagePlaceholderValues`
   - `Severity` mapping (`Severity.Warning` &rarr; `RequestValidationFailureSeverity.Warning`, `Severity.Info` &rarr; `RequestValidationFailureSeverity.Info`, `Severity.Error` &rarr; `RequestValidationFailureSeverity.Error`)
   - `RuleSetsExecuted`
4. If validation fails (`IsValid == false`), execution halts and throws a `RequestValidationException` containing the detailed failure collection before any downstream operation logic runs.

#### Example:
```csharp
using Defra.Livestock.Sdk.Api.Strategies.Operations; // Required for StrategyExtensions
using FluentValidation;

public class CreateAnimalHandler
{
    private readonly IRepoStrategyFactory<CreateAnimalHandler> _strategyFactory;
    private readonly IValidator<CreateAnimalRequest> _validator;
    private readonly IAnimalRepository _repository;

    public CreateAnimalHandler(
        IRepoStrategyFactory<CreateAnimalHandler> strategyFactory,
        IValidator<CreateAnimalRequest> validator,
        IAnimalRepository repository)
    {
        _strategyFactory = strategyFactory;
        _validator = validator;
        _repository = repository;
    }

    public async Task<AnimalDto> HandleAsync(CreateAnimalRequest request, CancellationToken cancellationToken)
    {
        return await _strategyFactory.BuildCreateStrategy<Animal>()
            .WithCancellationToken(cancellationToken)
            .WithEntityDescription("Create Animal")
            .WithRepository(_repository)
            .WithRequestValidation(async () => await _validator.ValidateAsync(request, cancellationToken))
            .WithCreate(() => new Animal { Name = request.Name, TagNumber = request.TagNumber })
            .ExecuteAndMap(animal => new AnimalDto { Id = animal.Id, Name = animal.Name });
    }
}
```

---

### Create Repo Strategy (`ICreateRepoStrategy<TService, TEntity>`)

Used to insert new records into a repository (`IRepoCreatable<TEntity>`).

#### Fluent Methods:
- `WithRepository(IRepoCreatable<TEntity> repository)` - Specifies the repository.
- `WithCreate(Func<TEntity> createAction)` - Specifies the factory function producing the entity to create.
- `WithReferenceRules(Action<IReferenceRulesBuilder<TEntity>> rulesAction)` - Configures reference integrity validation rules prior to creation.
- `WithAfterCreate(Func<TEntity, Task> afterCreateAction)` - Callback executed immediately after entity creation.
- `Execute()` - Executes creation and returns the created `TEntity`.
- `ExecuteAndMap<TResult>(Func<TEntity, TResult> map)` - Executes creation and maps `TEntity` to `TResult`.

#### Examples:

##### Using `ExecuteAndMap` (Returns Mapped Result):
```csharp
var animalDto = await _strategyFactory.BuildCreateStrategy<Animal>()
    .WithCancellationToken(cancellationToken)
    .WithEntityDescription("Create Animal")
    .WithRepository(_repository)
    .WithRequestValidation(async () => await ValidateAsync(request))
    .WithReferenceRules(rules =>
    {
        rules.AddRule(animal => _speciesRepo.Exists(animal.SpeciesId), "Species does not exist");
    })
    .WithCreate(() => new Animal { Name = request.Name, TagNumber = request.TagNumber })
    .WithAfterCreate(async entity => await _auditService.RecordCreationAsync(entity))
    .ExecuteAndMap(entity => new AnimalDto { Id = entity.Id, Name = entity.Name });
```

##### Using `Execute` (Returns Created Entity Directly):
```csharp
Animal createdAnimal = await _strategyFactory.BuildCreateStrategy<Animal>()
    .WithCancellationToken(cancellationToken)
    .WithEntityDescription("Create Animal")
    .WithRepository(_repository)
    .WithCreate(() => new Animal { Name = request.Name, TagNumber = request.TagNumber })
    .Execute();
```

---

### Get Repo Strategy (`IGetRepoStrategy<TService, TEntity>`)

Used to retrieve a single entity from a repository (`IRepoGettable<TEntity>`).

#### Fluent Methods:
- `WithRepository<TRepository>(TRepository repository)` - Specifies the repository implementing `IRepoGettable<TEntity>`.
- `WithRequest(ILoggableById request)` - Associates a request object with an `Id` for structured logging.
- `WithEntityFilter(Expression<Func<TEntity, bool>> entityFilter)` - Filter predicate expression to find the entity.
- `WithExistenceRules(Action<IExistenceRulesBuilder<TEntity>> rulesAction)` - Configures validation rules that verify the entity exists (throws `EntityNotFoundException` on failure).
- `Execute()` - Executes retrieval and returns the `TEntity`.
- `ExecuteAndMap<TResult>(Func<TEntity, TResult> map)` - Executes retrieval and maps `TEntity` to `TResult`.

#### Examples:

##### Using `ExecuteAndMap` (Returns Mapped Result):
```csharp
var animalDto = await _strategyFactory.BuildGetStrategy<Animal>()
    .WithCancellationToken(cancellationToken)
    .WithEntityDescription("Get Animal By Tag")
    .WithRepository(_repository)
    .WithEntityFilter(a => a.TagNumber == request.TagNumber)
    .WithExistenceRules(rules =>
    {
        rules.AddRule(a => a.IsActive, "Animal is inactive or not found");
    })
    .ExecuteAndMap(a => _mapper.Map<AnimalDto>(a));
```

##### Using `Execute` (Returns Entity Directly):
```csharp
Animal animal = await _strategyFactory.BuildGetStrategy<Animal>()
    .WithCancellationToken(cancellationToken)
    .WithEntityDescription("Get Animal By Tag")
    .WithRepository(_repository)
    .WithEntityFilter(a => a.TagNumber == request.TagNumber)
    .Execute();
```

---

### Get List Repo Strategy (`IGetListRepoStrategy<TService, TEntity>`)

Used to query a list of entities from a repository (`IRepoListable<TEntity>`).

#### Fluent Methods:
- `WithRepository<TRepository>(TRepository repository)` - Specifies the repository implementing `IRepoListable<TEntity>`.
- `WithEntityFilter(Expression<Func<TEntity, bool>> entityFilter)` - Filter predicate expression.
- `Execute()` - Executes query and returns `List<TEntity>`.
- `ExecuteAndMap<TResult>(Func<TEntity, TResult> map)` - Executes query and maps each entity in the list to `List<TResult>`.
- `ExecuteAndTransform<TResult>(Func<List<TEntity>, TResult> transform)` - Executes query and transforms the full entity list to `TResult`.

#### Examples:

##### Using `ExecuteAndMap` (Maps Each Entity to a Result List):
```csharp
List<AnimalDto> animalDtos = await _strategyFactory.BuildGetListStrategy<Animal>()
    .WithCancellationToken(cancellationToken)
    .WithEntityDescription("Get Animals By Herd")
    .WithRepository(_repository)
    .WithEntityFilter(a => a.HerdId == herdId)
    .ExecuteAndMap(a => _mapper.Map<AnimalDto>(a));
```

##### Using `Execute` (Returns Entity List Directly):
```csharp
List<Animal> animals = await _strategyFactory.BuildGetListStrategy<Animal>()
    .WithCancellationToken(cancellationToken)
    .WithEntityDescription("Get Animals By Herd")
    .WithRepository(_repository)
    .WithEntityFilter(a => a.HerdId == herdId)
    .Execute();
```

##### Using `ExecuteAndTransform` (Transforms Entire Entity List):
```csharp
HerdSummaryDto summary = await _strategyFactory.BuildGetListStrategy<Animal>()
    .WithCancellationToken(cancellationToken)
    .WithEntityDescription("Get Herd Summary")
    .WithRepository(_repository)
    .WithEntityFilter(a => a.HerdId == herdId)
    .ExecuteAndTransform(entities => new HerdSummaryDto
    {
        HerdId = herdId,
        TotalCount = entities.Count,
        ActiveCount = entities.Count(e => e.IsActive)
    });
```

---

### Get Paged Repo Strategy (`IGetPagedRepoStrategy<TService, TEntity>`)

Used to retrieve paged records from a repository (`IRepoPageable<TEntity>`).

#### Fluent Methods:
- `WithRepository<TRepository>(TRepository repository)` - Specifies the repository implementing `IRepoPageable<TEntity>`.
- `WithRequest<TRequest>(TRequest request)` - Associates request object implementing `IPagedQuery` and `ILoggableById`.
- `WithEntityFilter(Expression<Func<TEntity, bool>> entityFilter)` - Optional filter predicate expression.
- `Execute<TOrderBy>(Expression<Func<TEntity, TOrderBy>> orderBy, bool isAscending = true)` - Executes query and returns `PagedEntities<TEntity>`.
- `ExecuteAndMap<TResult, TOrderBy>(Expression<Func<TEntity, TOrderBy>> orderBy, Func<TEntity, TResult> map, bool isAscending = true)` - Executes query and maps paged items to `PagedResults<TResult>`.
- `ExecuteAndTransform<TResult, TOrderBy>(Expression<Func<TEntity, TOrderBy>> orderBy, Func<PagedEntities<TEntity>, TResult> transform, bool isAscending = true)` - Executes query and transforms `PagedEntities<TEntity>` to `TResult`.

#### Examples:

##### Using `ExecuteAndMap` (Maps Paged Entities to Paged DTOs):
```csharp
PagedResults<AnimalDto> pagedResult = await _strategyFactory.BuildGetPagedStrategy<Animal>()
    .WithCancellationToken(cancellationToken)
    .WithEntityDescription("Get Paged Animals")
    .WithRepository(_repository)
    .WithRequest(request) // request implements IPagedQuery & ILoggableById
    .WithEntityFilter(a => a.IsActive)
    .ExecuteAndMap(a => a.Name, a => _mapper.Map<AnimalDto>(a), isAscending: true);
```

##### Using `Execute` (Returns PagedEntities Directly):
```csharp
PagedEntities<Animal> pagedEntities = await _strategyFactory.BuildGetPagedStrategy<Animal>()
    .WithCancellationToken(cancellationToken)
    .WithEntityDescription("Get Paged Animals")
    .WithRepository(_repository)
    .WithRequest(request)
    .WithEntityFilter(a => a.IsActive)
    .Execute(a => a.Name, isAscending: true);
```

##### Using `ExecuteAndTransform` (Transforms PagedEntities to Custom Model):
```csharp
CustomPagedResponse<AnimalDto> customPagedResponse = await _strategyFactory.BuildGetPagedStrategy<Animal>()
    .WithCancellationToken(cancellationToken)
    .WithEntityDescription("Get Paged Animals")
    .WithRepository(_repository)
    .WithRequest(request)
    .WithEntityFilter(a => a.IsActive)
    .ExecuteAndTransform(a => a.Name, paged => new CustomPagedResponse<AnimalDto>
    {
        Items = paged.Items.Select(e => _mapper.Map<AnimalDto>(e)).ToList(),
        Page = paged.PageNumber,
        Size = paged.PageSize,
        Total = paged.TotalCount
    }, isAscending: true);
```

---

### Update Repo Strategy (`IUpdateRepoStrategy<TService, TEntity>`)

Used to update existing records in a repository (`IRepoGettable<TEntity>` & `IRepoUpdatable<TEntity>`).

#### Fluent Methods:
- `WithRepository<TRepository>(TRepository repository)` - Specifies repository implementing `IRepoGettable<TEntity>` and `IRepoUpdatable<TEntity>`.
- `WithRequest(ILoggableById request)` - Associates a request object with an `Id` for structured logging.
- `WithEntityFilter(Expression<Func<TEntity, bool>> entityFilter)` - Predicate to find target entity.
- `WithExistenceRules(Action<IExistenceRulesBuilder<TEntity>> rulesAction)` - Rules ensuring entity exists (throws `EntityNotFoundException` on failure).
- `WithConflictRules(Action<IConflictRulesBuilder<TEntity>> rulesAction)` - Rules detecting conflicts (throws `EntityConflictException` on failure).
- `WithBusinessRules(Action<IBusinessRulesBuilder<TEntity>> rulesAction)` - Domain/business validation rules (throws `BusinessValidationException` on failure).
- `WithReferenceRules(Action<IReferenceRulesBuilder<TEntity>> rulesAction)` - Reference integrity rules (throws `ReferenceValidationException` on failure).
- `WithBeforeUpdate(Func<TEntity, Task> beforeUpdateAction)` - Callback before updating entity.
- `WithUpdate(Action<TEntity> updateAction)` - Synchronous modification action applied to the entity.
- `WithAfterUpdate(Func<TEntity, Task> afterUpdateAction)` - Callback after updating entity.
- `Execute()` - Executes update and returns updated `TEntity`.
- `ExecuteAndMap<TResult>(Func<TEntity, TResult> map)` - Executes update and maps `TEntity` to `TResult`.

#### Examples:

##### Using `ExecuteAndMap` (Returns Mapped Result):
```csharp
var animalDto = await _strategyFactory.BuildUpdateStrategy<Animal>()
    .WithCancellationToken(cancellationToken)
    .WithEntityDescription("Update Animal Details")
    .WithRepository(_repository)
    .WithRequest(request)
    .WithEntityFilter(a => a.Id == request.Id)
    .WithExistenceRules(rules => rules.AddRule(a => a != null, "Animal not found"))
    .WithConflictRules(rules => rules.AddRule(a => a.RowVersion == request.RowVersion, "Record has changed"))
    .WithBusinessRules(rules => rules.AddRule(a => !a.IsDeceased, "Cannot update deceased animal"))
    .WithUpdate(a =>
    {
        a.Name = request.Name;
        a.Color = request.Color;
    })
    .ExecuteAndMap(a => _mapper.Map<AnimalDto>(a));
```

##### Using `Execute` (Returns Updated Entity Directly):
```csharp
Animal updatedAnimal = await _strategyFactory.BuildUpdateStrategy<Animal>()
    .WithCancellationToken(cancellationToken)
    .WithEntityDescription("Update Animal Details")
    .WithRepository(_repository)
    .WithRequest(request)
    .WithEntityFilter(a => a.Id == request.Id)
    .WithUpdate(a =>
    {
        a.Name = request.Name;
        a.Color = request.Color;
    })
    .Execute();
```

---

### Upsert Repo Strategy (`IUpsertRepoStrategy<TService, TEntity>`)

Used to insert a new entity or update an existing entity in a repository (`IRepoGettable<TEntity>`, `IRepoCreatable<TEntity>`, `IRepoUpdatable<TEntity>`).

#### Fluent Methods:
- `WithRepository<TRepository>(TRepository repository)` - Repository implementing `IRepoGettable<TEntity>`, `IRepoCreatable<TEntity>`, and `IRepoUpdatable<TEntity>`.
- `WithRequest(ILoggableById request)` - Associates a request object with an `Id` for structured logging.
- `WithEntityFilter(Expression<Func<TEntity, bool>> entityFilter)` - Predicate to check if entity already exists.
- `WithCreate(Func<TEntity> createAction)` - Creation action if entity does not exist.
- `WithUpdate(Action<TEntity> updateAction)` - Modification action if entity exists.
- `WithAfterCreate(Func<TEntity, Task> afterCreateAction)` - Callback after creation.
- `WithAfterUpdate(Func<TEntity, Task> afterUpdateAction)` - Callback after update.
- `WithExistenceRules(Action<IExistenceRulesBuilder<TEntity>> rulesAction)` - Existence validation rules.
- `WithConflictRules(Action<IConflictRulesBuilder<TEntity>> rulesAction)` - Conflict check rules.
- `WithBusinessRules(Action<IBusinessRulesBuilder<TEntity>> rulesAction)` - Business validation rules.
- `WithReferenceRules(Action<IReferenceRulesBuilder<TEntity>> rulesAction)` - Reference integrity rules.
- `Execute()` - Executes upsert and returns the created or updated `TEntity`.
- `ExecuteAndMap<TResult>(Func<TEntity, TResult> map)` - Executes upsert and maps result to `TResult`.

#### Examples:

##### Using `ExecuteAndMap` (Returns Mapped Result):
```csharp
var animalDto = await _strategyFactory.BuildUpsertStrategy<Animal>()
    .WithCancellationToken(cancellationToken)
    .WithEntityDescription("Upsert Animal")
    .WithRepository(_repository)
    .WithRequest(request)
    .WithEntityFilter(a => a.TagNumber == request.TagNumber)
    .WithCreate(() => new Animal { TagNumber = request.TagNumber, Name = request.Name })
    .WithUpdate(a => a.Name = request.Name)
    .ExecuteAndMap(a => _mapper.Map<AnimalDto>(a));
```

##### Using `Execute` (Returns Created or Updated Entity Directly):
```csharp
Animal animal = await _strategyFactory.BuildUpsertStrategy<Animal>()
    .WithCancellationToken(cancellationToken)
    .WithEntityDescription("Upsert Animal")
    .WithRepository(_repository)
    .WithRequest(request)
    .WithEntityFilter(a => a.TagNumber == request.TagNumber)
    .WithCreate(() => new Animal { TagNumber = request.TagNumber, Name = request.Name })
    .WithUpdate(a => a.Name = request.Name)
    .Execute();
```

---

## 2. SOAP Strategy (`ISoapStrategyFactory<TService>`)

### Registering and Injecting SOAP Strategy Factory

```csharp
public class ExternalSoapService
{
    private readonly ISoapStrategyFactory<ExternalSoapService> _soapFactory;

    public ExternalSoapService(
        ISoapStrategyFactory<ExternalSoapService> soapFactory,
        ILogger<ExternalSoapService> logger,
        IOperatorContext operatorContext)
    {
        _soapFactory = soapFactory
            .WithDefaultLogger(logger)
            .WithDefaultOperatorContext(operatorContext)
            .WithDefaultApiDescription("External Livestock SOAP Service")
            .WithDefaultBaseUrl("https://soap.service.gov.uk")
            .WithDefaultServiceUrl("/ws/animals.asmx")
            .WithDefaultSoapAction("http://tempuri.org/GetAnimalDetails")
            .WithDefaultMediaType("text/xml")
            .WithDefaultBasicAuth("admin", "secretPassword")
            .WithDefaultXmlDeclaration(false);
    }
}
```

### Factory Defaults for SOAP Strategies
- `WithDefaultApiDescription(string entityDescription)` - Sets default API description for logging.
- `WithDefaultBaseUrl(string baseUrl)` - Sets default base URL.
- `WithDefaultServiceUrl(string serviceUrl)` - Sets default service URL / relative path.
- `WithDefaultSoapAction(string soapAction)` - Sets default SOAP action header value.
- `WithDefaultMediaType(string mediaType)` - Sets default media type (e.g. `text/xml`).
- `WithDefaultApiKey(string apiKey)` - Sets default API key header (`x-api-key`).
- `WithDefaultApiKey(Func<bool> expression, string apiKey)` - Sets default API key header (`x-api-key`) conditionally if the expression evaluates to `true`.
- `WithDefaultBasicAuth(string username, string password)` - Sets default Basic Authentication credentials.
- `WithDefaultTimeout(int timeoutMs)` - Sets default request timeout in milliseconds (allowed limits: 1ms to 5mins / 1 to 300,000 ms). If not explicitly set, the .NET default timeout is used.
- `WithDefaultXmlDeclaration(bool withDefaultXmlDeclaration)` - Sets whether to include the XML declaration.
- `WithDefaultVerboseOutput(Action<string, string?> verboseOutputAction)` - Sets default verbose output handler for payload/response logging.

### SOAP Strategy Fluent Methods (`ISoapStrategy<TService>`)

Created via `_soapFactory.BuildSoapStrategy()`.

#### HTTP & SOAP Configuration:
- `WithBaseUrl(string baseUrl)` - Sets the base URL.
- `WithServiceUrl(string serviceUrl)` - Sets the service URL / relative endpoint.
- `WithSoapAction(string soapAction)` - Sets the `SOAPAction` header.
- `WithApiKey(string apiKey)` - Sets the `x-api-key` header.
- `WithApiKey(Func<bool> expression, string apiKey)` - Sets the `x-api-key` header conditionally if the expression evaluates to `true`.
- `WithBasicAuth(string username, string password)` - Sets Basic Authentication credentials on the request header.
- `WithHeader(string name, string value)` - Adds an HTTP header.
- `WithMediaType(string mediaType)` - Sets the HTTP Content-Type / Media-Type (e.g., `text/xml`).
- `WithTimeout(int timeoutMs)` - Sets the request timeout in milliseconds (allowed limits: 1ms to 5mins / 1 to 300,000 ms). If not explicitly set, the .NET default timeout is used.
- `WithXmlDeclaration(bool includeXmlDeclaration)` - Configures whether to include XML declaration in request body.
- `WithApiDescription(string apiDescription)` - Sets the descriptive API name for logging.
- `WithVerboseOutput(Action<string, string?> verboseOutputAction)` - Registers a callback to receive verbose debug output (raw and transformed request/response XML).

#### Payload Configuration:
- `WithPayload(Func<XElement> payloadAction)` - Sets raw `XElement` SOAP payload factory.
- `WithPayload<TRequest>(TRequest payload)` - Serializes strongly-typed model into XML payload.
- `WithPayload<TRequest>(Func<TRequest> payloadAction)` - Serializes result from a model factory function into XML payload.
- `WithPayloadTransformer(Func<XElement?, XElement?> payloadTransformerAction)` - Delegate to modify / transform XML payload before sending.

#### Validation & Schema Checking:
- `WithSchemas(Action<ISoapSchemaBuilder> builder)` - Registers XSD schemas from file system or embedded assembly resources.
- `WithValidatePreTransformPayloadSchema(string? targetElementName = null)` - Validates payload XML against registered schemas prior to executing payload transformation.
- `WithValidatePayloadSchema(string? targetElementName = null)` - Validates final payload XML against registered schemas prior to sending. Throws `XmlSchemaValidationException` if invalid.

#### Response Transformation & Execution:
- `WithResponseTransformer(Func<XElement?, XElement?> responseTransformerAction)` - Delegate to transform / extract response XML before deserialization.
- `Execute()` - Executes the SOAP request and returns the response body as an `XElement`.
- `Execute<TResult>()` - Executes the request and deserializes the response body XML into `TResult`.
- `ExecuteAndTransform<TResult>(Func<XElement, TResult> transform)` - Transforms the response body `XElement` to `TResult`.
- `ExecuteAndTransform<TResponse, TResult>(Func<TResponse, TResult> transform)` - Deserializes response body to `TResponse` and maps it to `TResult`.
- `ExecuteAndTransform<TResponse, TResult>(Func<TResponse, Func<XElement, TResult>, TResult> transform)` - Deserializes response body to `TResponse` with a fallback XML deserializer function.
- `ExecuteWithoutResponse()` - Executes the request where no response content is expected.

#### Examples:

##### Using `Execute<TResult>` (Deserializes XML Response into Strongly-Typed Result):
```csharp
AnimalStatusResponseDto responseDto = await _soapFactory.BuildSoapStrategy()
    .WithCancellationToken(cancellationToken)
    .WithApiDescription("Query Animal Status")
    .WithBaseUrl("https://soap.service.gov.uk")
    .WithServiceUrl("/ws/animals.asmx")
    .WithSoapAction("http://tempuri.org/GetStatus")
    .WithSchemas(schemas =>
    {
        schemas.Add(Assembly.GetExecutingAssembly(), "MyProject.Schemas.AnimalRequest.xsd");
    })
    .WithValidatePayloadSchema("AnimalStatusRequest")
    .WithPayload(new AnimalStatusRequest { TagNumber = "UK123456" })
    .WithResponseTransformer(xml => xml?.Descendants().FirstOrDefault(e => e.Name.LocalName == "StatusResponse"))
    .Execute<AnimalStatusResponseDto>();
```

##### Using `Execute` (Returns Raw Response XML as `XElement`):
```csharp
XElement responseXml = await _soapFactory.BuildSoapStrategy()
    .WithCancellationToken(cancellationToken)
    .WithApiDescription("Query Raw Animal XML")
    .WithBaseUrl("https://soap.service.gov.uk")
    .WithServiceUrl("/ws/animals.asmx")
    .WithSoapAction("http://tempuri.org/GetRawStatus")
    .WithPayload(new AnimalStatusRequest { TagNumber = "UK123456" })
    .Execute();
```

##### Using `ExecuteAndTransform` with `XElement` (Custom XML Parsing/Extraction):
```csharp
string status = await _soapFactory.BuildSoapStrategy()
    .WithCancellationToken(cancellationToken)
    .WithApiDescription("Query Status String")
    .WithBaseUrl("https://soap.service.gov.uk")
    .WithServiceUrl("/ws/animals.asmx")
    .WithSoapAction("http://tempuri.org/GetStatus")
    .WithPayload(new AnimalStatusRequest { TagNumber = "UK123456" })
    .ExecuteAndTransform(xml => xml.Descendants().First(e => e.Name.LocalName == "StatusCode").Value);
```

##### Using `ExecuteAndTransform` with `TResponse` (Deserializes to Intermediate Model and Maps to DTO):
```csharp
AnimalSummaryDto summary = await _soapFactory.BuildSoapStrategy()
    .WithCancellationToken(cancellationToken)
    .WithApiDescription("Query Animal Summary")
    .WithBaseUrl("https://soap.service.gov.uk")
    .WithServiceUrl("/ws/animals.asmx")
    .WithSoapAction("http://tempuri.org/GetStatus")
    .WithPayload(new AnimalStatusRequest { TagNumber = "UK123456" })
    .ExecuteAndTransform<RawSoapStatusResponse, AnimalSummaryDto>(raw => new AnimalSummaryDto
    {
        TagNumber = raw.Identifier,
        IsActive = raw.State == "ACTIVE"
    });
```

##### Using `ExecuteWithoutResponse` (For One-Way / Void Operations):
```csharp
await _soapFactory.BuildSoapStrategy()
    .WithCancellationToken(cancellationToken)
    .WithApiDescription("Notify Animal Movement")
    .WithBaseUrl("https://soap.service.gov.uk")
    .WithServiceUrl("/ws/animals.asmx")
    .WithSoapAction("http://tempuri.org/NotifyMovement")
    .WithPayload(new MovementNotificationRequest { MovementId = "MOV-999" })
    .ExecuteWithoutResponse();
```

---

## 3. REST Strategy (`IRestStrategyFactory<TService>`)

### Registering and Injecting REST Strategy Factory

```csharp
public class ExternalRestService
{
    private readonly IRestStrategyFactory<ExternalRestService> _restFactory;

    public ExternalRestService(
        IRestStrategyFactory<ExternalRestService> restFactory,
        ILogger<ExternalRestService> logger,
        IOperatorContext operatorContext)
    {
        _restFactory = restFactory
            .WithDefaultLogger(logger)
            .WithDefaultOperatorContext(operatorContext)
            .WithDefaultApiDescription("External Movement REST API")
            .WithDefaultBaseUrl("https://api.service.gov.uk")
            .WithDefaultResourceUrl("/api/v1/movements")
            .WithDefaultMediaType("application/json")
            .WithDefaultBasicAuth("api_user", "api_token")
            .WithDefaultJsonSerializerOptions(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });
    }
}
```

### Factory Defaults for REST Strategies
- `WithDefaultApiDescription(string entityDescription)` - Sets default API description.
- `WithDefaultBaseUrl(string baseUrl)` - Sets default base URL.
- `WithDefaultResourceUrl(string resourceUrl)` - Sets default resource URL / relative path.
- `WithDefaultMediaType(string mediaType)` - Sets default media type (defaults to `application/json`).
- `WithDefaultApiKey(string apiKey)` - Sets default API key header (`x-api-key`).
- `WithDefaultApiKey(Func<bool> expression, string apiKey)` - Sets default API key header (`x-api-key`) conditionally if the expression evaluates to `true`.
- `WithDefaultBasicAuth(string username, string password)` - Sets default Basic Authentication credentials.
- `WithDefaultTimeout(int timeoutMs)` - Sets default request timeout in milliseconds (allowed limits: 1ms to 5mins / 1 to 300,000 ms). If not explicitly set, the .NET default timeout is used.
- `WithDefaultJsonSerializerOptions(JsonSerializerOptions jsonSerializerOptions)` - Sets default JSON serialization options.
- `WithDefaultVerboseOutput(Action<string, string?> verboseOutputAction)` - Sets default verbose output handler for request/response logging.

### REST Strategy Fluent Methods (`IRestStrategy<TService>`)

Created via `_restFactory.BuildRestStrategy()`.

#### HTTP Request Configuration:
- `WithGet()` - Sets HTTP method to `GET`.
- `WithPost()` - Sets HTTP method to `POST`.
- `WithPut()` - Sets HTTP method to `PUT`.
- `WithDelete()` - Sets HTTP method to `DELETE`.
- `WithPatch()` - Sets HTTP method to `PATCH`.
- `WithHead()` - Sets HTTP method to `HEAD`.
- `WithOptions()` - Sets HTTP method to `OPTIONS`.
- `WithBaseUrl(string baseUrl)` - Sets/overrides base URL.
- `WithResourceUrl(string resourceUrl)` - Sets request resource URL / relative path.
- `WithApiKey(string apiKey)` - Sets the `x-api-key` header.
- `WithApiKey(Func<bool> expression, string apiKey)` - Sets the `x-api-key` header conditionally if the expression evaluates to `true`.
- `WithQueryParameter(string name, string value)` - Appends a single query string parameter.
- `WithQueryParameter(Func<bool> expression, string name, string value)` - Appends a query string parameter conditionally if the expression evaluates to `true`.
- `WithQueryParameters(IDictionary<string, string> queryParameters)` - Appends multiple query string parameters.
- `WithBasicAuth(string username, string password)` - Sets Basic Authentication credentials on the request header.
- `WithHeader(string name, string value)` - Adds an HTTP request header.
- `WithMediaType(string mediaType)` - Sets the request media type (defaults to `application/json`).
- `WithTimeout(int timeoutMs)` - Sets the request timeout in milliseconds (allowed limits: 1ms to 5mins / 1 to 300,000 ms). If not explicitly set, the .NET default timeout is used.
- `WithApiDescription(string apiDescription)` - Sets the descriptive API name for logging.
- `WithJsonSerializerOptions(JsonSerializerOptions jsonSerializerOptions)` - Custom JSON serializer options.
- `WithVerboseOutput(Action<string, string?> verboseOutputAction)` - Registers a callback to receive verbose debug output (raw request/response payload).

#### Payload Configuration:
- `WithPayload<TRequest>(TRequest payload)` - Serializes payload object to JSON body.
- `WithPayload<TRequest>(Func<TRequest> payloadAction)` - Serializes payload returned from factory function to JSON body.

#### Execution:
- `Execute<TResult>()` - Sends request and deserializes JSON response body to `TResult`.
- `ExecuteAndTransform<TResponse, TResult>(Func<TResponse, TResult> transform)` - Deserializes response body to `TResponse` and maps to `TResult`.
- `ExecuteWithoutResponse()` - Executes request where no response content is expected (e.g., `204 NoContent`).

#### Examples:

##### Using `Execute<TResult>` (Sends Request and Deserializes JSON Response):
```csharp
MovementResponseDto result = await _restFactory.BuildRestStrategy()
    .WithCancellationToken(cancellationToken)
    .WithApiDescription("Create Movement Record")
    .WithPost()
    .WithResourceUrl("/api/v1/movements")
    .WithQueryParameter("validateOnly", "false")
    .WithQueryParameter(() => includeAuditDetails, "audit", "true")
    .WithBasicAuth("api_user", "api_token")
    .WithHeader("X-Correlation-ID", Guid.NewGuid().ToString())
    .WithPayload(new CreateMovementRequest
    {
        AnimalTag = "UK123456",
        DestinationHolding = "98/765/4321"
    })
    .Execute<MovementResponseDto>();
```

##### Using `ExecuteAndTransform` (Deserializes Intermediate Response and Maps to DTO):
```csharp
MovementSummaryDto summary = await _restFactory.BuildRestStrategy()
    .WithCancellationToken(cancellationToken)
    .WithApiDescription("Get Movement Summary")
    .WithGet()
    .WithResourceUrl("/api/v1/movements/UK123456")
    .ExecuteAndTransform<RawMovementApiResponse, MovementSummaryDto>(raw => new MovementSummaryDto
    {
        Reference = raw.MovementRef,
        IsCompleted = raw.Status == "COMPLETED",
        DepartedAt = raw.Timestamp
    });
```

##### Using `ExecuteWithoutResponse` (For Void / No-Content Operations like DELETE):
```csharp
await _restFactory.BuildRestStrategy()
    .WithCancellationToken(cancellationToken)
    .WithApiDescription("Delete Movement Record")
    .WithDelete()
    .WithResourceUrl("/api/v1/movements/MOV-12345")
    .WithHeader("X-Correlation-ID", Guid.NewGuid().ToString())
    .ExecuteWithoutResponse();
```

### HTTP Client Exception Extensions (`HttpClientExceptionExtensions`)

Extension methods extending `HttpClientException` (the abstract base class for `RestRequestException`, `RestResponseException`, `SoapRequestException`, and `SoapResponseException`):
- `StatusCode()` - Extracts the `HttpStatusCode?` from the inner `HttpRequestException`.
- `IsNotFound()` - Returns `true` if HTTP status is `404 Not Found`.
- `IsBadRequest()` - Returns `true` if HTTP status is `400 Bad Request`.
- `IsUnprocessableEntity()` - Returns `true` if HTTP status is `422 Unprocessable Entity`.
- `IsConflict()` - Returns `true` if HTTP status is `409 Conflict`.
- `IsServiceUnavailable()` - Returns `true` if HTTP status is `503 Service Unavailable`.
- `IsGatewayTimeout()` - Returns `true` if HTTP status is `504 Gateway Timeout`.
- `IsUnauthorized()` - Returns `true` if HTTP status is `401 Unauthorized`.
- `IsForbidden()` - Returns `true` if HTTP status is `403 Forbidden`.
- `IsTooManyRequests()` - Returns `true` if HTTP status is `429 Too Many Requests`.
- `IsInternalServerError()` - Returns `true` if HTTP status is `500 Internal Server Error`.
- `Is400RangeError()` - Returns `true` if HTTP status code is in the 4xx client error range (`400`–`499`).
- `Is500RangeError()` - Returns `true` if HTTP status code is in the 5xx server error range (`500`–`599`).
- `IsNetworkError()` - Returns `true` if the inner exception is an `HttpRequestException` with null or `0` status code (indicating a network or transport failure).

---

## 4. Circuit Breaker Strategy (`ICircuitBreakerStrategyFactory<TService>`)

The Circuit Breaker strategy builder and factory provide resilience protection for external calls (REST, SOAP, databases, message queues, third-party integrations) using Polly v8 resilience pipelines.

### Registering and Injecting Circuit Breaker Strategy Factory

Register the factory via `IServiceCollection`:
```csharp
services.AddCircuitBreakerStrategyFactory<MyService>();
// Or register the full strategy framework including policy factories:
services.AddStrategyFramework();
```

Inject and preconfigure factory defaults in your consuming service constructor:
```csharp
public class ExternalLivestockService
{
    private readonly ICircuitBreakerStrategyFactory<ExternalLivestockService> _circuitBreakerFactory;

    public ExternalLivestockService(
        ICircuitBreakerStrategyFactory<ExternalLivestockService> circuitBreakerFactory,
        ICircuitBreakerPolicyFactory policyFactory,
        ILogger<ExternalLivestockService> logger,
        IOperatorContext operatorContext)
    {
        _circuitBreakerFactory = circuitBreakerFactory
            .WithDefaultLogger(logger)
            .WithDefaultOperatorContext(operatorContext)
            .WithDefaultCircuitKey("LivestockExternalApi")
            .WithDefaultConsecutiveFailureThreshold(3)
            .WithDefaultDurationOfBreak(30000) // 30,000 ms (30 seconds)
            .WithDefaultPolicy(policyFactory.CreateHttpPolicy()); // Apply HTTP resilience policy defaults
    }
}
```

### Shared Circuit Breaker State & `WithDefaultCircuitKey`

Circuit breaker resilience pipelines are managed through a singleton `ResiliencePipelineRegistry<string>` registered in DI, allowing circuit health and failure states to be **shared across requests and threads**.

> **Important**: Setting `WithDefaultCircuitKey(circuitKey)` on the factory assigns that key to all strategies produced by `BuildCircuitBreakerStrategy()`. This puts all subsequent operations created by that factory on the **exact same circuit breaker**. If the circuit trips due to failures in one operation or request, all other operations sharing that circuit key will immediately fast-fail (or execute their configured fallback) without hitting downstream dependencies.

### Factory Defaults for Circuit Breaker Strategies (`ICircuitBreakerStrategyFactory<TService>`)

The factory exposes the following methods to establish default options inherited by all created strategy instances:
- `WithDefaultCircuitKey(string circuitKey)` - Sets default circuit key identifier.
- `WithDefaultConsecutiveFailureThreshold(int consecutiveFailureThreshold)` - Sets default consecutive failure count before tripping.
- `WithDefaultFailureRatio(double failureRatio)` - Sets default failure ratio threshold (`0.001` to `1.0`).
- `WithDefaultSamplingDuration(int samplingDurationMs)` - Sets default sampling time window in milliseconds (strategy defaults to `30,000` ms / 30s if omitted).
- `WithDefaultMinimumThroughput(int minimumThroughput)` - Sets default minimum request count within sampling window (strategy defaults to `2` if omitted).
- `WithDefaultDurationOfBreak(int durationOfBreakMs)` - Sets default duration in milliseconds the circuit remains open (strategy defaults to `30,000` ms / 30s if omitted).
- `WithDefaultHandledException<TException>()` - Registers a default exception type treated as failure.
- `WithDefaultHandledException(Func<Exception, bool> predicate)` - Registers a default custom exception predicate treated as failure.
- `WithDefaultHandledException(Func<Exception, CancellationToken, bool> predicate)` - Registers a default custom exception predicate evaluated with the active cancellation token treated as failure.
- `WithDefaultExcludedException<TException>()` - Registers a default exception type explicitly excluded from contributing to circuit failures.
- `WithDefaultExcludedException(Func<Exception, bool> predicate)` - Registers a default custom exception predicate explicitly excluded from contributing to circuit failures.
- `WithDefaultExcludedException(Func<Exception, CancellationToken, bool> predicate)` - Registers a default custom exception predicate evaluated with the active cancellation token explicitly excluded from contributing to circuit failures.
- `WithDefaultPolicy(ICircuitBreakerPolicy policy)` - Applies a reusable policy (e.g. `CircuitBreakerHttpPolicy` or custom policy) to every strategy created by the factory.
- `WithDefaultOnBreak(Action<Exception, TimeSpan> onBreakAction)` - Sets default callback when circuit opens.
- `WithDefaultOnReset(Action onResetAction)` - Sets default callback when circuit resets to closed.
- `WithDefaultOnHalfOpen(Action onHalfOpenAction)` - Sets default callback when circuit enters half-open state.
- `BuildCircuitBreakerStrategy()` - Creates an `ICircuitBreakerStrategy<TService>` instance pre-populated with factory defaults.

### Minimum and Allowed Parameter Values

When providing values to either the factory defaults or strategy builder methods, the following constraints apply:
- **`ConsecutiveFailureThreshold`**: Minimum value is `1` failure (`ArgumentOutOfRangeException` thrown if `< 1`).
- **`FailureRatio`**: Range must be between `0.001` (0.1%) and `1.0` (100%) (`ArgumentOutOfRangeException` thrown if outside this range).
- **`SamplingDuration` (ms)**: Minimum value is `1` millisecond (`ArgumentOutOfRangeException` thrown if `< 1`). If omitted, defaults to `30,000` ms (30s).
- **`MinimumThroughput`**: Minimum value is `1` request (`ArgumentOutOfRangeException` thrown if `< 1`). If omitted, defaults to `2`.
- **`DurationOfBreak` (ms)**: Minimum value is `1` millisecond (`ArgumentOutOfRangeException` thrown if `< 1`). If omitted, defaults to `30,000` ms (30s).
- **Threshold Requirement**: At least one threshold must be configured (`WithConsecutiveFailureThreshold`, `WithFailureRatio`, or both). If neither is configured, `InvalidOperationException` is thrown upon execution.
- **Circuit Key & CancellationToken Requirement**: Both `CircuitKey` (non-empty string) and `CancellationToken` are mandatory for execution; omitting either throws `InvalidOperationException`.

### Circuit Breaker Strategy Fluent Methods (`ICircuitBreakerStrategy<TService>`)

Created via `_circuitBreakerFactory.BuildCircuitBreakerStrategy()`.

#### Configuration:
- `WithCircuitKey(string circuitKey)` - Identifies the shared circuit breaker across operations. Also sets `TargetDescription` to `$"circuit-breaker-{circuitKey}"` and sets `ActionDescription` to `"Circuit Breaker Scoped Action"` if an action description has not already been provided.
- `WithConsecutiveFailureThreshold(int consecutiveFailureThreshold)` - Sets consecutive failure threshold (min `1`).
- `WithFailureRatio(double failureRatio)` - Sets failure ratio threshold (min `0.001`, max `1.0`).
- `WithSamplingDuration(int samplingDurationMs)` - Sets sampling window in milliseconds (min `1`, defaults to `30,000` ms / 30s).
- `WithMinimumThroughput(int minimumThroughput)` - Sets minimum throughput before failure ratio is evaluated (min `1`, defaults to `2`).
- `WithDurationOfBreak(int durationOfBreakMs)` - Sets break duration in milliseconds (min `1`, defaults to `30,000` ms / 30s).
- `WithPolicy(ICircuitBreakerPolicy policy)` - Applies a reusable policy (e.g. `CircuitBreakerHttpPolicy` or custom policy) to configure handled and excluded rules on the strategy instance.
- `WithHandledException<TException>()` - Specifies an exception type contributing to circuit failures.
- `WithHandledException(Func<Exception, bool> predicate)` - Specifies a predicate for exceptions contributing to failures.
- `WithHandledException(Func<Exception, CancellationToken, bool> predicate)` - Specifies a predicate evaluated with the active cancellation token for exceptions contributing to failures.
- `WithExcludedException<TException>()` - Specifies an exception type explicitly excluded from contributing to circuit failures.
- `WithExcludedException(Func<Exception, bool> predicate)` - Specifies a predicate for exceptions explicitly excluded from contributing to failures.
- `WithExcludedException(Func<Exception, CancellationToken, bool> predicate)` - Specifies a predicate evaluated with the active cancellation token for exceptions explicitly excluded from contributing to failures.
- `WithHandledResult<TResult>(Func<TResult, bool> predicate)` - Specifies a predicate treating returned result models as failures.

#### Protected Actions & Fallbacks:
- `WithAction(Func<CancellationToken, Task> action)` - Configures a void / `Task` async action to protect.
- `WithAction<TResult>(Func<CancellationToken, Task<TResult>> action)` - Configures a result-returning (`Task<TResult>`) async action to protect.
- `WithFallback(Func<CancellationToken, Task> fallbackAction)` - Configures a fallback action for void / `Task` operations.
- `WithFallback<TResult>(Func<CancellationToken, Task<TResult>> fallbackAction)` - Configures a fallback action for `Task<TResult>` operations.

#### Exception Handling & Rethrow Semantics:
- **Default Handled Exceptions**: If no `WithHandledException` or policy is configured, **all exceptions are handled by default** (`HandledExceptions.Count == 0`), with the exception of default excluded exceptions and caller cancellation.
- **Default Excluded Exceptions**: The following exceptions are excluded by default and will **never trip the circuit breaker** or execute fallbacks:
  - `StrategyPreConditionException` (and all derived exceptions: `BusinessRuleException`, `ConflictRuleException`, `EntityNotFoundException`, `ExistenceRuleException`, `ReferenceRuleException`, `RequestValidationException`, `SoapSchemaBuilderException`)
  - `ArgumentException` (including `ArgumentNullException` and `ArgumentOutOfRangeException`)
  - `InvalidOperationException`
  - `UnauthorizedAccessException`
- **Custom Excluded Exceptions**: Additional exceptions configured via `WithExcludedException` (or `WithDefaultExcludedException` on the factory) take strict precedence over handled exceptions. They **never contribute to opening the circuit breaker**, **bypass fallback handling**, and **propagate directly to the caller**.
- **Automatic Caller Cancellation Exclusion**: When an operation throws `OperationCanceledException` (or `TaskCanceledException`) and cancellation was requested by the caller's `CancellationToken` (`cancellationToken.IsCancellationRequested == true`), the circuit breaker **automatically excludes** it. Caller cancellation is treated as an intentional abort rather than a downstream service failure. It **never counts as a failure**, **never contributes to tripping the circuit breaker**, and bypasses fallback logic to propagate the cancellation exception directly to the caller.
- **Handled Exceptions Rethrown Without Fallback**: If an execution encounters a handled exception and **no fallback is configured** (`WithFallback` is omitted), the failure is recorded by Polly to update circuit statistics, logged as a warning, and the exception is **rethrown (`throw;`) directly to the caller**.
- **Fallback Invocation**: When a fallback is provided, handled exceptions and open circuit rejections will invoke the fallback delegate and return its result rather than throwing.
- **Circuit Breaker Open Exception (`CircuitBreakerOpenException`)**: When the circuit is `Open` and no fallback is configured, attempting execution fast-fails and throws `CircuitBreakerOpenException` with message `$"Circuit breaker '{CircuitKey}' is open. Requests are temporarily blocked."`, wrapping Polly's inner `BrokenCircuitException`.

#### Lifecycle Callbacks (`On...` Methods):
The `On...` methods are **optional** lifecycle hooks for observability, metric emission, or alerting:
- **`OnBreak(Action<Exception, TimeSpan> onBreakAction)`**: Invoked when the circuit trips from Closed or Half-Open to **Open**. Passes the triggering `Exception` and the `TimeSpan` break duration.
- **`OnReset(Action onResetAction)`**: Invoked when the circuit successfully recovers and resets to **Closed** (healthy). Normal traffic resumes.
- **`OnHalfOpen(Action onHalfOpenAction)`**: Invoked when the break duration expires and the circuit transitions to **Half-Open**. In this state, Polly allows a single trial probe call through to test downstream health while blocking concurrent requests.

#### Multi-Circuit / Multi-Breaker Approach:
The builder supports combining **both** `WithConsecutiveFailureThreshold` and `WithFailureRatio` on the exact same strategy / circuit key:
- When both thresholds are configured, the pipeline builds a **composite multi-circuit resilience pipeline** containing two distinct, independent circuit breakers:
  - `{circuitKey}:consecutive` (consecutive failure count breaker)
  - `{circuitKey}:ratio` (sliding window rate-based breaker)
- **Independent State Machines**: Each breaker maintains its own counters, timers, and state transitions independently.
- **Tripping Condition (OR Logic)**: The composite circuit trips if **either** breaker opens:
  - Trips immediately if severe sequential failures occur (e.g. 3 consecutive connection drops).
  - Trips if background error rate exceeds the configured ratio within the sampling duration (e.g. 40% failures across 50 requests over 60 seconds).
- **Blocking & Protection**: As soon as either breaker is Open, execution is blocked and diverted to the fallback delegate (or throws `CircuitBreakerOpenException`), protecting the downstream service from overload.

#### Execution Methods:
- `Execute()` - Executes a void / `Task` action within the circuit breaker.
- `Execute<TResult>()` - Executes a result-returning `Task<TResult>` action within the circuit breaker.

### Understanding Circuit Breaker Evaluation: Consecutive Failures vs. Failure Ratio

The Circuit Breaker strategy supports two distinct resilience mechanisms that can be used individually or combined into a composite multi-breaker.

```
                                  ┌────────────────────────────────────────────────────────┐
                                  │                     Incoming Call                      │
                                  └──────────────────────────┬─────────────────────────────┘
                                                             │
                              ┌──────────────────────────────┴──────────────────────────────┐
                              ▼                                                             ▼
                 ┌─────────────────────────┐                                   ┌─────────────────────────┐
                 │   Consecutive Breaker   │                                   │      Ratio Breaker      │
                 │   ({key}:consecutive)   │                                   │      ({key}:ratio)      │
                 └────────────┬────────────┘                                   └────────────┬────────────┘
                              │                                                             │
                  Fails N times in a row?                                  ≥ MinimumThroughput in last 30s?
                              │                                                             │
                        ┌─────┴─────┐                                                 ┌─────┴─────┐
                       Yes          No                                               Yes          No
                        │           │                                                 │           │
                        ▼           ▼                                                 ▼           ▼
                   [TRIP OPEN]  [Stay Closed]                              Failure % ≥ Ratio?  [Stay Closed]
                                                                                      │
                                                                                ┌─────┴─────┐
                                                                               Yes          No
                                                                                │           │
                                                                                ▼           ▼
                                                                           [TRIP OPEN]  [Stay Closed]
```

#### 1. Consecutive Failure Breaker (`WithConsecutiveFailureThreshold`)

- **Purpose:** Protects against immediate, total downstream failures (e.g. network partition, crashed container, hard 500 error).
- **Evaluation:** Counts purely sequential failures. Any successful execution immediately resets the consecutive failure counter back to `0`.
- **Sliding Window:** Does **not** use `SamplingDuration` or `MinimumThroughput`. It solely tracks the streak of failures in a row.

##### Scenario Example (`ConsecutiveFailureThreshold = 3`):

| Call # | Result | Consecutive Failures | Breaker State | Explanation |
| :--- | :--- | :--- | :--- | :--- |
| **1** | ❌ Fail | 1 | **Closed** | 1 of 3 failures reached |
| **2** | ❌ Fail | 2 | **Closed** | 2 of 3 failures reached |
| **3** | ✅ Success | **0** | **Closed** | Success resets consecutive counter to 0 |
| **4** | ❌ Fail | 1 | **Closed** | Counter restarts at 1 |
| **5** | ❌ Fail | 2 | **Closed** | 2 of 3 failures reached |
| **6** | ❌ Fail | 3 | **OPEN** 🚨 | Hit 3 consecutive failures; circuit opens |

#### 2. Failure Ratio Breaker (`WithFailureRatio`, `WithSamplingDuration`, `WithMinimumThroughput`)

- **Purpose:** Protects against high error rates or degraded service health, even when errors are interleaved with successes (e.g. 40% of database queries timing out under heavy load).
- **Rolling Sampling Window (`SamplingDuration`):** Tracks all executions over a trailing, rolling time window (defaults to `30,000` ms / 30s). Executions older than 30 seconds age out and drop from the calculation.
- **Minimum Throughput Gatekeeper (`MinimumThroughput`):** The minimum number of requests (successes + failures) that **must** occur within the rolling window before the failure ratio is evaluated. If throughput is below this number, the ratio breaker will **never trip**, preventing low-volume noise from opening the circuit.
- **Failure Ratio Threshold (`FailureRatio`):** The proportion of failures to total requests required to trip the circuit (e.g. `0.5` = 50%, `0.25` = 25%, `1.0` = 100%).

##### Scenario A: Low Volume / Below Minimum Throughput (`MinimumThroughput = 10`, `FailureRatio = 0.5` in 30s window)

| Call # | Result | Calls in Window | Failures in Window | Failure % | Breaker State | Explanation |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **1** | ❌ Fail | 1 | 1 | 100% | **Closed** | Throughput (1) < 10 (sample too small) |
| **2** | ❌ Fail | 2 | 2 | 100% | **Closed** | Throughput (2) < 10 (sample too small) |
| **3** | ❌ Fail | 3 | 3 | 100% | **Closed** | Throughput (3) < 10 (sample too small) |

*Note: The ratio breaker stays closed because the total throughput in the window (3) is below `MinimumThroughput` (10).*

##### Scenario B: Normal Volume Exceeding Failure Ratio (`MinimumThroughput = 10`, `FailureRatio = 0.5` in 30s window)

| Call # | Result | Calls in Window | Failures in Window | Failure % | Breaker State | Explanation |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **1–8** | 4 ✅ / 4 ❌ | 8 | 4 | 50% | **Closed** | Throughput (8) < 10 |
| **9** | ✅ Success | 9 | 4 | 44.4% | **Closed** | Throughput (9) < 10 |
| **10** | ❌ Fail | 10 | 5 | **50%** | **OPEN** 🚨 | Throughput reached (10) and failure ratio ≥ 50% (5/10) |

##### Scenario C: Rolling Recovery (Failures Aging Out)
If no further calls are made for 30 seconds, all 10 calls drop out of the sliding window, resetting both throughput and failure counts to zero.

#### 3. Composite Multi-Breaker & Common Configuration Pitfalls

When both `WithConsecutiveFailureThreshold` and `WithFailureRatio` are supplied:
- Polly registers two independent circuit breakers (`{key}:consecutive` and `{key}:ratio`).
- If **either** breaker trips, requests are blocked.

##### ⚠️ Common Pitfall: Setting `MinimumThroughput` Too Low
If you configure:
```json
{
  "CircuitBreakerConsecutiveFailureThreshold": "5",
  "CircuitBreakerFailureRatio": "0.5",
  "CircuitBreakerMinimumThroughput": "2",
  "CircuitBreakerSamplingDuration": "30000"
}
```
- On the **2nd consecutive failure**:
  - Consecutive breaker has only 2 of 5 failures (does not trip).
  - But the **ratio breaker** sees 2 requests out of 2 failed ($2/2 = 100\% \ge 50\%$) and throughput ($2 \ge 2$) is met.
  - **Result:** The ratio breaker trips on the 2nd failure, making your consecutive failure threshold of 5 ineffective!

##### ✅ Recommended Best Practice:
1. **Consecutive-Only Resilience:** If you only care about $N$ failures in a row, omit `FailureRatio`.
2. **Composite Resilience:** When using both, set `MinimumThroughput` to a representative traffic volume (e.g. `10` or higher) so small bursts of 2–4 errors are handled by the consecutive threshold while larger error rates over time are caught by the ratio threshold.

---

## 5. Circuit Breaker Policies (`ICircuitBreakerPolicy`, `CircuitBreakerHttpPolicy`, `ICircuitBreakerPolicyFactory`)

Circuit breaker policies encapsulate reusable sets of handled exceptions, excluded exceptions, and result predicates to enforce consistent failure criteria across applications.

### `CircuitBreakerHttpPolicy`

`CircuitBreakerHttpPolicy` provides standard, battle-tested resilience rules tailored for HTTP, REST, and SOAP communications:

#### Handled Failure Conditions:
1. **HTTP Client Exceptions (`HttpClientException`)**:
   `HttpClientException` is the common abstract base exception for all strategy HTTP clients. By pattern matching `ex is HttpClientException`, the policy automatically brings all derived exceptions into scope:
   - `RestRequestException` (REST client request/transport failures)
   - `RestResponseException` (REST client HTTP error responses)
   - `SoapRequestException` (SOAP client request/transport failures)
   - `SoapResponseException` (SOAP client HTTP error responses)

   For any `HttpClientException` (or derived type), the circuit breaker handles:
   - **Network & Transport Errors**: `httpClientException.IsNetworkError()` (e.g., DNS failures, connection drops, socket errors with status code 0 or null).
   - **Rate Limiting**: `httpClientException.IsTooManyRequests()` (HTTP 429 Too Many Requests).
   - **Server Errors**: `httpClientException.Is500RangeError()` (HTTP 500–599 server error status codes).
   - **Timeouts**:
     - `httpClientException.InnerException is TimeoutException`
     - `httpClientException.InnerException is OperationCanceledException { InnerException: TimeoutException }`
     - `httpClientException.InnerException is TaskCanceledException` when the caller's cancellation token was **not** requested (indicating an internal HTTP client request timeout rather than user cancellation).

2. **Polly Timeout Rejections**:
   - `TimeoutRejectedException` (Polly timeout pipeline rejections).

#### Excluded Exceptions:
- **`XmlSchemaValidationException`**: Schema validation errors are client-side payload validation issues and are explicitly excluded so they never contribute to circuit breaker failure counters or trip the circuit.
- **Caller Cancellation**: If `TaskCanceledException` or `OperationCanceledException` occurs because the caller's cancellation token was cancelled (`ct.IsCancellationRequested == true`), it is automatically excluded by both the policy condition and the strategy pipeline, and will not count as a failure or trip the circuit breaker.

---

### `CircuitBreakerPolicyFactory` (`ICircuitBreakerPolicyFactory`)

The policy factory provides a clean mechanism to create pre-configured policies:

#### DI Registration:
```csharp
services.AddCircuitBreakerPolicyFactory();
// Or via the complete strategy framework:
services.AddStrategyFramework();
```

#### Usage with Strategy Factory:
```csharp
public class LivestockApiClient
{
    private readonly ICircuitBreakerStrategyFactory<LivestockApiClient> _strategyFactory;

    public LivestockApiClient(
        ICircuitBreakerStrategyFactory<LivestockApiClient> strategyFactory,
        ICircuitBreakerPolicyFactory policyFactory)
    {
        _strategyFactory = strategyFactory
            .WithDefaultCircuitKey("LivestockApi")
            .WithDefaultConsecutiveFailureThreshold(3)
            .WithDefaultPolicy(policyFactory.CreateHttpPolicy()); // Pre-configure all strategies with HTTP policy
    }
}
```

---

### Creating Custom Circuit Breaker Policies (`ICircuitBreakerPolicy`)

Custom policies allow encapsulating domain-specific failure rules, database error classifications, message queue retry/breaker rules, or third-party SDK exceptions into reusable, testable policy classes.

#### Implementing `ICircuitBreakerPolicy`:
```csharp
using System;
using System.Data.Common;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Resilience.CircuitBreaker;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Resilience.CircuitBreaker.Policies;

public class DatabaseCircuitBreakerPolicy : ICircuitBreakerPolicy
{
    public void Execute<TService>(ICircuitBreakerStrategy<TService> circuitBreakerStrategy)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(circuitBreakerStrategy);

        // Handle database transport / connection exceptions
        circuitBreakerStrategy.WithHandledException<DbException>(ex =>
            ex.IsTransient || ex.Message.Contains("Timeout", StringComparison.OrdinalIgnoreCase));

        circuitBreakerStrategy.WithHandledException<TimeoutException>();

        // Exclude constraint / data integrity errors (they are client/data errors, not infrastructure outages)
        circuitBreakerStrategy.WithExcludedException<InvalidOperationException>();
    }
}
```

#### Applying Custom Policies:
```csharp
// Apply directly on a strategy instance:
await _circuitBreakerFactory.BuildCircuitBreakerStrategy()
    .WithCancellationToken(cancellationToken)
    .WithActionDescription("Query Database")
    .WithCircuitKey("LivestockDb")
    .WithConsecutiveFailureThreshold(3)
    .WithPolicy(new DatabaseCircuitBreakerPolicy())
    .WithAction(async ct => await _repository.GetLivestockRecordsAsync(ct))
    .Execute<List<LivestockRecord>>();

// Or apply as default policy on factory:
_circuitBreakerFactory.WithDefaultPolicy(new DatabaseCircuitBreakerPolicy());
```

---

### Circuit Breaker Exception Extensions (`ExceptionExtensions`)

Extension methods extending `System.Exception` (defined under namespace `System` for direct availability without requiring additional `using` directives) for diagnosing circuit breaker exceptions:
- `IsCircuitBreakerOpen()` - Traverses the exception and its `InnerException` hierarchy to determine if a `CircuitBreakerOpenException` is present at any level. Useful in catch blocks, middleware, error handlers, or filter pipelines where exceptions may be wrapped in higher-level exception types (e.g. `InvalidOperationException`, `AggregateException`, `RpcException`).

---

### Circuit Breaker Examples

#### 1. Void / `Task` Operation with HTTP Policy & Fallback:
```csharp
await _circuitBreakerFactory.BuildCircuitBreakerStrategy()
    .WithCancellationToken(cancellationToken)
    .WithActionDescription("Delete Animal Record")
    .WithCircuitKey("AnimalDeletionApi")
    .WithConsecutiveFailureThreshold(3)
    .WithDurationOfBreak(30000) // 30 seconds
    .WithPolicy(new CircuitBreakerHttpPolicy())
    .WithAction(async ct => await _apiClient.DeleteAnimalAsync(animalId, ct))
    .WithFallback(async ct => await _deadLetterQueue.EnqueueDeleteAsync(animalId, ct))
    .Execute();
```

#### 2. Generic `Task<TResult>` Operation with Failure Ratio & Factory HTTP Policy:
```csharp
// Strategy inherits WithDefaultPolicy(policyFactory.CreateHttpPolicy()) from factory
AnimalDto animal = await _circuitBreakerFactory.BuildCircuitBreakerStrategy()
    .WithCancellationToken(cancellationToken)
    .WithActionDescription("Get Animal Details")
    .WithCircuitKey("AnimalLookupApi")
    .WithFailureRatio(0.5) // Trip if 50% fail
    .WithSamplingDuration(30000) // 30-second sliding window
    .WithMinimumThroughput(10) // Require at least 10 requests in window
    .WithDurationOfBreak(15000) // 15-second break
    .WithAction(async ct => await _apiClient.GetAnimalAsync(tagNumber, ct))
    .WithFallback(async ct => await _cacheService.GetAnimalAsync(tagNumber, ct))
    .Execute<AnimalDto>();
```

#### 3. Multi-Breaker (Consecutive + Ratio-Based) without Fallback & Catching `CircuitBreakerOpenException`:
```csharp
// Handled exceptions and open circuit rejections are rethrown to caller when WithFallback is omitted
try
{
    MovementResult result = await _circuitBreakerFactory.BuildCircuitBreakerStrategy()
        .WithCancellationToken(cancellationToken)
        .WithActionDescription("Process Movement")
        .WithCircuitKey("MovementProcessingApi")
        .WithConsecutiveFailureThreshold(5) // Fast break on 5 consecutive failures
        .WithFailureRatio(0.4) // OR break if 40% fail over 60 seconds
        .WithSamplingDuration(60000)
        .WithMinimumThroughput(20)
        .WithDurationOfBreak(45000)
        .WithPolicy(new CircuitBreakerHttpPolicy())
        .OnBreak((ex, duration) => _alertService.NotifyCircuitOpened("MovementProcessingApi", ex, duration))
        .OnReset(() => _alertService.NotifyCircuitRecovered("MovementProcessingApi"))
        .WithAction(async ct => await _movementClient.ProcessAsync(movementRequest, ct))
        .Execute<MovementResult>();
}
catch (Exception ex) when (ex.IsCircuitBreakerOpen())
{
    // Thrown when circuit is in OPEN state and no fallback is configured
    // IsCircuitBreakerOpen() checks both root exception and any inner exception chain
    _logger.LogError(ex, "Circuit '{CircuitKey}' is open. Fast-failing request.", ex.Message);
    throw;
}
catch (RestRequestException ex)
{
    // Handled exception (derived from HttpClientException) rethrown after recording failure
    _logger.LogError(ex, "REST request failed with network or 5xx error.");
    throw;
}
```
