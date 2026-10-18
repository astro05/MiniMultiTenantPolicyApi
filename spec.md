# Mini Multi-Tenant Policy API — Implementation Specification

## 1. Objective

Build a small, production-minded ASP.NET Core Web API in C# that demonstrates secure multi-tenant data isolation.

The application represents a CRM/administration platform where multiple independent business tenants have customers and insurance policies.

The most important requirement is:

> A request operating under Tenant A must never be able to retrieve Tenant B's customer or policy data, even when the caller knows a valid customer ID or policy ID belonging to Tenant B.

The project is a take-home assignment. Keep the implementation simple and readable. Do not over-engineer it.

---

# 2. Technology Stack

Use:

* .NET 10 stable ASP.NET Core
* ASP.NET Core Web API
* Controllers, not Minimal APIs
* Entity Framework Core
* SQLite
* xUnit for unit tests
* Microsoft.EntityFrameworkCore.Sqlite
* Microsoft.EntityFrameworkCore.InMemory only if genuinely useful for isolated unit tests; prefer SQLite in-memory where relational behavior matters
* Built-in dependency injection
* Nullable reference types enabled
* Swagger/OpenAPI for local API exploration

Do not introduce unnecessary third-party libraries.

Do not build a frontend. UI/styling is not part of the assignment.

---

# 3. Solution Structure

Use a clean but lightweight structure:

```text
MiniMultiTenantPolicyApi/
│
├── MiniMultiTenantPolicyApi.sln
│
├── src/
│   └── MiniMultiTenantPolicyApi/
│       ├── Controllers/
│       │   ├── CustomersController.cs
│       │   └── PoliciesController.cs
│       │
│       ├── Data/
│       │   ├── AppDbContext.cs
│       │   └── DbSeeder.cs
│       │
│       ├── Models/
│       │   ├── Tenant.cs
│       │   ├── Customer.cs
│       │   └── Policy.cs
│       │
│       ├── DTOs/
│       │   ├── PolicyResponseDto.cs
│       │   └── ExpiringPolicyResponseDto.cs
│       │
│       ├── Services/
│       │   ├── ITenantContext.cs
│       │   ├── TenantContext.cs
│       │   ├── ICustomerPolicyService.cs
│       │   ├── CustomerPolicyService.cs
│       │   ├── IPolicyService.cs
│       │   └── PolicyService.cs
│       │
│       ├── Program.cs
│       ├── appsettings.json
│       └── MiniMultiTenantPolicyApi.csproj
│
├── tests/
│   └── MiniMultiTenantPolicyApi.Tests/
│       ├── CustomerPolicyServiceTests.cs
│       ├── PolicyServiceTests.cs
│       └── MiniMultiTenantPolicyApi.Tests.csproj
│
└── README.md
```

The exact folder names may be adjusted slightly if necessary, but preserve the separation between:

* Controllers
* Data access
* Domain models
* DTOs
* Tenant context
* Business/application services
* Tests

---

# 4. Core Design Principles

Follow OOP and SOLID principles without over-engineering.

## Single Responsibility

Each component should have one primary responsibility.

Examples:

* Controller handles HTTP concerns.
* TenantContext determines the current tenant.
* Service contains application/business logic.
* DbContext handles EF Core persistence.
* Seeder creates demo data.

Controllers should NOT contain complicated EF Core queries.

## Dependency Inversion

Controllers should depend on service interfaces:

```csharp
ICustomerPolicyService
IPolicyService
ITenantContext
```

rather than directly implementing business logic.

Register implementations through ASP.NET Core dependency injection.

## Encapsulation

Do not expose EF Core entities unnecessarily through API responses.

Use DTOs for API responses.

## Tenant isolation

Tenant filtering must happen in the data access/service query itself.

Never:

1. Retrieve a customer by ID.
2. Retrieve its tenant.
3. Decide afterward whether the caller is allowed to see it.

Instead, query using both identifiers:

```csharp
customer.Id == customerId &&
customer.TenantId == currentTenantId
```

For policies, ensure the policy belongs to a customer belonging to the current tenant:

```csharp
policy.Customer.TenantId == currentTenantId
```

This should be obvious from the code.

---

# 5. Data Model

Create the following entities.

## Tenant

```csharp
Tenant
{
    int Id
    string Name
    ICollection<Customer> Customers
}
```

Requirements:

* `Id` primary key
* `Name` required
* Maximum reasonable string length, e.g. 200

---

## Customer

```csharp
Customer
{
    int Id
    int TenantId
    string Name
    Tenant Tenant
    ICollection<Policy> Policies
}
```

Requirements:

* `Id` primary key
* `TenantId` required foreign key
* `Name` required
* Customer belongs to exactly one Tenant

Relationship:

```text
Tenant 1 ---- * Customer
```

---

## Policy

```csharp
Policy
{
    int Id
    int CustomerId
    string PolicyNumber
    DateTime ExpirationDate
    decimal PremiumAmount
    Customer Customer
}
```

Requirements:

* `Id` primary key
* `CustomerId` required foreign key
* `PolicyNumber` required
* `ExpirationDate` required
* `PremiumAmount` required
* Policy belongs to exactly one Customer

Relationship:

```text
Customer 1 ---- * Policy
```

---

# 6. EF Core Configuration

Use `AppDbContext : DbContext`.

Configure:

```text
Tenant
  └── Customers
        └── Policies
```

Use proper foreign keys and indexes.

At minimum, add indexes for:

```text
Customer.TenantId
Policy.CustomerId
Policy.ExpirationDate
```

Consider a unique constraint/index for:

```text
TenantId + Name
```

only if useful. Do not complicate the assignment unnecessarily.

Use SQLite.

Connection string should be configurable through `appsettings.json`.

Example:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=policy-api.db"
  }
}
```

---

# 7. Tenant Context

Tenant context is central to the assignment.

Create:

```csharp
public interface ITenantContext
{
    int TenantId { get; }
}
```

Implement:

```csharp
public class TenantContext : ITenantContext
{
    public int TenantId { get; }

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        // Read X-Tenant-Id from the current request.
    }
}
```

The tenant is supplied through an HTTP header:

```http
X-Tenant-Id: 1
```

Example:

```http
GET /api/customers/101/policy
X-Tenant-Id: 1
```

Do NOT treat the customer ID as the tenant identifier.

The tenant context must always come from the current request context.

---

# 8. Tenant Header Rules

Use:

```text
X-Tenant-Id
```

as the tenant header.

Behavior:

### Valid tenant header

```http
X-Tenant-Id: 1
```

The request executes under tenant 1.

### Missing header

Return:

```text
400 Bad Request
```

with a clear error message.

### Invalid/non-numeric header

Return:

```text
400 Bad Request
```

### Tenant ID does not exist

Return:

```text
400 Bad Request
```

or another clearly documented client error.

Prefer validating that the tenant exists.

Do not silently default to a tenant.

Never use:

```csharp
tenantId = 1;
```

as a fallback.

Never allow a missing tenant ID to mean "all tenants".

---

# 9. Endpoint 1 — Customer Policy

Implement:

```http
GET /api/customers/{customerId}/policy
```

Example:

```http
GET /api/customers/101/policy
X-Tenant-Id: 1
```

## Successful response

Return HTTP 200.

Example:

```json
{
  "customerId": 101,
  "policyId": 1001,
  "policyNumber": "POL-ABC-001",
  "expirationDate": "2026-09-15",
  "premiumAmount": 1250.00
}
```

Use a DTO rather than exposing the EF entity directly.

Suggested DTO:

```csharp
public class PolicyResponseDto
{
    public int CustomerId { get; set; }
    public int PolicyId { get; set; }
    public string PolicyNumber { get; set; }
    public DateTime ExpirationDate { get; set; }
    public decimal PremiumAmount { get; set; }
}
```

## Not found behavior

If the customer does not exist:

```text
404 Not Found
```

If the customer exists but belongs to another tenant:

```text
404 Not Found
```

Treat cross-tenant access as not found rather than revealing that another tenant's customer exists.

This is important.

For example:

```text
Tenant 1:
Customer 101

Tenant 2:
Customer 201
```

Tenant 2 requests:

```http
GET /api/customers/101/policy
X-Tenant-Id: 2
```

The API must return:

```text
404 Not Found
```

and must NOT return Customer 101's policy.

The response should also avoid leaking information such as:

```text
"Customer 101 belongs to Tenant 1"
```

---

# 10. Endpoint 2 — Expiring Policies

Implement:

```http
GET /api/policies/expiring?withinDays=30
```

Header:

```http
X-Tenant-Id: 1
```

Meaning:

> Return policies belonging to the current tenant that expire within the requested number of days.

Example:

```http
GET /api/policies/expiring?withinDays=30
X-Tenant-Id: 1
```

## Date calculation

Use the current UTC date/time from a suitable abstraction.

Prefer introducing:

```csharp
IClock
```

or another small abstraction so that date-dependent behavior can be tested deterministically.

For example:

```csharp
public interface IClock
{
    DateTime UtcNow { get; }
}
```

Production implementation:

```csharp
SystemClock
```

Tests can provide a fixed clock.

Avoid making unit tests dependent on the actual current date.

## Filtering rules

A policy should be returned when:

```text
ExpirationDate >= today
AND
ExpirationDate <= today + withinDays
AND
Customer.TenantId == currentTenantId
```

Do not return already expired policies.

Example:

If today is:

```text
2026-08-21
```

and `withinDays=30`, include policies expiring between:

```text
2026-08-21
and
2026-09-20
```

inclusive.

## Invalid withinDays

If `withinDays` is:

```text
negative
```

return:

```text
400 Bad Request
```

You may also impose a reasonable upper bound if desired, but this is not required.

If omitted, default to:

```text
30
```

## Response

Return HTTP 200 with an array.

Example:

```json
[
  {
    "policyId": 1001,
    "customerId": 101,
    "policyNumber": "POL-ABC-001",
    "expirationDate": "2026-09-15",
    "premiumAmount": 1250.00
  }
]
```

Only return policies belonging to the current tenant.

---

# 11. Optional Bonus Endpoint

Implement only if time permits.

Suggested endpoint:

```http
GET /api/policies/{policyId}/expiration
```

Header:

```http
X-Tenant-Id: 1
```

Return the exact expiration date stored in the database.

Example:

```json
{
  "policyId": 1001,
  "policyNumber": "POL-ABC-001",
  "expirationDate": "2026-09-15"
}
```

If the policy does not exist or belongs to another tenant:

```text
404 Not Found
```

The endpoint must never infer, estimate, calculate, or hallucinate an expiration date.

The only valid successful response is the value retrieved from the database.

---

# 12. Demo Data

Create deterministic demo data automatically when the application starts.

Use at least two tenants.

Example:

## Tenant 1

```text
Id: 1
Name: Acme Insurance
```

Customers:

```text
101 - Alice Johnson
102 - Bob Smith
```

Policies:

```text
1001 - Customer 101 - POL-ACME-001
1002 - Customer 102 - POL-ACME-002
```

## Tenant 2

```text
Id: 2
Name: Globex Insurance
```

Customers:

```text
201 - Charlie Brown
202 - Diana Miller
```

Policies:

```text
2001 - Customer 201 - POL-GLOBEX-001
2002 - Customer 202 - POL-GLOBEX-002
```

Use deterministic expiration dates relative to a fixed seed date or ensure the test/application behavior remains predictable.

The seed data must clearly demonstrate that each tenant has separate records.

Do not generate random demo data.

Do not use production-like secrets.

---

# 13. Important Security Requirement

The implementation MUST make tenant filtering part of every tenant-sensitive query.

Correct:

```csharp
var policy = await _db.Policies
    .Where(p =>
        p.CustomerId == customerId &&
        p.Customer.TenantId == tenantId)
    .FirstOrDefaultAsync();
```

Correct:

```csharp
var policies = await _db.Policies
    .Where(p =>
        p.Customer.TenantId == tenantId &&
        p.ExpirationDate >= startDate &&
        p.ExpirationDate <= endDate)
    .ToListAsync();
```

Incorrect:

```csharp
var customer = await _db.Customers
    .FirstOrDefaultAsync(c => c.Id == customerId);

// Check tenant later
```

Incorrect:

```csharp
var policy = await _db.Policies
    .FirstOrDefaultAsync(p => p.CustomerId == customerId);

// Return it without checking tenant
```

Incorrect:

```csharp
var policies = await _db.Policies.ToListAsync();
```

followed by in-memory tenant filtering.

Tenant filtering should happen in SQL through EF Core.

---

# 14. Controllers

Create:

```text
CustomersController
PoliciesController
```

Controllers should be thin.

They should:

1. Read route/query parameters.
2. Obtain tenant context through `ITenantContext`.
3. Call the appropriate application service.
4. Return the appropriate HTTP result.

Controllers should not contain complex EF Core queries.

Example conceptual flow:

```text
HTTP Request
    ↓
Controller
    ↓
Service
    ↓
ITenantContext
    ↓
EF Core query
    ↓
SQLite
```

---

# 15. Services

Create:

```csharp
public interface ICustomerPolicyService
{
    Task<PolicyResponseDto?> GetCustomerPolicyAsync(
        int customerId,
        CancellationToken cancellationToken);
}
```

Implementation:

```csharp
CustomerPolicyService
```

It should use:

```csharp
ITenantContext
AppDbContext
```

Similarly:

```csharp
public interface IPolicyService
{
    Task<IReadOnlyList<ExpiringPolicyResponseDto>> GetExpiringPoliciesAsync(
        int withinDays,
        CancellationToken cancellationToken);
}
```

Implementation:

```csharp
PolicyService
```

This keeps business/application logic out of controllers.

---

# 16. Tests

Use xUnit.

Write at least 3 meaningful tests.

The tests should focus primarily on tenant isolation rather than trivial framework behavior.

## Test 1 — Tenant can access its own customer policy

Arrange:

```text
Tenant 1
Customer 101
Policy 1001
```

Execute as:

```text
TenantId = 1
CustomerId = 101
```

Assert:

```text
Policy is returned
PolicyId == 1001
```

---

## Test 2 — Cross-tenant access is blocked

This is mandatory.

Arrange:

```text
Tenant 1 owns Customer 101.
Tenant 2 exists.
```

Execute:

```text
TenantId = 2
CustomerId = 101
```

Assert:

```text
Result == null / NotFound
```

and verify that no policy belonging to Tenant 1 is returned.

This test must explicitly demonstrate the core security property.

Give the test a descriptive name such as:

```text
GetCustomerPolicy_WhenCustomerBelongsToAnotherTenant_ReturnsNotFound
```

---

## Test 3 — Expiring policies are tenant scoped

Arrange:

```text
Tenant 1:
Policy A expires in 10 days

Tenant 2:
Policy B expires in 10 days
```

Execute:

```text
TenantId = 1
withinDays = 30
```

Assert:

```text
Policy A is returned
Policy B is not returned
```

This proves that the list endpoint also enforces tenant isolation.

---

# 17. Test Infrastructure

Tests must not depend on the developer's existing SQLite database.

Create a fresh test database for each test or test class.

Prefer SQLite in-memory if practical because it behaves more like a relational database than EF Core's InMemory provider.

Ensure:

```text
Tests are isolated
Tests are repeatable
Tests do not depend on test execution order
```

Use a fixed clock for date-sensitive tests.

---

# 18. README

Create a concise README, approximately half a page to one page.

It must include:

## Overview

What the application does.

## Running locally

Include commands such as:

```bash
dotnet restore
dotnet build
dotnet test
dotnet run
```

Explain how to access Swagger.

## Tenant isolation

Explain:

* Tenant is identified by `X-Tenant-Id`.
* Tenant context is injected into services.
* Every tenant-sensitive EF Core query includes tenant filtering.
* Cross-tenant requests return `404 Not Found`.
* The API does not reveal whether another tenant owns the requested resource.

## Production considerations

Explain what would change in a real production system.

Mention at least:

* Tenant ID should come from authenticated identity/claims rather than an arbitrary client-controlled header.
* Strong authentication and authorization should be used.
* Database indexes should support tenant-aware queries.
* Consider database row-level security for stronger defense in depth.
* Add audit logging and security monitoring.
* Add integration/security tests.
* Consider centralized tenant filtering/global query filters carefully.
* Consider pagination for large policy lists.
* Consider proper migrations and production database infrastructure.

Keep this concise.

---

# 19. API Error Handling

Use appropriate HTTP status codes.

Recommended:

```text
200 OK
400 Bad Request
404 Not Found
500 Internal Server Error
```

Do not expose stack traces or database exception details to API clients.

For expected validation failures, return clear messages.

Example:

```json
{
  "error": "X-Tenant-Id header is required."
}
```

For cross-tenant resources, return a generic not-found response:

```text
404 Not Found
```

Do not say:

```text
Customer exists but belongs to another tenant.
```

because that leaks information about another tenant.

---

# 20. Validation

Add basic validation.

Tenant ID:

```text
Required
Positive integer
```

Customer ID:

```text
Positive integer
```

withinDays:

```text
Must be >= 0
```

Policy number:

```text
Required
Reasonable maximum length
```

Names:

```text
Required
Reasonable maximum length
```

Premium:

```text
Decimal
Must not be negative
```

Do not spend excessive time building elaborate validation infrastructure.

---

# 21. Async and Cancellation

Use asynchronous EF Core operations:

```csharp
FirstOrDefaultAsync(...)
ToListAsync(...)
```

Accept:

```csharp
CancellationToken
```

where appropriate.

Do not use synchronous database calls in the API.

---

# 22. Dependency Injection

Register services in `Program.cs`.

Conceptually:

```csharp
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ITenantContext, TenantContext>();

builder.Services.AddScoped<ICustomerPolicyService, CustomerPolicyService>();
builder.Services.AddScoped<IPolicyService, PolicyService>();
```

Register:

```text
AppDbContext
```

with SQLite.

Register:

```text
IClock
```

and its production implementation.

---

# 23. API Documentation

Enable Swagger/OpenAPI.

Document the tenant header:

```text
X-Tenant-Id
```

so a reviewer can easily test both tenants.

Provide example requests in the README.

For example:

```http
GET /api/customers/101/policy
X-Tenant-Id: 1
```

and:

```http
GET /api/customers/101/policy
X-Tenant-Id: 2
```

The second request should demonstrate the security boundary.

Also provide an example:

```http
GET /api/policies/expiring?withinDays=30
X-Tenant-Id: 1
```

---

# 24. Database Initialization

For this take-home assignment, automatically create the SQLite database and seed demo data when running locally.

Use EF Core migrations if practical.

Do not require the evaluator to manually insert data.

The application should work after:

```bash
dotnet run
```

with minimal setup.

Do not seed duplicate data every time the application starts.

Check whether the database is already initialized before seeding.

---

# 25. Expected Manual Verification

After implementation, verify these scenarios manually.

## Scenario A — Tenant 1 own data

```http
GET /api/customers/101/policy
X-Tenant-Id: 1
```

Expected:

```text
200 OK
```

with Tenant 1's policy.

## Scenario B — Tenant 2 attempts to access Tenant 1 data

```http
GET /api/customers/101/policy
X-Tenant-Id: 2
```

Expected:

```text
404 Not Found
```

No Tenant 1 information should be returned.

## Scenario C — Tenant 1 expiring policies

```http
GET /api/policies/expiring?withinDays=30
X-Tenant-Id: 1
```

Expected:

Only Tenant 1 policies.

## Scenario D — Tenant 2 expiring policies

```http
GET /api/policies/expiring?withinDays=30
X-Tenant-Id: 2
```

Expected:

Only Tenant 2 policies.

## Scenario E — Missing tenant

```http
GET /api/customers/101/policy
```

Expected:

```text
400 Bad Request
```

---

# 26. Definition of Done

The project is complete only when all of the following are true:

* [ ] ASP.NET Core Web API builds successfully.
* [ ] SQLite database works locally.
* [ ] Tenant, Customer, and Policy entities exist.
* [ ] EF Core relationships are correctly configured.
* [ ] Deterministic demo data is automatically seeded.
* [ ] Controllers are used.
* [ ] Tenant context is obtained from `X-Tenant-Id`.
* [ ] Missing/invalid tenant context is rejected.
* [ ] `GET /api/customers/{customerId}/policy` works.
* [ ] Customer policy endpoint is tenant isolated.
* [ ] `GET /api/policies/expiring?withinDays=N` works.
* [ ] Expiring-policy endpoint is tenant isolated.
* [ ] Cross-tenant access returns 404.
* [ ] At least 3 automated tests exist.
* [ ] One test explicitly proves cross-tenant access is blocked.
* [ ] One test proves expiring policies are tenant scoped.
* [ ] Tests are deterministic and independent.
* [ ] README explains tenant isolation.
* [ ] README explains production improvements.
* [ ] Swagger works.
* [ ] `dotnet build` succeeds.
* [ ] `dotnet test` succeeds.
* [ ] No secrets are committed.
* [ ] No unnecessary architecture or dependencies are introduced.

---

# 27. Implementation Priorities

If time becomes limited, implement in this order:

1. Domain models
2. EF Core + SQLite
3. Demo seed data
4. TenantContext
5. Customer policy endpoint
6. Expiring policy endpoint
7. Cross-tenant tests
8. Remaining tests
9. README
10. Optional bonus endpoint
11. Minor polish

Do not sacrifice tenant isolation or tests for extra features.

---

# 28. Important Guidance for OpenCode

Implement the project directly from this specification.

Do not add:

* Authentication providers
* JWT infrastructure
* Frontend applications
* CQRS/MediatR
* Generic repository frameworks
* Event sourcing
* Microservices
* Docker unless necessary
* Complex dependency injection frameworks
* Unnecessary abstractions
* Large logging frameworks
* AI/LLM functionality

The goal is a small, understandable take-home project.

Prefer straightforward code over clever code.

The reviewer should be able to open the project and quickly answer:

1. Where does the tenant ID come from?
2. Where is tenant isolation enforced?
3. Can Tenant A retrieve Tenant B's customer?
4. Are there tests proving this?
5. Is the code easy to understand?

The answer to questions 2–4 must be obvious from the implementation and tests.

Before finishing, run:

```bash
dotnet build
dotnet test
```

Fix all compilation errors and failing tests.

Finally, provide a concise summary of:

* Architecture
* Tenant isolation approach
* Endpoints
* Tests
* How to run the project
* Any assumptions or trade-offs
* Whether the optional bonus endpoint was implemented
