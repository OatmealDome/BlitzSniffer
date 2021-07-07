using PacketDotNet;
using SharpPcap;

namespace BlitzSniffer.Network.Manager
{
    class PacketReceivedEventArgs
    {
        public RawCapture RawCapture
        {
            get;
            set;
        }

        public IPPacket IPPacket
        {
            get;
            set;
        }

        public UdpPacket UdpPacket
        {
            get;
            set;
        }

        public PacketReceivedEventArgs(RawCapture rawCapture, IPPacket ipPacket, UdpPacket udpPacket)
        {
            RawCapture = rawCapture;
            IPPacket = ipPacket;
            UdpPacket = udpPacket;
        }
        
    }
}