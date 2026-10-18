namespace Iam.Application.Tenants;

/// <summary>Persisted-state conflict (unique key). Hosts map to 409; use case pre-empts with the typed conflict error.</summary>
public sealed class TenantConflictException(string tenantId, Exception? innerException = null)
    : Exception($"Tenant '{tenantId}' conflicts with persisted state (unique key violation).", innerException)
{
    /// <summary>The conflicting tenant identifier.</summary>
    public string TenantId => tenantId;
}

/// <summary>Use-case level duplicate-provision rejection mapped to a conflict response (spec: rejected with a conflict error, creates nothing).</summary>
public sealed class TenantAlreadyExistsException(string tenantId)
    : Exception($"Tenant '{tenantId}' already exists.")
{
    /// <summary>The rejected tenant identifier.</summary>
    public string TenantId => tenantId;
}
