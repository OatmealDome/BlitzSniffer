using System;

namespace BlitzSniffer.Network.Netcode.Clone
{
    public class ClockChangedEventArgs : EventArgs
    {
        public uint Clock
        {
            get;
            set;
        }

        public ClockChangedEventArgs(uint clock)
        {
            Clock = clock;
        }

    }
}
