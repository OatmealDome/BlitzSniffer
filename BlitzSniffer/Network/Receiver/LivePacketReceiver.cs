using BlitzSniffer.Network.Searcher;
using SharpPcap;
using SharpPcap.LibPcap;
using SharpPcap.Npcap;

namespace BlitzSniffer.Network.Receiver
{
    public class LivePacketReceiver : PacketReceiver
    {
        private static readonly int ReadTimeout = 1;

        public LivePacketReceiver(ICaptureDevice device) : base()
        {
            Device = device;

            LibPcapLiveDevice libPcapDevice = device as LibPcapLiveDevice;
            device.Open(DeviceMode.Promiscuous, ReadTimeout);
        }

        public override void Start()
        {
            Device.Filter = "ip and udp";

            base.Start();
        }

    }
}
