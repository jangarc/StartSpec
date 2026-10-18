using Iam.Application.Users;
using Xunit;

namespace Iam.ContractTests.Auth;

/// <summary>
/// Password hasher port contract (auth seam, scenarios derived from identity/user-auth; the fake leg
/// binds FIRST, the wired Argon2id leg re-runs the SAME locked facts): hashes are salted one-way
/// representations that never expose plaintext, verification is exact (wrong=false, right=true),
/// salt uniqueness is observable (same password twice → distinct hashes), and corrupt or unknown
/// hash formats verify to false without throwing.
/// 雜湊埠契約：鹽化單向且不露明文；驗證精確（錯 false／對 true）；同密碼兩次雜湊必異（鹽可觀察）；毀損或未知格式一律 false 且不拋例外；fake 先行、真實 Argon2id 腿重跑同一鎖定事實。
/// </summary>
public abstract class PasswordHasherContractBase
{
    // ":" is outside the base64 alphabet, so no correct digest can ever contain the secret verbatim.
    private const string Secret = "pass:word-0";

    protected abstract Task<IPasswordHasher> CreateHasherAsync();

    [Fact]
    public async Task Hash_never_contains_the_plaintext()
    {
        var hasher = await CreateHasherAsync();

        var hash = hasher.Hash(Secret);

        Assert.DoesNotContain(Secret, hash);
    }

    [Fact]
    public async Task Verify_accepts_the_correct_password()
    {
        var hasher = await CreateHasherAsync();
        var hash = hasher.Hash(Secret);

        Assert.True(hasher.Verify(Secret, hash));
    }

    [Fact]
    public async Task Verify_rejects_the_wrong_password()
    {
        var hasher = await CreateHasherAsync();
        var hash = hasher.Hash(Secret);

        Assert.False(hasher.Verify("wrong:pass", hash));
    }

    [Fact]
    public async Task Same_password_hashed_twice_yields_distinct_hashes()
    {
        var hasher = await CreateHasherAsync();

        var first = hasher.Hash(Secret);
        var second = hasher.Hash(Secret);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public async Task Corrupt_hash_form_verifies_false_without_throwing()
    {
        var hasher = await CreateHasherAsync();

        Assert.False(hasher.Verify(Secret, "not-a-real-hash"));
    }
}
