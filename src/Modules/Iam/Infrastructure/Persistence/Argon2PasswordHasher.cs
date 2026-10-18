using System.Security.Cryptography;
using System.Text;
using Iam.Application.Users;
using Konscious.Security.Cryptography;

namespace Iam.Infrastructure.Persistence;

/// <summary>
/// Real auth seam adapter: Argon2id (Konscious, MIT; design decision 4/9) with a fresh random salt
/// prepended to each digest, both base64-coded. Verification recomputes the digest and compares in
/// constant time; decode failures and wrong digest lengths verify to false and never throw on
/// corrupt storage.
/// 真實認證接縫轉接：Argon2id；每次雜湊前綴新隨機鹽並以 base64 編碼；驗證重算摘要並以常數時間比較；解碼失敗或長度不符一律 false（毀損儲存不拋例外）。
/// </summary>
public sealed class Argon2PasswordHasher : IPasswordHasher
{
    private const int SaltSizeBytes = 16;
    private const int DigestSizeBytes = 32;
    private const int Parallelism = 2;
    private const int Iterations = 3;
    private const int MemorySizeKib = 8192;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        var salt = new byte[SaltSizeBytes];
        RandomNumberGenerator.Fill(salt);
        var digest = ComputeDigest(password, salt);
        return Convert.ToBase64String([.. salt, .. digest]);
    }

    public bool Verify(string password, string existingHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(existingHash))
        {
            return false;
        }

        byte[] stored;
        try
        {
            stored = Convert.FromBase64String(existingHash);
        }
        catch (FormatException)
        {
            return false;
        }

        if (stored.Length != SaltSizeBytes + DigestSizeBytes)
        {
            return false;
        }

        var salt = stored[..SaltSizeBytes];
        var expectedDigest = stored[SaltSizeBytes..];
        var candidateDigest = ComputeDigest(password, salt);
        return CryptographicOperations.FixedTimeEquals(candidateDigest, expectedDigest);
    }

    private static byte[] ComputeDigest(string password, byte[] salt)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = Parallelism,
            Iterations = Iterations,
            MemorySize = MemorySizeKib,
        };
        return argon2.GetBytes(DigestSizeBytes);
    }
}
