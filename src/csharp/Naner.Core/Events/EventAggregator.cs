using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Naner.Core.Events;

/// <summary>
/// Thread-safe event aggregator implementation for decoupling components.
/// Enables publish-subscribe pattern for installation events.
/// </summary>
public class EventAggregator : IEventAggregator
{
    private readonly ConcurrentDictionary<Type, List<object>> _subscriptions = new();
    private readonly object _lock = new();

    /// <summary>
    /// Gets the singleton instance of the event aggregator.
    /// </summary>
    public static EventAggregator Instance { get; } = new();

    public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : NanerEvent
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var eventType = typeof(TEvent);
        var subscription = new Subscription<TEvent>(handler, null);

        lock (_lock)
        {
            if (!_subscriptions.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<object>();
                _subscriptions[eventType] = handlers;
            }
            handlers.Add(subscription);
        }

        return new SubscriptionToken(() => Unsubscribe(eventType, subscription));
    }

    public IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : NanerEvent
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var eventType = typeof(TEvent);
        var subscription = new Subscription<TEvent>(null, handler);

        lock (_lock)
        {
            if (!_subscriptions.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<object>();
                _subscriptions[eventType] = handlers;
            }
            handlers.Add(subscription);
        }

        return new SubscriptionToken(() => Unsubscribe(eventType, subscription));
    }

    public void Publish<TEvent>(TEvent eventToPublish) where TEvent : NanerEvent
    {
        if (eventToPublish == null)
            throw new ArgumentNullException(nameof(eventToPublish));

        var eventType = typeof(TEvent);

        List<object>? handlers;
        lock (_lock)
        {
            if (!_subscriptions.TryGetValue(eventType, out handlers))
                return;

            // Create a copy to avoid issues with modifications during iteration
            handlers = new List<object>(handlers);
        }

        foreach (var handler in handlers)
        {
            if (handler is Subscription<TEvent> subscription)
            {
                try
                {
                    if (subscription.SyncHandler != null)
                    {
                        subscription.SyncHandler(eventToPublish);
                    }
                    else if (subscription.AsyncHandler != null)
                    {
                        // Fire and forget for async handlers in sync publish
                        _ = subscription.AsyncHandler(eventToPublish);
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't throw to prevent one handler from affecting others
                    Logger.Debug($"Event handler error for {eventType.Name}: {ex.Message}", debugMode: false);
                }
            }
        }
    }

    public async Task PublishAsync<TEvent>(TEvent eventToPublish) where TEvent : NanerEvent
    {
        if (eventToPublish == null)
            throw new ArgumentNullException(nameof(eventToPublish));

        var eventType = typeof(TEvent);

        List<object>? handlers;
        lock (_lock)
        {
            if (!_subscriptions.TryGetValue(eventType, out handlers))
                return;

            // Create a copy to avoid issues with modifications during iteration
            handlers = new List<object>(handlers);
        }

        var tasks = new List<Task>();

        foreach (var handler in handlers)
        {
            if (handler is Subscription<TEvent> subscription)
            {
                try
                {
                    if (subscription.AsyncHandler != null)
                    {
                        tasks.Add(subscription.AsyncHandler(eventToPublish));
                    }
                    else if (subscription.SyncHandler != null)
                    {
                        subscription.SyncHandler(eventToPublish);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Debug($"Event handler error for {eventType.Name}: {ex.Message}", debugMode: false);
                }
            }
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }
    }

    private void Unsubscribe(Type eventType, object subscription)
    {
        lock (_lock)
        {
            if (_subscriptions.TryGetValue(eventType, out var handlers))
            {
                handlers.Remove(subscription);
            }
        }
    }

    /// <summary>
    /// Clears all subscriptions. Useful for testing.
    /// </summary>
    public void ClearAllSubscriptions()
    {
        lock (_lock)
        {
            _subscriptions.Clear();
        }
    }

    private class Subscription<TEvent> where TEvent : NanerEvent
    {
        public Action<TEvent>? SyncHandler { get; }
        public Func<TEvent, Task>? AsyncHandler { get; }

        public Subscription(Action<TEvent>? syncHandler, Func<TEvent, Task>? asyncHandler)
        {
            SyncHandler = syncHandler;
            AsyncHandler = asyncHandler;
        }
    }

    private class SubscriptionToken : IDisposable
    {
        private readonly Action _unsubscribe;
        private bool _disposed;

        public SubscriptionToken(Action unsubscribe)
        {
            _unsubscribe = unsubscribe;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _unsubscribe();
            _disposed = true;
        }
    }
}
