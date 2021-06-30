using System;

namespace BlitzSniffer.Network.Netcode.Clone
{
    public class CloneChangedEventArgs : EventArgs
    {
        public uint CloneId
        {
            get;
            set;
        }

        public uint ElementId
        {
            get;
            set;
        }

        public byte[] Data
        {
            get;
            set;
        }

        public bool IsDataSimilar
        {
            get;
            set;
        }

        public ulong SourceStationId
        {
            get;
            set;
        }

        public CloneChangedEventArgs(uint cid, uint eid, byte[] data, bool similar, ulong ssid)
        {
            CloneId = cid;
            ElementId = eid;
            Data = data;
            IsDataSimilar = similar;
            SourceStationId = ssid;
        }

    }
}
