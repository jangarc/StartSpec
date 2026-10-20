using Hosts.Base;
using Iam.Application.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Hosts.HQ;

/// <summary>
/// Authorization API surface (task 5.2), mapped 1:1 from the identity/authorization spec delta.
/// Hosts carry identity and format validation only; assignment and evaluation decisions live in
/// Application. Tenant scope resolves server-side from validated claims (design decision 2 — the
/// client header is never trusted alone); an unknown and a foreign-tenant role share ONE rejection
/// shape (no cross-tenant probing oracle).
/// 5.2 授權 API 面：Hosts 只做身分＋格式驗證；判定在 Application；租戶範圍僅自驗證 claims 解析；
/// 未知角色與他租戶角色共用同一拒絕形（不可反推他租戶）。
/// </summary>
public static class AuthorizationEndpoints
{
    public static IEndpointRouteBuilder MapAuthorizationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/iam/roles", async (ProvisionRoleCommand? command, ProvisionRoleUseCase useCase, TenantScope scope) =>
        {
            if (command is null
                || string.IsNullOrWhiteSpace(command.Name)
                || command.Permissions is not { Count: > 0 })
            {
                return Results.Json(new { title = "invalid_provision_request", status = 400 }, statusCode: 400);
            }

            var response = await useCase.HandleAsync(command with { TenantId = scope.ActiveTenantId! });
            return Results.Created($"/iam/roles/{response.RoleId}", new { roleId = response.RoleId });
        });

        endpoints.MapPost("/iam/roles/assignments", async (AssignRoleCommand? command, AssignRoleUseCase useCase, TenantScope scope) =>
        {
            if (command is null
                || command.UserId == Guid.Empty
                || command.RoleId == Guid.Empty)
            {
                return Results.Json(new { title = "invalid_assignment_request", status = 400 }, statusCode: 400);
            }

            try
            {
                var response = await useCase.HandleAsync(command with { TenantId = scope.ActiveTenantId! });
                return Results.Created($"/iam/roles/assignments/{response.AssignmentId}", new { assignmentId = response.AssignmentId });
            }
            catch (RoleUnknownException)
            {
                // Unknown and foreign-tenant roles resolve through the SAME store shape — no oracle.
                return Results.Json(new { title = "invalid_assignment_request", status = 400 }, statusCode: 400);
            }
        });

        endpoints.MapDelete("/iam/roles/assignments/{assignmentId:guid}", async (Guid assignmentId, RevokeRoleUseCase useCase, TenantScope scope) =>
        {
            try
            {
                await useCase.HandleAsync(new RevokeRoleCommand(scope.ActiveTenantId!, assignmentId));
                return Results.NoContent();
            }
            catch (RoleAssignmentUnknownException)
            {
                return Results.Json(new { title = "role_assignment_unknown", status = 404 }, statusCode: 404);
            }
        });

        endpoints.MapPost("/iam/authorization/evaluations", async (AuthorizeCommand? command, AuthorizeUseCase useCase, TenantScope scope) =>
        {
            if (command is null
                || command.UserId == Guid.Empty
                || string.IsNullOrWhiteSpace(command.Permission))
            {
                return Results.Json(new { title = "invalid_evaluation_request", status = 400 }, statusCode: 400);
            }

            var decision = await useCase.HandleAsync(command with { TenantId = scope.ActiveTenantId! });
            return Results.Ok(new { allowed = decision.Allowed });
        });

        return endpoints;
    }
}
