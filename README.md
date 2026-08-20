# Mini Multi-Tenant Policy API

A small ASP.NET Core Web API (Controllers + EF Core + SQLite) demonstrating secure multi-tenant data isolation for a CRM/administration domain with tenants, customers, and insurance policies.

## Overview

Each request must identify its tenant through the `X-Tenant-Id` header. Every tenant-sensitive EF Core query filters by tenant **in SQL** — a request operating under Tenant A can never retrieve Tenant B's data, even with a valid customer/policy ID. Cross-tenant access returns `404 Not Found` without revealing that the resource exists for another tenant.

## Running locally

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/MiniMultiTenantPolicyApi
```

- The SQLite database (`policy-api.db`) is created and seeded automatically on first start.
- Swagger UI: `http://localhost:5092/swagger` (http profile) or the HTTPS URL in `Properties/launchSettings.json`.
- The `MiniMultiTenantPolicyApi.http` file contains ready-to-run sample requests.

## Endpoints

| Endpoint | Description |
| --- | --- |
| `GET /api/customers/{customerId}/policy` | The customer's policy (tenant-scoped, 404 otherwise) |
| `GET /api/policies/expiring?withinDays=30` | Policies expiring within N days (default 30, must be >= 0) |
| `GET /api/policies/{policyId}/expiration` | Exact stored expiration date |

## Tenant Isolation

The current tenant is supplied through the `X-Tenant-Id` request header.

The header is parsed once by `TenantContext` and exposed to application services through `ITenantContext`. Controllers do not directly handle tenant IDs. A missing, empty, non-numeric, or non-positive header produces a clear `400 Bad Request`, and a tenant ID that does not exist in the database is also rejected with `400 Bad Request`.

Tenant isolation is enforced at the database query level. Every query that accesses tenant-owned data includes the current tenant ID as part of its filtering criteria. For example, customer policy lookups require both the requested customer ID and the customer's tenant ID to match the current tenant.

Cross-tenant resources are returned as `404 Not Found` so the API does not reveal whether a resource belonging to another tenant exists.

The `X-Tenant-Id` header is intentionally a simplified mechanism for this exercise. In production, tenant identity should be derived from an authenticated identity/authorization claim rather than trusted directly from a client-controlled header.

The service layer is covered by unit tests (SQLite in-memory, fixed clock) proving isolation.

### Demo data

| Tenant | Customers | Policies (expiration relative to seed date) |
| --- | --- | --- |
| 1 — Acme Insurance | 101 Alice Johnson, 102 Bob Smith | 1001 (in 10 days), 1002 (in 45 days) |
| 2 — Globex Insurance | 201 Charlie Brown, 202 Diana Miller | 2001 (in 10 days), 2002 (in 60 days) |

### Example requests

```http
GET /api/customers/101/policy
X-Tenant-Id: 1          -> 200 OK, policy 1001

GET /api/customers/101/policy
X-Tenant-Id: 2          -> 404 Not Found (security boundary)

GET /api/policies/expiring?withinDays=30
X-Tenant-Id: 1          -> [1001] only

GET /api/customers/101/policy
                        -> 400 Bad Request (missing header)
```

## Production considerations

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

The tenant ID should not be accepted blindly from a client-controlled header in a real production environment. Instead, tenant identity should come from the authenticated identity. Possible production improvements:

- Authentication and authorization (e.g. OIDC/JWT)
- Tenant claim validation
- Audit logging
- Database indexes for tenant-aware queries
- Integration/security tests
- Database row-level security as defense in depth
- Centralized tenant filtering where appropriate

In a real system, EF Core migrations and proper database infrastructure would also replace `EnsureCreated`, and pagination would be added for large policy lists. Global query filters could centralize tenant filtering, but explicit filters keep the security property visible.
