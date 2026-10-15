# Proposal

## Why

Enterprise platform needs a single trust anchor before store, consumer, and hospital contexts can be built. Without tenant isolation plus unified identity, every downstream Bounded Context would re-invent auth and risk cross-tenant leakage. Establishing IAM foundation first unblocks all vertical slices.

## What Changes

- Introduce tenant + organization model with `tenant_id` row-level isolation as the default for all future modules.
- Introduce user lifecycle (provision, disable) and credential authentication issuing short-lived tokens plus refresh flow.
- Introduce role/permission evaluation (role assignment, permission check API, cross-tenant denial) consumable via integration ports/events.
- Scaffold Modular Monolith composition root (`AppHost`) and `Modules/Iam` + `Modules/SharedKernel` boundaries; no direct DB access across modules.
- Scaffold frontend token-based session handling behind `AuthPort`; no business logic in Vue adapters yet.
- **BREAKING**: N/A (greenfield, `src/` empty, no existing API consumers).

## Capabilities

### New Capabilities

- `identity/tenant`: tenant and organization provisioning, tenant resolution from request, isolation guarantees.
- `identity/user-auth`: user provisioning/lifecycle, credential authentication, session/token issuance and refresh.
- `identity/authorization`: role assignment, permission evaluation, cross-tenant access denial.

### Modified Capabilities

- None (main specs empty).

## Impact

- New .NET 10 solution: `Modules/Iam/{Domain,Application,Infrastructure,Api}`, `SharedKernel`, `AppHost`; EF Core + Npgsql migrations; Reqnroll BDD harness.
- New frontend layers: `domain/`, `application/ports/`, `adapters/vue/`, `theme/tokens.css`; Tailwind maps tokens only.
- New Postgres schema for tenants, users, roles; global query filters required.
- Out of scope: hospital compliance rules, consumer read-scale optimization, microservice extraction, OIDC federation beyond local issuer.
