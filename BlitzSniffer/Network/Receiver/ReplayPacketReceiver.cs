using BlitzSniffer.Network.Searcher;
using NintendoNetcode.Pia;
using SharpPcap.LibPcap;

namespace BlitzSniffer.Network.Receiver
{
    // Replays the session as fast as the CPU allows it
    public class ReplayPacketReceiver : PacketReceiver
    {
        public ReplayPacketReceiver(string path) : base()
        {
            Device = new CaptureFileReaderDevice(path);
            Device.Open();
        }

        public override void Start()
        {
            Device.Filter = "ip and udp";

            base.Start();
        }

    }
}
