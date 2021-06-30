using System;
using System.Collections.Concurrent;
using System.Threading;

namespace BlitzSniffer.Game.Event
{
    public class EventTracker
    {
        public static readonly EventTracker Instance = new EventTracker();

        private readonly BlockingCollection<GameEvent> EventQueue;
        private readonly CancellationTokenSource TokenSource;

        public delegate void SendEventHandler(object sender, SendEventArgs args);
        public event SendEventHandler SendEvent;

        public EventTracker()
        {
            EventQueue = new BlockingCollection<GameEvent>(new ConcurrentQueue<GameEvent>());
            TokenSource = new CancellationTokenSource();

            new Thread(SendEvents).Start();
        }

        public void AddEvent(GameEvent gameEvent)
        {
            EventQueue.Add(gameEvent);
        }

        public void Shutdown()
        {
            TokenSource.Cancel();
        }

        private void SendEvents()
        {
            while (!TokenSource.IsCancellationRequested)
            {
                try
                {
                    if (EventQueue.TryTake(out GameEvent gameEvent, -1, TokenSource.Token))
                    {
                        SendEventArgs args = new SendEventArgs(gameEvent);
                        SendEvent?.Invoke(this, args);
                    }
                }
                catch (OperationCanceledException)
                {
                    ;
                }
            }
        }

    }
}
