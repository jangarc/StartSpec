using Microsoft.AspNetCore.Http;
using SharedKernel.Ports;

namespace Hosts.Base;

/// <summary>
/// Tenant scope resolution (foundation slice, design decision 2): every /iam path except the two
/// auth bootstrap routes carries a Bearer access token; the active tenant resolves from VALIDATED
/// claims only — a missing or invalid token is ONE uniform 401 before any business logic runs (no
/// oracle between failure kinds). /iam/auth/sign-in and /iam/auth/refresh are anonymous by spec
/// shape (the credentials live in the request body).
/// 範圍中介：除登入／刷新外一律 Bearer；作用中租戶僅自驗證通過的 claims 解析；缺失或無效單一 401。
/// </summary>
public sealed class TenantScopeMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, TenantScope scope, ISessionInspector inspector)
    {
        var path = context.Request.Path;
        if (!path.StartsWithSegments("/iam")
            || path.StartsWithSegments("/iam/auth/sign-in")
            || path.StartsWithSegments("/iam/auth/refresh"))
        {
            await next(context);
            return;
        }

        var header = context.Request.Headers.Authorization.FirstOrDefault();
        var token = header is not null && header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? header["Bearer ".Length..].Trim()
            : null;
        var identity = token is null ? null : inspector.Inspect(token);
        if (identity is null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { title = "tenant_scope_missing", status = 401 });
            return;
        }

        scope.ActiveTenantId = identity.TenantId;
        await next(context);
    }
}
