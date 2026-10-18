namespace Iam.Application.Tenants;

/// <summary>
/// Store-for-scope factory port (Application-owned seam contract). Hosts resolve identity and ask for
/// the store of THAT scope; the platform entry exists because provisioning runs before the new
/// tenant's identity is requestable, and seeder/bootstrap paths share it (G4: sole migrator).
/// 範圍工廠 port：Host 解析身份後取對應範圍之 store；平台入口服務建立路徑（該租戶身份尚不存在）。
/// </summary>
public interface ITenantStoreScope
{
    /// <summary>Provisioning/seed path — no tenant scope binds the platform write (Creation before identity).</summary>
    ITenantStore Platform();

    /// <summary>Member path — construction-level scoped store for the RESOLVED tenant (denied-as-absent everywhere else).</summary>
    ITenantStore Member(string tenantId);
}
