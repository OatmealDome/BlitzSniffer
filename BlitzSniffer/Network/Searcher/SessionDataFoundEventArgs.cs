using System;

namespace BlitzSniffer.Network.Searcher
{
    public class SessionDataFoundEventArgs : EventArgs
    {
        public SessionFoundDataType FoundDataType
        {
            get;
            set;
        }

        public byte[] Data
        {
            get;
            set;
        }

        public SessionDataFoundEventArgs(SessionFoundDataType type, byte[] data)
        {
            FoundDataType = type;
            Data = data;
        }

    }
}
