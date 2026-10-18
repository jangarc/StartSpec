using Iam.Application.Users;

namespace Iam.ContractTests.Fakes;

/// <summary>
/// In-memory fake for the <see cref="IPasswordHasher"/> auth seam port. Hashes hold no plaintext
/// (hash→password kept in instance state) so the SAME port contract facts run over fake and real
/// Argon2id legs; each hash is unique (the fake's analogue of salting).
/// 認證接縫偽雜湊器：雜湊不含明文（實例狀態保存映射）、每次雜湊皆唯一（模擬鹽）；契約事實在 fake＋真實腿雙跑。
/// </summary>
public sealed class InMemoryPasswordHasher : IPasswordHasher
{
    private readonly Dictionary<string, string> _hashToPassword = [];
    private int _counter;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        var hash = $"fake-argon2:{_counter++}";
        _hashToPassword[hash] = password;
        return hash;
    }

    public bool Verify(string password, string existingHash)
    {
        return _hashToPassword.TryGetValue(existingHash, out var expected) && expected == password;
    }
}
