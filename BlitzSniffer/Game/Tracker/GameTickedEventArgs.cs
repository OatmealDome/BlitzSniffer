using System;

namespace BlitzSniffer.Game.Tracker
{
    class GameTickedEventArgs : EventArgs
    {
        public uint ElapsedTicks
        {
            get;
            set;
        }

        public GameTickedEventArgs(uint elapsedTicks)
        {
            ElapsedTicks = elapsedTicks;
        }

    }
}
