using Iam.Application.Users;
using Iam.ContractTests.Auth;
using Iam.ContractTests.Fakes;

namespace Iam.ContractTests.Auth.FakeLegs;

/// <summary>
/// Fake leg of the hasher port contract: binds FIRST (task 4.2 fake-first discipline), pinning the
/// locked facts before any real adapter exists.
/// fake 腿：先行綁定鎖定事實，真實轉接存在前即釘住契約。
/// </summary>
public sealed class InMemoryPasswordHasherLeg : PasswordHasherContractBase
{
    protected override Task<IPasswordHasher> CreateHasherAsync()
    {
        return Task.FromResult<IPasswordHasher>(new InMemoryPasswordHasher());
    }
}
