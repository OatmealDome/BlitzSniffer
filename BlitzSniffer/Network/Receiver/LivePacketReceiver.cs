using BlitzSniffer.Config;
using NintendoNetcode.Pia;
using SharpPcap;
using SharpPcap.LibPcap;
using SharpPcap.Npcap;

namespace BlitzSniffer.Network.Receiver
{
    public class LivePacketReceiver : PacketReceiver
    {
        private static readonly int ReadTimeout = 1;

        public LivePacketReceiver(PiaSessionType sessionType, ICaptureDevice device) : base(sessionType)
        {
            Device = device;

            NpcapDevice npcapDevice = device as NpcapDevice;
            if (npcapDevice != null)
            {
                npcapDevice.Open(OpenFlags.DataTransferUdp, ReadTimeout);
            }
            else
            {
                LibPcapLiveDevice libPcapDevice = device as LibPcapLiveDevice;
                device.Open(DeviceMode.Promiscuous, ReadTimeout);
            }
        }

        public override void Start(string outputFile = null)
        {
            if (SessionType == PiaSessionType.Lan)
            {
                Device.Filter = "ip and udp and (udp portrange 49150-49160 or udp port 30000)";
            }
            else
            {
                Device.Filter = $"ip and udp and ip host {SnifferConfig.Instance.Snicom.IpAddress}";
            }

            base.Start(outputFile);
        }

    }
}
