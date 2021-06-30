using PacketDotNet;
using SharpPcap;
using Syroot.BinaryData;
using System.IO;

namespace BlitzSniffer.Network.Searcher
{
    class OnlineReplaySessionSearcher : SessionSearcher
    {
        public static readonly uint PACKET_MAGIC = 0x534A3445;

        private ICaptureDevice Device;

        private OnlineReplaySessionSearcher(ICaptureDevice device) : base()
        {
            Device = device;

            Device.OnPacketArrival += OnPacketArrival;
        }

        public static void Initialize(ICaptureDevice device)
        {
            Instance = new OnlineReplaySessionSearcher(device);
        }

        public override void Dispose()
        {
            Device.OnPacketArrival -= OnPacketArrival;
        }

        protected virtual void OnPacketArrival(object sender, CaptureEventArgs e)
        {
            Packet packet = Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);

            UdpPacket udpPacket = packet.Extract<UdpPacket>();
            if (udpPacket.DestinationPort != 13390)
            {
                return;
            }

            using (MemoryStream memoryStream = new MemoryStream(udpPacket.PayloadData))
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
