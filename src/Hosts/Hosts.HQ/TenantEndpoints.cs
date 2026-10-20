using Hosts.Base;
using Iam.Application.Tenants;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Hosts.HQ;

/// <summary>
/// Tenant API surface (task 2.4), mapped 1:1 from the identity-tenant spec delta. Hosts carry identity
/// plus format validation only (routes/limits); business rules and authorization decisions live in
/// Application. Provisioning is the platform entry (no scope); member reads resolve the scope captured
/// by the TenantScopeMiddleware. Unknown and cross-tenant reads share one identical 404 body.
/// 2.4 租戶 API 面：Hosts 只做身分傳遞＋格式驗證；業務規則驗證與判定在 Application。
/// </summary>
public static class TenantEndpoints
{
    private const string TenantConflictTitle = "tenant_conflict";

    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/iam/tenants", async (ProvisionTenantCommand? command, ProvisionTenantUseCase useCase) =>
        {
            if (command is null
                || string.IsNullOrWhiteSpace(command.TenantId)
                || string.IsNullOrWhiteSpace(command.TenantName)
                || string.IsNullOrWhiteSpace(command.InitialOrganizationName))
            {
                return Results.Json(new { title = "invalid_provision_request", status = 400 }, statusCode: 400);
            }

            try
            {
                var response = await useCase.HandleAsync(command);
                return Results.Created(
                    $"/iam/tenants/{Uri.EscapeDataString(command.TenantId)}",
                    new { tenantId = response.TenantId, organizationId = response.OrganizationId });
            }
            catch (Exception exception) when (exception is TenantAlreadyExistsException or TenantConflictException)
            {
                return Results.Json(new { title = TenantConflictTitle, status = 409 }, statusCode: 409);
            }
        });

        endpoints.MapGet("/iam/organizations", async (string? limit, string? cursor, TenantScope scope, ITenantStoreScope storeScope) =>
        {
            if (!int.TryParse(limit, out var parsedLimit) || parsedLimit < 1 || parsedLimit > 200)
            {
                return Results.Json(new { title = "invalid_page_request", status = 400 }, statusCode: 400);
            }

            await using var store = storeScope.Member(scope.ActiveTenantId!);
            var page = await store.ListOrganizationsAsync(scope.ActiveTenantId!, parsedLimit, cursor);

            return Results.Ok(new { items = page.Items, hasNext = page.HasNext });
        });

        endpoints.MapGet("/iam/organizations/{organizationId:guid}", async (Guid organizationId, TenantScope scope, ITenantStoreScope storeScope) =>
        {
            await using var store = storeScope.Member(scope.ActiveTenantId!);
            var entry = await store.FindOrganizationAsync(scope.ActiveTenantId!, organizationId);

            // Unknown AND cross-tenant requests return the exact same body (denied-as-not-found,
            // spec identity/tenant) — no resource shape may leak through the 404.
            return entry is null
                ? Results.Json(new { title = "not_found", status = 404 }, statusCode: 404)
                : Results.Ok(new { organizationId = entry.OrganizationId, name = entry.Name });
        });

        return endpoints;
    }
}
