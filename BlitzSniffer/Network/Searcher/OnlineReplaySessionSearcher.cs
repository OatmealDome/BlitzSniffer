using BlitzSniffer.Network.Manager;
using Syroot.BinaryData;
using System.IO;

namespace BlitzSniffer.Network.Searcher
{
    class OnlineReplaySessionSearcher : SessionSearcher
    {
        public static readonly uint PACKET_MAGIC = 0x534A3445;

        public OnlineReplaySessionSearcher() : base()
        {
            NetworkManager.Instance.PacketReceived += HandlePacketReceived;
        }

        public override void Dispose()
        {
            NetworkManager.Instance.PacketReceived -= HandlePacketReceived;
        }

        protected virtual void HandlePacketReceived(object sender, PacketReceivedEventArgs e)
        {
            if (e.UdpPacket.DestinationPort != 13390)
            {
                return;
            }

            using (MemoryStream memoryStream = new MemoryStream(e.UdpPacket.PayloadData))
            using (BinaryDataReader reader = new BinaryDataReader(memoryStream))
            {
                reader.ByteOrder = ByteOrder.BigEndian;

                if (reader.ReadUInt32() != PACKET_MAGIC)
                {
                    return;
                }

                byte[] data;

                SessionFoundDataType dataType = (SessionFoundDataType)reader.ReadByte();
                switch (dataType)
                {
                    case SessionFoundDataType.Key:
                        data = reader.ReadBytes(16);
                        break;
                    case SessionFoundDataType.GatheringId:
                        data = reader.ReadBytes(4);
                        break;
                    default:
                        throw new SnifferException("Invalid SJ4E data type");
                }

                NotifySessionDataFound(dataType, data);
            }
        }

    }
}
