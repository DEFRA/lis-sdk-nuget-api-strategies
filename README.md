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

Register the framework and factory dependencies in your `IServiceCollection` (e.g. within `Program.cs` or `Startup.cs`):

```csharp
using Defra.Livestock.Sdk.Api.Strategies;

// Register core framework components:
// - IOperatorContext (Scoped)
// - Strategy validators from assembly
services.AddStrategyFramework();

// Or register individual components:
services.AddStrategyOperatorContext();
services.AddStrategyValidators();

// Register Strategy Factories for specific consuming services:
services.AddRepoStrategyFactory<MyService>();
services.AddSoapStrategyFactory<MyService>();
services.AddRestStrategyFactory<MyService>();
services.AddCircuitBreakerStrategyFactory<MyService>();
```

---

## Strategy Factories & Default Configuration

Each factory can be injected into your service and pre-configured with default values (such as loggers, operator context, base URLs, headers, entity descriptions, etc.) that will automatically propagate to all strategy instances created by that factory.

### Common Strategy Factory Methods (`StrategyFactoryBase<TService, TFactory>`)

- `WithDefaultLogger(ILogger<TService> logger)` - Sets the default logger for all strategies built by this factory.
- `WithDefaultOperatorContext(IOperatorContext operatorContext)` - Sets the default operator context for user authorization / auditing.

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

Extension methods extending `HttpClientException` (and derived types `RestRequestException` and `SoapRequestException`):
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
```

Inject and preconfigure factory defaults in your consuming service constructor:
```csharp
public class ExternalLivestockService
{
    private readonly ICircuitBreakerStrategyFactory<ExternalLivestockService> _circuitBreakerFactory;

    public ExternalLivestockService(
        ICircuitBreakerStrategyFactory<ExternalLivestockService> circuitBreakerFactory,
        ILogger<ExternalLivestockService> logger,
        IOperatorContext operatorContext)
    {
        _circuitBreakerFactory = circuitBreakerFactory
            .WithDefaultLogger(logger)
            .WithDefaultOperatorContext(operatorContext)
            .WithDefaultCircuitKey("LivestockExternalApi")
            .WithDefaultConsecutiveFailureThreshold(3)
            .WithDefaultDurationOfBreak(30000) // 30,000 ms (30 seconds)
            .WithDefaultHandledException<HttpRequestException>()
            .WithDefaultHandledException<TimeoutException>();
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
- `WithDefaultSamplingDuration(int samplingDurationMs)` - Sets default sampling time window in milliseconds.
- `WithDefaultMinimumThroughput(int minimumThroughput)` - Sets default minimum request count within sampling window.
- `WithDefaultDurationOfBreak(int durationOfBreakMs)` - Sets default duration in milliseconds the circuit remains open.
- `WithDefaultHandledException<TException>()` - Registers a default exception type treated as failure.
- `WithDefaultHandledException(Func<Exception, bool> predicate)` - Registers a default custom exception predicate treated as failure.
- `WithDefaultHandledException(Func<Exception, CancellationToken, bool> predicate)` - Registers a default custom exception predicate evaluated with the active cancellation token treated as failure.
- `WithDefaultExcludedException<TException>()` - Registers a default exception type explicitly excluded from contributing to circuit failures.
- `WithDefaultExcludedException(Func<Exception, bool> predicate)` - Registers a default custom exception predicate explicitly excluded from contributing to circuit failures.
- `WithDefaultExcludedException(Func<Exception, CancellationToken, bool> predicate)` - Registers a default custom exception predicate evaluated with the active cancellation token explicitly excluded from contributing to circuit failures.
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
- `WithCircuitKey(string circuitKey)` - Identifies the shared circuit breaker across operations. Also sets `TargetDescription` to `$"circuit-breaker-{circuitKey}"`.
- `WithConsecutiveFailureThreshold(int consecutiveFailureThreshold)` - Sets consecutive failure threshold (min `1`).
- `WithFailureRatio(double failureRatio)` - Sets failure ratio threshold (min `0.001`, max `1.0`).
- `WithSamplingDuration(int samplingDurationMs)` - Sets sampling window in milliseconds (min `1`).
- `WithMinimumThroughput(int minimumThroughput)` - Sets minimum throughput before failure ratio is evaluated (min `1`).
- `WithDurationOfBreak(int durationOfBreakMs)` - Sets break duration in milliseconds (min `1`).
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
- **Default Handled Exceptions**: If no `WithHandledException` is configured, **all exceptions are handled by default** (`HandledExceptions.Count == 0`), with the exception of default excluded exceptions and caller cancellation.
- **Default Excluded Exceptions**: The following exceptions are excluded by default and will **never trip the circuit breaker** or execute fallbacks:
  - `StrategyPreConditionException` (and all derived exceptions: `BusinessRuleException`, `ConflictRuleException`, `EntityNotFoundException`, `ExistenceRuleException`, `ReferenceRuleException`, `RequestValidationException`, `SoapSchemaBuilderException`)
  - `ArgumentException` (including `ArgumentNullException` and `ArgumentOutOfRangeException`)
  - `InvalidOperationException`
  - `UnauthorizedAccessException`
  - `XmlSchemaValidationException`
- **Custom Excluded Exceptions**: Additional exceptions configured via `WithExcludedException` (or `WithDefaultExcludedException` on the factory) take strict precedence over handled exceptions. They **never contribute to opening the circuit breaker**, **bypass fallback handling**, and **propagate directly to the caller**.
- **Caller Cancellation Excluded**: If the operation throws `OperationCanceledException` and cancellation was requested by the caller's cancellation token, this is **not treated as a circuit failure** and will not trip the circuit breaker.
- **Handled Exceptions Rethrown Without Fallback**: If an execution encounters a handled exception and **no fallback is configured** (`WithFallback` is omitted), the failure is recorded by Polly to update circuit statistics, logged as a warning, and the exception is **rethrown (`throw;`) directly to the caller**.
- **Fallback Invocation**: When a fallback is provided, handled exceptions and open circuit rejections will invoke the fallback delegate and return its result rather than throwing.
- **Circuit Breaker Open Exception**: When the circuit is `Open` and no fallback is configured, attempting execution throws a `CircuitBreakerOpenException`.

#### Lifecycle Callbacks (`On...` Methods):
The `On...` methods are **optional** lifecycle hooks for observability, metric emission, or alerting:
- **`OnBreak(Action<Exception, TimeSpan> onBreakAction)`**: Invoked when the circuit trips from Closed or Half-Open to **Open**. Passes the triggering `Exception` and the `TimeSpan` break duration.
- **`OnReset(Action onResetAction)`**: Invoked when the circuit successfully recovers and resets to **Closed** (healthy). Normal traffic resumes.
- **`OnHalfOpen(Action onHalfOpenAction)`**: Invoked when the break duration expires and the circuit transitions to **Half-Open**. In this state, Polly allows a single trial probe call through to test downstream health while blocking concurrent requests.

#### Multi-Breaker Behavior:
The builder allows configuring **both** `WithConsecutiveFailureThreshold` and `WithFailureRatio` on the same strategy:
- When both are specified, Polly builds a composite pipeline containing two independent circuit breakers:
  - `{circuitKey}:consecutive` (consecutive failure count breaker)
  - `{circuitKey}:ratio` (sliding window rate-based breaker)
- Each breaker manages its own state machine, counters, and timers independently.
- **Tripping Condition**: The circuit breaks if **either** condition is met (e.g. fast break on 3 consecutive failures **or** if 50% of requests fail over a 60-second window).
- When either breaker opens, calls are blocked and handled according to the fallback or exception rethrow logic.

#### Execution Methods:
- `ExecuteAsync()` - Executes a void / `Task` action within the circuit breaker.
- `ExecuteAsync<TResult>()` - Executes a result-returning `Task<TResult>` action within the circuit breaker.

---

### Examples

#### 1. Void / `Task` Operation with Consecutive Failure Count:
```csharp
await _circuitBreakerFactory.BuildCircuitBreakerStrategy()
    .WithCancellationToken(cancellationToken)
    .WithActionDescription("Delete Animal Record")
    .WithCircuitKey("AnimalDeletionApi")
    .WithConsecutiveFailureThreshold(3)
    .WithDurationOfBreak(30000) // 30 seconds
    .WithHandledException<HttpRequestException>()
    .WithAction(async ct => await _apiClient.DeleteAnimalAsync(animalId, ct))
    .WithFallback(async ct => await _deadLetterQueue.EnqueueDeleteAsync(animalId, ct))
    .ExecuteAsync();
```

#### 2. Generic `Task<TResult>` Operation with Failure Ratio:
```csharp
AnimalDto animal = await _circuitBreakerFactory.BuildCircuitBreakerStrategy()
    .WithCancellationToken(cancellationToken)
    .WithActionDescription("Get Animal Details")
    .WithCircuitKey("AnimalLookupApi")
    .WithFailureRatio(0.5) // Trip if 50% fail
    .WithSamplingDuration(30000) // 30-second sliding window
    .WithMinimumThroughput(10) // Require at least 10 requests in window
    .WithDurationOfBreak(15000) // 15-second break
    .WithHandledException<HttpRequestException>()
    .WithHandledException<TimeoutException>()
    .WithAction(async ct => await _apiClient.GetAnimalAsync(tagNumber, ct))
    .WithFallback(async ct => await _cacheService.GetAnimalAsync(tagNumber, ct))
    .ExecuteAsync<AnimalDto>();
```

#### 3. Multi-Breaker (Consecutive + Ratio-Based) without Fallback (Exceptions Rethrown):
```csharp
// Handled exceptions are rethrown to caller when WithFallback is omitted
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
        .OnBreak((ex, duration) => _alertService.NotifyCircuitOpened("MovementProcessingApi", ex, duration))
        .OnReset(() => _alertService.NotifyCircuitRecovered("MovementProcessingApi"))
        .WithAction(async ct => await _movementClient.ProcessAsync(movementRequest, ct))
        .ExecuteAsync<MovementResult>();
}
catch (CircuitBreakerOpenException ex)
{
    // Thrown when circuit is open and no fallback is configured
    _logger.LogError(ex, "Circuit is currently open. Movement processing blocked.");
    throw;
}
catch (HttpRequestException ex)
{
    // Handled exception rethrown to caller after updating circuit failure metrics
    _logger.LogError(ex, "HTTP call failed.");
    throw;
}
```
