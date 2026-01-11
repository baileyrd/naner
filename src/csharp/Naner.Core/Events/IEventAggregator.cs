using System;

namespace Naner.Core.Events;

/// <summary>
/// Interface for the event aggregator that manages event subscriptions and publishing.
/// Implements the Observer pattern for loose coupling between components.
/// </summary>
public interface IEventAggregator
{
    /// <summary>
    /// Subscribes to events of a specific type.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to subscribe to</typeparam>
    /// <param name="handler">The handler to invoke when the event is published</param>
    /// <returns>A subscription token that can be used to unsubscribe</returns>
    IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : NanerEvent;

    /// <summary>
    /// Subscribes to events of a specific type with async support.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to subscribe to</typeparam>
    /// <param name="handler">The async handler to invoke when the event is published</param>
    /// <returns>A subscription token that can be used to unsubscribe</returns>
    IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : NanerEvent;

    /// <summary>
    /// Publishes an event to all subscribers.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to publish</typeparam>
    /// <param name="eventToPublish">The event instance to publish</param>
    void Publish<TEvent>(TEvent eventToPublish) where TEvent : NanerEvent;

    /// <summary>
    /// Publishes an event to all subscribers asynchronously.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to publish</typeparam>
    /// <param name="eventToPublish">The event instance to publish</param>
    Task PublishAsync<TEvent>(TEvent eventToPublish) where TEvent : NanerEvent;
}
