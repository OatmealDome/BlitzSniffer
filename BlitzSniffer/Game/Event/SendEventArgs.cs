using System;

namespace BlitzSniffer.Game.Event
{
    public class SendEventArgs : EventArgs
    {
        public GameEvent GameEvent
        {
            get;
            private set;
        }

        public SendEventArgs(GameEvent gameEvent)
        {
            GameEvent = gameEvent;
        }

    }
}
