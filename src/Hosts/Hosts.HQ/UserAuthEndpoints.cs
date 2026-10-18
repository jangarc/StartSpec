using Iam.Application.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Hosts.HQ;

/// <summary>
/// User-auth API surface (task 4.4), mapped 1:1 from the identity/user-auth spec delta. Hosts carry
/// identity and format validation only (routes/limits); business rules and auth decisions live in
/// the Application use cases. User provisioning is tenant-scoped via the X-Tenant-Id scope; sign-in
/// and refresh carry the tenant in their bodies. ALL credential rejections share one 401 body and
/// ALL refresh rejections share one 401 body — no oracle on which factor failed (no witness leak).
/// 4.4 user-auth API 面：Hosts 只做身分＋格式驗證；使用者建立吃 X-Tenant-Id 範圍；登入與刷新以身分置於本體；
/// 登入各失敗共用同一 401 本體、刷新各失敗共用同一 401 本體（認證失敗不可反推、不得洩漏見證內容）。
/// </summary>
public static class UserAuthEndpoints
{
    public static IEndpointRouteBuilder MapUserAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/iam/users", async ([FromHeader(Name = "X-Tenant-Id")] string? tenantId, ProvisionUserCommand? command, ProvisionUserUseCase useCase) =>
        {
            if (string.IsNullOrWhiteSpace(tenantId)
                || command is null
                || string.IsNullOrWhiteSpace(command.LoginName)
                || string.IsNullOrWhiteSpace(command.DisplayName)
                || string.IsNullOrWhiteSpace(command.TypeCode)
                || string.IsNullOrWhiteSpace(command.Number))
            {
                return Results.Json(new { title = "invalid_provision_request", status = 400 }, statusCode: 400);
            }

            try
            {
                var response = await useCase.HandleAsync(command with { TenantId = tenantId });
                return Results.Created($"/iam/users/{response.UserId}", new { userId = response.UserId, partyId = response.PartyId });
            }
            catch (UserLoginConflictException)
            {
                return Results.Json(new { title = "user_login_conflict", status = 409 }, statusCode: 409);
            }
        });

        endpoints.MapPost("/iam/auth/sign-in", async (SignInCommand? command, SignInUseCase useCase) =>
        {
            if (command is null
                || string.IsNullOrWhiteSpace(command.TenantId)
                || string.IsNullOrWhiteSpace(command.LoginName)
                || string.IsNullOrWhiteSpace(command.Password))
            {
                return Results.Json(new { title = "invalid_signin_request", status = 400 }, statusCode: 400);
            }

            try
            {
                var response = await useCase.HandleAsync(command);
                return Results.Ok(new
                {
                    userId = response.UserId,
                    partyId = response.PartyId,
                    accessToken = response.AccessToken,
                    refreshToken = response.RefreshToken,
                });
            }
            catch (AuthenticationFailedException)
            {
                return Results.Json(new { title = "authentication_failed", status = 401 }, statusCode: 401);
            }
        });

        endpoints.MapPost("/iam/auth/refresh", async (RefreshCommand? command, RefreshSessionUseCase useCase) =>
        {
            if (command is null || string.IsNullOrWhiteSpace(command.RefreshToken))
            {
                return Results.Json(new { title = "invalid_refresh_request", status = 400 }, statusCode: 400);
            }

            try
            {
                var pair = await useCase.HandleAsync(command);
                return Results.Ok(new { accessToken = pair.AccessToken, refreshToken = pair.RefreshToken });
            }
            catch (TokenRefreshRejectedException)
            {
                return Results.Json(new { title = "refresh_rejected", status = 401 }, statusCode: 401);
            }
        });

        return endpoints;
    }
}
