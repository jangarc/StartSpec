using SharedKernel.Ports;

namespace Iam.Application.Users;

/// <summary>
/// Pads the elapsed time to the configured minimum using the module clock; over-floor durations
/// return immediately (failures never extend beyond the floor by waiting).
/// 依模組時鐘將經過時間墊至最短時長；已逾下限者立即返回（墊底不拉長失敗回應）。
/// </summary>
public sealed class MinimumSignInTimingNormalizer(TimeSpan minimum, IClock clock) : ISignInTimingNormalizer
{
    public async Task PadAsync(DateTimeOffset startedAt, CancellationToken cancellationToken = default)
    {
        var remaining = minimum - (clock.UtcNow - startedAt);
        if (remaining > TimeSpan.Zero)
        {
            await Task.Delay(remaining, cancellationToken);
        }
    }
}
