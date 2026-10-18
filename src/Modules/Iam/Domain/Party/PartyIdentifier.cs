namespace Iam.Domain.Party;

/// <summary>
/// Exact identifier tuple (identifier type + number) — the ONLY match key for Party identity.
/// Record equality IS the exact-match contract; display name and birthdate are never part of it,
/// which structurally forbids fuzzy merging (identity/party spec).
/// 精確識別元組（證件類型＋證號）——Party 身分的唯一比對鍵；姓名生日不參與匹配，結構上禁止模糊合併。
/// </summary>
public sealed record PartyIdentifier(string TypeCode, string Number)
{
    /// <summary>Normalizes by trimming only — "exact" means exact after whitespace cleanup.</summary>
    public static PartyIdentifier Create(string typeCode, string number)
    {
        if (string.IsNullOrWhiteSpace(typeCode))
        {
            throw new ArgumentException("Identifier type is required.", nameof(typeCode));
        }

        if (string.IsNullOrWhiteSpace(number))
        {
            throw new ArgumentException("Identifier number is required.", nameof(number));
        }

        return new PartyIdentifier(typeCode.Trim(), number.Trim());
    }
}
