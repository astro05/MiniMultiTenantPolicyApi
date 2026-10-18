# Task: Improve X-Tenant-Id Tenant Context Handling

You are working on the existing repository:


Update the existing implementation to improve how `X-Tenant-Id` is handled while keeping the project simple and appropriate for a 3–4 hour take-home assignment.

Do NOT redesign the application or introduce unnecessary architecture.

---

## 1. Goal

Improve the tenant-context implementation so that:

1. `X-Tenant-Id` is handled in one centralized place.
2. Controllers do not manually parse or validate the tenant header.
3. Services consume `ITenantContext`.
4. Tenant isolation remains enforced directly in EF Core queries.
5. Missing or invalid tenant headers produce a clear `400 Bad Request`.
6. Cross-tenant access continues to return `404 Not Found`.
7. The README clearly explains that `X-Tenant-Id` is a demo mechanism and should be replaced by authenticated identity/claims in production.
8. Existing functionality and tests must continue to work.

---

# 2. Important Architectural Principle

Use this flow:

```text
HTTP Request
    |
    | X-Tenant-Id: 1
    v
TenantContext
    |
    v
ITenantContext
    |
    v
Application Service
    |
    v
EF Core query containing tenant restriction
    |
    v
SQLite
```

The `X-Tenant-Id` header identifies the tenant context for the request.

The header itself is NOT the security boundary.

The actual tenant isolation must continue to be enforced in database queries.

For example:

```csharp
.Where(p =>
    p.CustomerId == customerId &&
    p.Customer.TenantId == tenantId)
```

and:

```csharp
.Where(p =>
    p.Customer.TenantId == tenantId &&
    p.ExpirationDate >= startDate &&
    p.ExpirationDate <= endDate)
```

Do not remove these tenant predicates.

---

# 3. ITenantContext

Keep or introduce:

```csharp
public interface ITenantContext
{
    int TenantId { get; }
}
```

The application services should depend on this interface rather than directly accessing `HttpContext`.

Example:

```csharp
public class CustomerPolicyService : ICustomerPolicyService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public CustomerPolicyService(
        AppDbContext db,
        ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }
}
```

Do not inject `IHttpContextAccessor` into application services.

Only the tenant-context infrastructure should know about HTTP.

---

# 4. TenantContext

Implement or refactor `TenantContext` so it is responsible for reading and parsing:

```text
X-Tenant-Id
```

Example request:

```http
GET /api/customers/101/policy
X-Tenant-Id: 1
```

Rules:

### Missing header

If `X-Tenant-Id` is missing:

```text
400 Bad Request
```

with a clear message such as:

```json
{
  "error": "X-Tenant-Id header is required."
}
```

### Empty header

Return:

```text
400 Bad Request
```

### Non-numeric value

For:

```http
X-Tenant-Id: abc
```

return:

```text
400 Bad Request
```

### Zero or negative value

For:

```http
X-Tenant-Id: 0
```

or:

```http
X-Tenant-Id: -1
```

return:

```text
400 Bad Request
```

The tenant ID must be a positive integer.

Do not silently default to a tenant.

Never do:

```csharp
tenantId = 1;
```

as a fallback.

---

# 5. Exception Handling

If the existing project already has a `TenantHeaderException` or equivalent, reuse/refactor it rather than introducing unnecessary new exception types.

If an exception is used for invalid tenant context, make sure it is translated consistently into:

```text
400 Bad Request
```

Do not duplicate exception handling logic across controllers.

Prefer centralized handling if the existing architecture supports it cleanly.

Do not introduce a large exception-handling framework just for this.

---

# 6. Dependency Injection

Ensure the following are registered:

```csharp
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ITenantContext, TenantContext>();
```

The exact registration can be adapted to the existing project.

Do not register `TenantContext` as singleton because it depends on request-specific HTTP context.

---

# 7. Tenant Existence Validation

If the existing implementation already validates that the tenant exists, preserve it.

If it does not, consider adding lightweight validation so that:

```http
X-Tenant-Id: 999
```

where tenant 999 does not exist is rejected rather than treated as a valid tenant context.

However:

* Do not make `TenantContext` responsible for complex database/business logic.
* Do not introduce unnecessary middleware solely for this.
* If adding tenant existence validation requires significant architectural changes, leave it out and document the assumption.

The primary assignment requirement is tenant isolation, not sophisticated tenant authentication.

---

# 8. Controllers

Controllers should NOT do this:

```csharp
var tenantId = Request.Headers["X-Tenant-Id"];
```

Controllers should not manually parse the tenant ID.

Controllers should only call services.

Example:

```csharp
[HttpGet("{customerId}/policy")]
public async Task<IActionResult> GetPolicy(
    int customerId,
    CancellationToken cancellationToken)
{
    var result = await _customerPolicyService
        .GetCustomerPolicyAsync(customerId, cancellationToken);

    if (result is null)
        return NotFound();

    return Ok(result);
}
```

The service obtains tenant information through:

```csharp
ITenantContext
```

---

# 9. Verify Customer Policy Query

Review the customer policy query carefully.

It MUST enforce both:

```text
CustomerId
AND
TenantId
```

Correct pattern:

```csharp
var tenantId = _tenantContext.TenantId;

var policy = await _db.Policies
    .AsNoTracking()
    .Where(p =>
        p.CustomerId == customerId &&
        p.Customer.TenantId == tenantId)
    .FirstOrDefaultAsync(cancellationToken);
```

Do not fetch the customer/policy first and perform tenant validation afterward.

Do not perform tenant filtering only in memory.

The tenant restriction must be translated into the SQL query.

---

# 10. Verify Expiring Policies Query

Review the expiring policy query.

It MUST filter by tenant as part of the database query.

Correct pattern:

```csharp
var tenantId = _tenantContext.TenantId;

var policies = await _db.Policies
    .AsNoTracking()
    .Where(p =>
        p.Customer.TenantId == tenantId &&
        p.ExpirationDate >= startDate &&
        p.ExpirationDate <= endDate)
    .ToListAsync(cancellationToken);
```

Do not retrieve all policies and then filter by tenant in memory.

---

# 11. Verify Bonus Endpoint

If the existing optional policy expiration endpoint exists, verify that it is also tenant isolated.

For example:

```csharp
var policy = await _db.Policies
    .AsNoTracking()
    .Where(p =>
        p.Id == policyId &&
        p.Customer.TenantId == _tenantContext.TenantId)
    .FirstOrDefaultAsync(cancellationToken);
```

A policy belonging to another tenant must result in:

```text
404 Not Found
```

Do not return the expiration date for another tenant.

---

# 12. Tests

Review and improve the existing tests.

Keep all existing passing tests.

Add tests if necessary.

At minimum, ensure these scenarios are covered.

## Test 1 — Own tenant access

Tenant 1 requests a customer belonging to Tenant 1.

Expected:

```text
Policy returned successfully
```

## Test 2 — Cross-tenant access

Tenant 2 requests a customer belonging to Tenant 1.

Expected:

```text
NotFound / null
```

This is the most important security test.

Suggested name:

```text
GetCustomerPolicy_WhenCustomerBelongsToAnotherTenant_ReturnsNotFound
```

## Test 3 — Expiring policies are tenant scoped

Tenant 1 has an expiring policy.

Tenant 2 also has an expiring policy.

Request as Tenant 1.

Expected:

```text
Only Tenant 1 policy is returned.
```

---

# 13. Add Header-Level Tests Where Practical

If the current test architecture makes it easy, add at least a small number of tests around tenant context parsing.

Test:

```text
Missing X-Tenant-Id → 400
```

and:

```text
X-Tenant-Id: abc → 400
```

However, do not replace the existing service-level tenant-isolation tests.

Ideally the test suite demonstrates both:

```text
Tenant header/context validation
+
Database-level tenant isolation
```

---

# 14. README Update

Update the README to clearly explain the design.

Add a section similar in meaning to:

```text
Tenant Isolation

For this take-home assignment, the current tenant is supplied through
the X-Tenant-Id request header.

The header is parsed once by TenantContext and exposed to application
services through ITenantContext. Controllers do not directly handle
tenant IDs.

Tenant isolation is enforced at the database query level. Every query
that accesses tenant-owned data includes the current tenant ID as part
of its filtering criteria. For example, customer policy lookups require
both the requested customer ID and the customer's tenant ID to match
the current tenant.

Cross-tenant resources are returned as 404 Not Found so the API does
not reveal whether a resource belonging to another tenant exists.

The X-Tenant-Id header is intentionally a simplified mechanism for this
exercise. In production, tenant identity should be derived from an
authenticated identity/authorization claim rather than trusted directly
from a client-controlled header.
```

Adapt the wording to match the actual implementation.

Do not claim that the header itself provides authentication.

---

# 15. Production Explanation

The README should explicitly distinguish:

### Take-home implementation

```text
X-Tenant-Id
    ↓
ITenantContext
    ↓
Tenant-filtered EF Core queries
```

### Production implementation

```text
Authenticated user
    ↓
JWT / identity claims
    ↓
Trusted TenantId claim
    ↓
ITenantContext
    ↓
Tenant-filtered EF Core queries
```

Explain that the tenant ID should not be accepted blindly from a client-controlled header in a real production environment.

Mention possible production improvements:

* Authentication
* Authorization
* Tenant claim validation
* Audit logging
* Database indexes
* Integration/security tests
* Database row-level security as defense in depth
* Centralized tenant filtering where appropriate

Keep this concise.

---

# 16. Do Not Over-Engineer

Do NOT introduce:

* MediatR
* CQRS
* Generic repositories
* Unit of Work
* JWT authentication
* IdentityServer
* OAuth infrastructure
* Microservices
* Docker
* Redis
* message queues
* unnecessary middleware
* complicated tenant frameworks
* global query filters unless there is a very strong reason

This is a small take-home assignment.

The reviewer should be able to understand the tenant isolation implementation within a few minutes.

---

# 17. Security Review

After making the changes, inspect every endpoint that accesses:

* Customers
* Policies

For every database query, ask:

> "Could this query ever return a record belonging to another tenant?"

Verify that the answer is no.

Specifically verify:

```text
GET /api/customers/{customerId}/policy
GET /api/policies/expiring
GET /api/policies/{policyId}/expiration
```

if the bonus endpoint exists.

---

# 18. Regression Verification

Run:

```bash
dotnet restore
dotnet build
dotnet test
```

All tests must pass.

If possible, manually verify:

### Tenant 1 own customer

```http
GET /api/customers/101/policy
X-Tenant-Id: 1
```

Expected:

```text
200 OK
```

### Tenant 2 accessing Tenant 1 customer

```http
GET /api/customers/101/policy
X-Tenant-Id: 2
```

Expected:

```text
404 Not Found
```

### Missing tenant

```http
GET /api/customers/101/policy
```

Expected:

```text
400 Bad Request
```

### Invalid tenant

```http
GET /api/customers/101/policy
X-Tenant-Id: abc
```

Expected:

```text
400 Bad Request
```

---

# 19. Final Review

Before finishing, inspect the git diff and make sure:

* No unnecessary files were added.
* No database runtime files are committed.
* No secrets are committed.
* No existing functionality was accidentally removed.
* Tests cover the security boundary.
* README accurately describes the implementation.
* Code remains simple and readable.

Also check `.gitignore`.

It should ignore SQLite runtime files such as:

```text
*.db
*.db-shm
*.db-wal
```

If database files are already tracked by Git, remove them from tracking while preserving the local database if appropriate.

Do not rewrite unrelated Git history.

---

# 20. Final Output

After implementation, provide a concise summary containing:

1. Files changed.
2. How `X-Tenant-Id` is now handled.
3. How tenant isolation is enforced.
4. Tests added/updated.
5. README changes.
6. Build/test result.
7. Any remaining limitations.

Do not make unrelated changes to the project.
