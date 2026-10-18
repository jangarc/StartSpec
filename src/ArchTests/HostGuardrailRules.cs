using Hosts.Base;
using NetArchTest.Rules;
using System.Reflection;
using Xunit;

namespace ArchTests;

/// <summary>
/// Host guardrails (ADR-001/002 + design D1' G1–G4), per platform spec "Hosts compose per org unit":
/// 1) Hosts reference Application ports + DTOs only — never Iam.Domain; never Iam.Infrastructure except the
///    sanctioned composition entries and the Migrator's migration wiring (whitelisted explicitly when they land);
/// 2) one canonical exposure per use case — hosts must not reference each other;
/// 3) cross-cutting lives only in <c>HostBase</c>.
/// Hosts 護欄：僅引用 Application ports＋DTO；組合入口與 Migrator 遷移接線為明文豁免；橫切只進 HostBase。
/// </summary>
public class HostGuardrailRules
{
    private static Assembly HostBaseAssembly => typeof(HostBaseInfo).Assembly;

    private static Assembly[] AllHostAssemblies { get; } =
    [
        typeof(Hosts.HQ.HostMarker).Assembly,
        typeof(Hosts.Store.HostMarker).Assembly,
        typeof(Hosts.Consumer.HostMarker).Assembly,
        typeof(Hosts.Dev.HostMarker).Assembly,
        typeof(Hosts.Migrator.HostMarker).Assembly,
        HostBaseAssembly,
    ];

    private static string[] HostNamespaces { get; } =
    [
        "Hosts.HQ",
        "Hosts.Store",
        "Hosts.Consumer",
        "Hosts.Dev",
        "Hosts.Migrator",
    ];

    [Fact]
    public void Hosts_Shall_Not_Depend_On_Iam_Domain_Namespace()
    {
        var failing = InspectAll(host =>
            Types.InAssembly(host)
                .ShouldNot()
                .HaveDependencyOn("Iam.Domain")
                .GetResult());

        Assert.True(failing.Length == 0, Describe("Host referencing Iam.Domain", failing));
    }

    [Fact]
    public void Hosts_Shall_Not_Depend_On_Iam_Infrastructure_Except_Sanctioned_Entries()
    {
        var failing = AllHostAssemblies
            .SelectMany(host =>
            {
                var result = Types.InAssembly(host)
                    .ShouldNot()
                    .HaveDependencyOn("Iam.Infrastructure")
                    .GetResult();
                return (result.FailingTypeNames ?? Array.Empty<string>())
                    .Where(type => !IsSanctioned(host, type))
                    .Select(type => $"{host.GetName().Name}: {type}");
            })
            .ToArray();

        Assert.True(failing.Length == 0, Describe("Host referencing Iam.Infrastructure outside sanctioned entries", failing));
    }

    // Sanctioned dependents on Iam.Infrastructure (design G4 + ADR-003):
    // - every type in Hosts.Migrator (the sole migrator binds DbContext + migrations);
    // - only the Hosts.HQ composition root calling AddIam — NetArchTest reports the global-namespace
    //   partial Program by bare full name "Program", not "Hosts.HQ.Program".
    private static bool IsSanctioned(Assembly host, string typeName) =>
        host.GetName().Name == "Hosts.Migrator"
        || (host.GetName().Name == "Hosts.HQ" && typeName == "Program");

    [Fact]
    public void OrgUnitHosts_Shall_Not_Reference_Each_Other_Canonical_Exposure()
    {
        // Cross-host only: NetArchTest's namespace-prefix dependency check would count a host's own
        // namespace (self/nested types) as a violation, so the rule bans every BUT the host's own.
        var failing = AllHostAssemblies
            .SelectMany(host =>
            {
                var assemblyName = host.GetName().Name!;
                var bannedNamespaces = HostNamespaces.Where(ns => ns != assemblyName).ToArray();
                return Types.InAssembly(host)
                    .ShouldNot()
                    .HaveDependencyOnAny(bannedNamespaces)
                    .GetResult()
                    .FailingTypeNames?
                    .Select(type => $"{assemblyName}: {type}") ?? Array.Empty<string>();
            })
            .ToArray();

        Assert.True(failing.Length == 0, Describe("cross-host reference (canonical exposure violated)", failing));
    }

    [Fact]
    public void CrossCutting_Types_Shall_Live_Only_In_HostBase()
    {
        var failing = AllHostAssemblies
            .Where(host => host != HostBaseAssembly)
            .SelectMany(host => Types.InAssembly(host)
                .ShouldNot()
                .ResideInNamespace("Hosts.Base")
                .GetResult().FailingTypeNames
                ?? Array.Empty<string>())
            .Select(t => t)
            .ToArray();

        Assert.True(failing.Length == 0, Describe("cross-cutting type outside HostBase", failing));
    }

    [Fact]
    public void HostBase_Shall_Not_Depend_On_Iam_Domain()
    {
        var result = Types.InAssembly(HostBaseAssembly)
            .ShouldNot()
            .HaveDependencyOn("Iam.Domain")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"HostBase referencing Iam.Domain: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void HostMarkers_Shall_Cover_Every_Org_Unit_Host()
    {
        // Structural completeness: every expected host assembly declares its org unit marker.
        var declared = AllHostAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => string.Equals(type.Name, "HostMarker", StringComparison.Ordinal))
            .Select(type => type.Namespace)
            .Where(candidate => candidate != null)
            .Distinct()
            .ToArray();

        var missing = HostNamespaces.Where(expected => !declared.Contains(expected)).ToArray();
        Assert.True(missing.Length == 0,
            $"Missing HostMarkers: {string.Join(", ", missing)}. Declared: {string.Join(", ", declared)}");
    }

    private static string[] InspectAll(Func<Assembly, TestResult> inspect)
    {
        return AllHostAssemblies
            .SelectMany(host =>
            {
                var result = inspect(host);
                return result.FailingTypeNames?.Select(type => $"{host.GetName().Name}: {type}") ?? Array.Empty<string>();
            })
            .ToArray();
    }

    private static string Describe(string ruleName, string[] violations)
    {
        return violations.Length == 0
            ? $"{ruleName}: green"
            : $"{ruleName} — {string.Join("; ", violations)}";
    }
}
