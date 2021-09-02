using System;
using System.Collections.Concurrent;
using System.Threading;

namespace BlitzSniffer.Game.Event
{
    public class EventTracker
    {
        public static readonly EventTracker Instance = new EventTracker();

        private readonly BlockingCollection<SnifferEvent> EventQueue;
        private readonly CancellationTokenSource TokenSource;

        public delegate void SendEventHandler(object sender, SendEventArgs args);
        public event SendEventHandler SendEvent;

        public EventTracker()
        {
            EventQueue = new BlockingCollection<SnifferEvent>(new ConcurrentQueue<SnifferEvent>());
            TokenSource = new CancellationTokenSource();

            new Thread(SendEvents).Start();
        }

        public void AddEvent(SnifferEvent snifferEvent)
        {
            EventQueue.Add(snifferEvent);
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
                    if (EventQueue.TryTake(out SnifferEvent snifferEvent, -1, TokenSource.Token))
                    {
                        SendEventArgs args = new SendEventArgs(snifferEvent);
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
