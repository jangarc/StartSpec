namespace Iam.Application.Users;

/// <summary>
/// Auth seam port (contract-kept): hashes and verifies passwords. Hashes are salted and one-way;
/// hashing never exposes plaintext, and an unknown or corrupt hash format verifies to false. The
/// real Argon2id adapter binds in Infrastructure; tests bind the fake first.
/// 認證接縫埠：雜湊與驗證密碼；雜湊鹽化且單向、不含明文，未知或毀損格式一律驗證為 false；真實 Argon2id 由 Infrastructure 綁定、測試先綁 fake。
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Returns a fresh salted hash of the password.</summary>
    string Hash(string password);

    /// <summary>Verifies a password against a stored hash; wrong or unreadable input yields false.</summary>
    bool Verify(string password, string existingHash);
}
