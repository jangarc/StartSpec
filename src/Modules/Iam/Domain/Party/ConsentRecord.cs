namespace Iam.Domain.Party;

/// <summary>Consent timeline kind.</summary>
public enum ConsentKind
{
    Grant = 0,
    Revoke = 1,
}

/// <summary>
/// Append-only consent record: timestamped and immutable by construction (positional record,
/// init-only fields). Revocation is a NEW record; existing records are never edited (identity/party spec).
/// 同意時間軸記錄：具時間戳、結構上不可變；撤銷為新增記錄，絕不修改既有記錄。
/// </summary>
public sealed record ConsentRecord(Guid ConsentId, ConsentKind Kind, string TenantId, string Purpose, DateTimeOffset OccurredAt);
