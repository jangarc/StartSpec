namespace SharedKernel;

/// <summary>
/// Base for every domain event (ADR-007 admission category 1, ADR-009 V1 envelope).
/// Envelope fields are frozen from day one: <c>event_id</c>, <c>occurred_at</c>, <c>tenant_id</c>,
/// <c>correlation_id</c>, <c>causation_id</c>, <c>version</c>, <c>payload</c>. Events evolve additively;
/// deployments are lockstep across Hosts until broker persistence or a service split (V2 trigger).
/// 每個領域事件的基底：標準信封自第一日凍結，加法演進，broker 持久化或服務拆分前跨 Hosts 鎖步。
/// </summary>
public abstract record DomainEvent
{
    /// <summary>Unique event identity（<c>event_id</c>）.</summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>UTC time the event occurred（<c>occurred_at</c>; clock-injected by the publishing module）.</summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>Owning tenant, or null for platform-scoped events（<c>tenant_id</c>）.</summary>
    public required string? TenantId { get; init; }

    /// <summary>Correlates a logical operation across events（<c>correlation_id</c>）.</summary>
    public Guid? CorrelationId { get; init; }

    /// <summary>Points at the producing event/message（<c>causation_id</c>）.</summary>
    public Guid? CausationId { get; init; }

    /// <summary>Envelope/event schema version; V1 until the ADR-009 V2 trigger（<c>version</c>）.</summary>
    public int Version { get; init; } = 1;

    /// <summary>Serialized payload transport name（<c>payload</c> carries the typed record itself）.</summary>
    public byte[] PayloadBytes { get; init; } = [];
}
