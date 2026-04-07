using UnityEngine;
using System;
using System.Collections.Generic;

namespace EventManagment
{
public struct GameEvent
{
    public string EventType;
    public float Timestamp;
    public object Data;

    public GameEvent(string eventType)
    {
        EventType = eventType;
        Timestamp = Time.time;
        Data = null;
    }

    public GameEvent(string eventType, object data)
    {
        EventType = eventType;
        Timestamp = Time.time;
        Data = data;
    }
}

public class EventBus
{
    private readonly Dictionary<string, List<Action<GameEvent>>> listeners = new();
    private readonly Queue<GameEvent> eventQueue = new();
    private bool isProcessing;

    public delegate bool SendEventDelegate(GameEvent gameEvent);

    private readonly SendEventDelegate _sendEventDelegate;

    public EventBus(SendEventDelegate sendEvent)
    {
        _sendEventDelegate = sendEvent;
    }

    public void Subscribe(string eventType, Action<GameEvent> listener)
    {
        if (!listeners.TryGetValue(eventType, out var list))
        {
            list = new List<Action<GameEvent>>();
            listeners[eventType] = list;
        }
        if (!list.Contains(listener))
            list.Add(listener);
    }

    public void Unsubscribe(string eventType, Action<GameEvent> listener)
    {
        if (listeners.TryGetValue(eventType, out var list))
        {
            list.Remove(listener);
        }
    }

    public void SendEvent(GameEvent gameEvent)
    {
        if (_sendEventDelegate(gameEvent))
            return;

        if (!listeners.TryGetValue(gameEvent.EventType, out var list))
            return;

        for (int i = list.Count - 1; i >= 0; i--)
        {
            list[i]?.Invoke(gameEvent);
        }
    }

    public void QueueEvent(GameEvent gameEvent)
    {
        eventQueue.Enqueue(gameEvent);
    }

    public void ProcessQueuedEvents()
    {
        if (isProcessing) return;

        isProcessing = true;
        while (eventQueue.Count > 0)
        {
            var gameEvent = eventQueue.Dequeue();
            SendEvent(gameEvent);
        }
        isProcessing = false;
    }

    public void Clear()
    {
        listeners.Clear();
        eventQueue.Clear();
    }
}
}