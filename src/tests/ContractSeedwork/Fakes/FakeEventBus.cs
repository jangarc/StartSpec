using SharedKernel;
using SharedKernel.Ports;

namespace ContractSeedwork.Fakes;

/// <summary>
/// In-memory fake for the SharedKernel <see cref="IEventBus"/> port. Delivery mirrors the locked port
/// contract (ADR-010 / platform spec "Event delivery contract"): <b>at-least-once</b> — every publication
/// hands the SAME event instance to all matching subscribers, and <see cref="SimulateRedeliveryAsync"/>
/// re-delivers the most recent publication without a new publish (same event_id!). Consumers are expected
/// to be idempotent; the fake makes duplicates observable instead of hiding them.
/// in-memory fake：at-least-once 語意、可模擬重投遞（同 event_id 再投），令冪等義務可被斷言。
/// </summary>
public sealed class FakeEventBus : IEventBus
{
    private readonly List<DomainEvent> _published = [];
    private readonly List<Subscription> _subscriptions = [];
    private int _deliveryCount;

    /// <summary>Every publication in order (single reference; a simulated redelivery does NOT append new entries).</summary>
    public IReadOnlyList<DomainEvent> Published => _published;

    /// <summary>All deliveries incl. simulated redelivery — the at-least-once observability hook.</summary>
    public int DeliveredCount => _deliveryCount;

    public async Task PublishAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _published.Add(domainEvent);
        await DeliverToAsync(domainEvent, cancellationToken);
    }

    public IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
        where TEvent : DomainEvent
    {
        Func<DomainEvent, CancellationToken, Task> adapter =
            (eventMessage, cancellationToken) => handler((TEvent)eventMessage, cancellationToken);

        var subscription = new Subscription(this, typeof(TEvent), adapter);
        _subscriptions.Add(subscription);
        return subscription;
    }

    /// <summary>Re-delivers the most recent publication without publishing a new one (duplicate delivery, same event_id).</summary>
    public async Task SimulateRedeliveryAsync(CancellationToken cancellationToken = default)
    {
        if (_published.Count == 0)
        {
            return;
        }

        await DeliverToAsync(_published[_published.Count - 1], cancellationToken);
    }

    private async Task DeliverToAsync(DomainEvent domainEvent, CancellationToken cancellationToken)
    {
        foreach (var subscription in _subscriptions)
        {
            if (subscription.EventType.IsInstanceOfType(domainEvent))
            {
                _deliveryCount++;
                await subscription.Handler(domainEvent, cancellationToken);
            }
        }
    }

    private void Remove(Subscription subscription)
    {
        _subscriptions.Remove(subscription);
    }

    private sealed class Subscription(FakeEventBus owner, Type eventType, Func<DomainEvent, CancellationToken, Task> handler) : IDisposable
    {
        public Type EventType => eventType;

        public Func<DomainEvent, CancellationToken, Task> Handler => handler;

        public void Dispose() => owner.Remove(this);
    }
}
