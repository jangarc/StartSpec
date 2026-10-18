using SharedKernel;
using SharedKernel.Ports;

namespace Hosts.Base;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

/// <summary>
/// At-least-once in-memory delivery (platform spec "Event delivery contract"): every publication hands
/// the event to all matching subscribers synchronously; consumers stay idempotent. Broker transports
/// (V2 trigger) swap in behind the same port.
/// 事件接縫 in-memory 實作：同步投遞予訂閱者；消費者保持冪等；broker 於 V2 觸發時替換。
/// </summary>
public sealed class InMemoryEventBus : IEventBus
{
    private readonly List<Subscription> _subscriptions = [];

    public async Task PublishAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        foreach (var subscription in _subscriptions.Where(s => s.EventType.IsInstanceOfType(domainEvent)).ToArray())
        {
            await subscription.Handler(domainEvent, cancellationToken);
        }
    }

    public IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
        where TEvent : DomainEvent
    {
        Subscription registration = new(this, typeof(TEvent), (domainEvent, cancellationToken) => handler((TEvent)domainEvent, cancellationToken));
        _subscriptions.Add(registration);
        return registration;
    }

    private void Remove(Subscription registration) => _subscriptions.Remove(registration);

    private sealed class Subscription : IDisposable
    {
        public Subscription(InMemoryEventBus owner, Type eventType, Func<DomainEvent, CancellationToken, Task> handler)
        {
            Owner = owner;
            EventType = eventType;
            Handler = handler;
        }

        private InMemoryEventBus Owner { get; }

        public Type EventType { get; }

        public Func<DomainEvent, CancellationToken, Task> Handler { get; }

        public void Dispose() => Owner.Remove(this);
    }
}
