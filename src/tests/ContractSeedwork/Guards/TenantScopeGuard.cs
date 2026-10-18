using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ContractSeedwork.Guards;

/// <summary>
/// Guard-first query interceptor (task 2.3): wired persistence seams register it on their EF options;
/// any SELECT hitting a registered tenant-scoped table without a tenant_id predicate throws on the
/// spot, so queries are born guarded (H1 precedent). Platform-level stores (Party identity store) are
/// exempt by NOT being registered — registration is the seam's declaration of tenant scoping.
/// Throwing in place makes removal of any scoping filter immediately visible: red on removal, green on restore.
/// </summary>
public sealed class TenantScopeGuardInterceptor(IReadOnlySet<string> scopedTables) : DbCommandInterceptor
{
    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        ThrowIfViolation(command.CommandText);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        ThrowIfViolation(command.CommandText);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    private void ThrowIfViolation(string commandText)
    {
        if (Violates(commandText, scopedTables))
        {
            throw new TenantScopeViolationException(commandText);
        }
    }

    internal static bool Violates(string commandText, IReadOnlySet<string> scopedTables)
    {
        var sql = commandText.ReplaceLineEndings(" ").Replace("\"", string.Empty).ToLowerInvariant();

        foreach (var table in scopedTables)
        {
            var fromAt = sql.IndexOf($"from {table}", StringComparison.Ordinal);
            if (fromAt < 0)
            {
                continue;
            }

            var tail = sql[fromAt..];
            return !(tail.Contains("tenant_id =") || tail.Contains("tenant_id="));
        }

        return false;
    }
}

/// <summary>Red-light signal of task 2.3: a scoped-table SELECT ran without a tenant_id predicate.</summary>
public sealed class TenantScopeViolationException(string commandText)
    : Exception($"Tenant-scoped query ran without a tenant_id predicate. Command:{Environment.NewLine}{commandText}");

/// <summary>Tables this seam declares tenant-scoped (schema-qualified, lowercase, unquoted); grows with the seam.</summary>
public static class TenantQueryTables
{
    public static readonly string[] All = ["iam.organizations", "iam.tenants", "iam.users", "iam.roles", "iam.role_assignments"];
}
