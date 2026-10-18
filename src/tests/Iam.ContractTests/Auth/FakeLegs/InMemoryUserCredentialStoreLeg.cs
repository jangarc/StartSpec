using Iam.Application.Users;
using Iam.ContractTests.Auth;
using Iam.ContractTests.Fakes;

namespace Iam.ContractTests.Auth.FakeLegs;

/// <summary>
/// Fake leg of the credential store port contract: binds FIRST on the in-memory fake (no user seeding
/// needed — the fake is keyed by the exact pair alone).
/// fake 腿：先行綁定；in-memory fake 以精確配對為鍵，無需 user 佈建。
/// </summary>
public sealed class InMemoryUserCredentialStoreLeg : UserCredentialStoreContractBase
{
    protected override Task<IUserCredentialStore> CreateCredentialStoreAsync()
    {
        return Task.FromResult<IUserCredentialStore>(new InMemoryUserCredentialStore());
    }

    protected override Task SeedUserAsync(string tenantId, string loginName)
    {
        return Task.CompletedTask;
    }

    protected override Task WaitForSeamAsync()
    {
        return Task.CompletedTask;
    }
}
