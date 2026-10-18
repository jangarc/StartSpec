namespace Iam.Application.Authorization;

/// <summary>
/// Deployment-level authorization tuning (design decision 7) — no per-tenant options are ever added
/// here (tenant data stays in the policy stores). Values self-validate at construction so bad rollout
/// configuration fails at startup, never mid-request.
/// 部署級授權調校（決策 7）：禁 per-tenant options；建構當下自驗，壞配置在啟動即失敗。
/// </summary>
public sealed record AuthorizationOptions
{
    private int _propagationBoundSeconds = 5;

    private TimeSpan _watcherInterval = TimeSpan.FromSeconds(1);

    /// <summary>Propagation bound the revocation scenario asserts (default 5 seconds; tests may override).</summary>
    /// <summary>撤銷傳播斷言上限（預設 5 秒；測試可覆寫）。</summary>
    public int PropagationBoundSeconds
    {
        get => _propagationBoundSeconds;
        init
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "PropagationBoundSeconds must be positive.");
            }

            _propagationBoundSeconds = value;
        }
    }

    /// <summary>Polling fallback for transports without push (in-memory/broker transports push today).</summary>
    /// <summary>無推送傳輸的輪詢退路（今日 in-memory／broker 皆為推送）。</summary>
    public TimeSpan WatcherInterval
    {
        get => _watcherInterval;
        init
        {
            if (value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "WatcherInterval must be positive.");
            }

            _watcherInterval = value;
        }
    }
}
