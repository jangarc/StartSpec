using Microsoft.AspNetCore.Http;

namespace Hosts.Base;

/// <summary>
/// Foundation-slice tenant resolution: member paths under /iam/organizations require a resolvable
/// scope; requests without one are rejected BEFORE business logic runs (spec identity/tenant).
/// Interim transport = the HostBase tenant header; the user-auth tasks (4.x) replace it with
/// resolved identity claims behind the same middleware seam.
/// 過渡期傳輸＝HostBase 租戶標頭；4.x 以已解析的身分宣告取代，接縫不變。
/// </summary>
public sealed class TenantScopeMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, TenantScope scope)
    {
        if (!context.Request.Path.StartsWithSegments("/iam/organizations"))
        {
            await next(context);
            return;
        }

        var tenantId = context.Request.Headers[HostBaseComposition.TenantHeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { title = "tenant_scope_missing", status = 401 });
            return;
        }

        scope.ActiveTenantId = tenantId.Trim();
        await next(context);
    }
}
