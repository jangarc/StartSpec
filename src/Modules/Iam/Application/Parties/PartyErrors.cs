namespace Iam.Application.Parties;

/// <summary>
/// Persistence conflict: the identifier tuple is claimed by another party (platform-global
/// uniqueness; provisioning resolves exact matches first, the store is the backstop).
/// 儲存衝突：證件元組已被他方認領；provisioning 流程先行精確比對，此為唯一性後盾。
/// </summary>
public sealed class PartyIdentifierConflictException(string typeCode, string number, Exception? innerException = null)
    : Exception($"Party identifier tuple '{typeCode}:{number}' is already claimed by another party.", innerException)
{
    public string TypeCode { get; } = typeCode;

    public string Number { get; } = number;
}

/// <summary>The exact identifier tuple resolves to no party; binding/deactivation/consent may never create one implicitly. 精確元組未命中；綁定不得隱含新建。</summary>
public sealed class PartyNotFoundException(string typeCode, string number)
    : Exception($"No party carries the exact identifier tuple '{typeCode}:{number}'.")
{
    public string TypeCode { get; } = typeCode;

    public string Number { get; } = number;
}
