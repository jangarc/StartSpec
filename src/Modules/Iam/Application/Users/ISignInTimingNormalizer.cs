namespace Iam.Application.Users;

/// <summary>
/// Auth seam port: pads sign-in responses to a shared minimum duration so success and every failure
/// shape are timing-indistinguishable (design decision 4 — an unknown tenant must not fail fast).
/// 計時歸一化埠：將登入回應墊至共同最短時長，成功與各種失敗時序上不可區分（設計決策 4——未知租戶不得快失敗）。
/// </summary>
public interface ISignInTimingNormalizer
{
    /// <summary>Awaits any remainder between the elapsed time and the configured floor.</summary>
    Task PadAsync(DateTimeOffset startedAt, CancellationToken cancellationToken = default);
}
