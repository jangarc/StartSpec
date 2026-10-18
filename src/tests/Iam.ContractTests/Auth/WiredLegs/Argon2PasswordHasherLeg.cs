using Iam.Application.Users;
using Iam.ContractTests.Auth;
using Iam.Infrastructure.Persistence;

namespace Iam.ContractTests.Auth.WiredLegs;

/// <summary>
/// Wired leg of the hasher port contract: the real Argon2id adapter (Konscious, design decision 9)
/// must satisfy the SAME facts the fake pinned — pure CPU, no database involved.
/// 真實腿：Argon2id 轉接須滿足 fake 所釘同一事實；純 CPU，不涉資料庫。
/// </summary>
public sealed class Argon2PasswordHasherLeg : PasswordHasherContractBase
{
    protected override Task<IPasswordHasher> CreateHasherAsync()
    {
        return Task.FromResult<IPasswordHasher>(new Argon2PasswordHasher());
    }
}
