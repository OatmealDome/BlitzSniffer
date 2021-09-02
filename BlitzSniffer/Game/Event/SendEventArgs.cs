using System;

namespace BlitzSniffer.Game.Event
{
    public class SendEventArgs : EventArgs
    {
        public SnifferEvent Event
        {
            get;
            private set;
        }

        public SendEventArgs(SnifferEvent snifferEvent)
        {
            Event = snifferEvent;
        }

    }
}
