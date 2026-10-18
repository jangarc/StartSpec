namespace SharedKernel.Ports;

/// <summary>
/// Cross-cutting event transport port (ADR-007 admission category 3). Binding adapters live in
/// <c>HostBase</c>, never inside a single module's Infrastructure (iam design D1' / platform spec
/// "Hosts compose per org unit").
/// 跨模組事件埠；binding adapter 住 <c>HostBase</c>。
/// Seam obligation (ADR-010): delivery is <b>at-least-once</b> — consumers SHALL be idempotent.
/// 接縫義務：at-least-once 傳遞，消費者 SHALL 冪等。
/// </summary>
public interface IEventBus
{
    /// <summary>Publishes an event; delivery is at-least-once, so handlers may see duplicates.</summary>
    Task PublishAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes a handler for strongly-typed delivery. The returned handle unsubscribes on dispose.
    /// </summary>
    IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
        where TEvent : DomainEvent;
}
