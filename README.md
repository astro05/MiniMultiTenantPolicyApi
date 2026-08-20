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

## Tenant isolation

- The tenant is identified by the `X-Tenant-Id` header (required, positive integer, must exist — otherwise `400 Bad Request`).
- A scoped `ITenantContext` reads the header from the current request; controllers and services never default to a tenant.
- Tenant filtering is part of every query, e.g.
  `p.CustomerId == customerId && p.Customer.TenantId == tenantId`, translated to SQL by EF Core.
- Cross-tenant requests return `404 Not Found` and never reveal whether another tenant owns the resource.
- The service layer is covered by unit tests (SQLite in-memory, fixed clock) proving isolation.

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

In a real system:

- The tenant ID would come from the authenticated identity (e.g. a claim), not an arbitrary client-controlled header.
- Strong authentication/authorization (OIDC/JWT), audit logging, and security monitoring would be added.
- Database indexes already support tenant-aware queries (`Customer.TenantId`, `Policy.CustomerId`, `Policy.ExpirationDate`); consider row-level security for defense in depth.
- EF Core migrations and proper database infrastructure would replace `EnsureCreated`.
- Global query filters could centralize tenant filtering, but explicit filters keep the security property visible.
- Pagination for large policy lists, and integration/security tests (e.g. `WebApplicationFactory`) would be added.
