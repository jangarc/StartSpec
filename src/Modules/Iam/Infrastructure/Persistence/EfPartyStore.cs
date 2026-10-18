using Iam.Application.Parties;
using Iam.Domain.Party;
using Microsoft.EntityFrameworkCore;

namespace Iam.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of the platform Party store: exact-only reach over the guarded store, and
/// whole-aggregate save with an owner-aware pre-check plus the database unique key as race backstop.
/// The context is platform-scoped (no tenant filter — party tables are exempt by spec).
/// EF Core 實作：僅精確觸達、整聚合保存；預檢排除自身 party，資料庫唯一鍵為 race 後盾。
/// </summary>
public sealed class EfPartyStore(DbContextOptions<IamDbContext> options) : IPartyStore
{
    private readonly IamDbContext _context = new(options);

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync().ConfigureAwait(false);
    }

    public async Task<Party?> FindExactAsync(PartyIdentifier tuple, CancellationToken cancellationToken = default)
    {
        return await _context.Parties
            .Where(party => party.Identifiers.Any(identifier => identifier.TypeCode == tuple.TypeCode && identifier.Number == tuple.Number))
            .Include(party => party.Identifiers)
            .Include(party => party.Roles)
            .Include(party => party.Consents)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task SaveAsync(Party party, CancellationToken cancellationToken = default)
    {
        foreach (var tuple in party.Identifiers)
        {
            var tupleTaken = await _context.Identifiers
                .AnyAsync(candidate
                    => candidate.TypeCode == tuple.TypeCode
                        && candidate.Number == tuple.Number
                        && EF.Property<Guid>(candidate, "party_id") != party.PartyId,
                    cancellationToken);
            if (tupleTaken)
            {
                throw new PartyIdentifierConflictException(tuple.TypeCode, tuple.Number);
            }
        }

        var wasAutoDetectEnabled = _context.ChangeTracker.AutoDetectChangesEnabled;
        _context.ChangeTracker.AutoDetectChangesEnabled = false;
        bool attachmentSaved = false;
        try
        {
            if (_context.Entry(party).State is not EntityState.Detached)
            {
                attachmentSaved = true;
                // Attachment-style loads are tracked in this same store context. Domain-issued child
                // keys (set Guids) would be promoted to Modified by DetectChanges (0-row UPDATE), so
                // auto-detect is suspended during this pass and children born in this save are forced
                // to Added before SaveChanges runs.
                foreach (var child in party.Identifiers.Cast<object>().Concat(party.Roles).Concat(party.Consents))
                {
                    if (_context.Entry(child).State is EntityState.Detached)
                    {
                        _context.Add(child);
                    }
                }
            }
        }
        finally
        {
            _context.ChangeTracker.AutoDetectChangesEnabled = wasAutoDetectEnabled;
        }

        if (attachmentSaved)
        {
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        _context.Add(party);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true
            || exception.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true)
        {
            throw new PartyIdentifierConflictException(party.Identifiers[0].TypeCode, party.Identifiers[0].Number, exception);
        }
    }
}
