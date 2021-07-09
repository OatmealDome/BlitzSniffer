using BlitzSniffer.Util;
using SharpPcap;
using SharpPcap.LibPcap;

namespace BlitzSniffer.Network.Receiver
{
    // Replays the session as fast as the CPU allows it
    public class ReplayPacketReceiver : PacketReceiver
    {
        public PosixTimeval FirstPacketTimeval
        {
            get;
            private set;
        }

        public PosixTimeval TotalLengthTimeval
        {
            get;
            private set;
        }

        public ReplayPacketReceiver(string path) : base()
        {
            // This is a terrible hack. We need the first packet to obtain Timeval, but
            // there is no way to peek the first packet or rewind a CaptureFileReaderDevice.
            // So, we make another device for temporary usage and use that to read the 
            // capture's first packet, allowing us to leave the original device alone.
            ICaptureDevice temporaryDevice = new CaptureFileReaderDevice(path);

            RawCapture firstPacket = temporaryDevice.GetNextPacket();
            FirstPacketTimeval = firstPacket.Timeval;

            RawCapture packet;
            PosixTimeval lastTimeval = FirstPacketTimeval;

            while ((packet = temporaryDevice.GetNextPacket()) != null)
            {
                lastTimeval = packet.Timeval;
            }

            TotalLengthTimeval = lastTimeval.Subtract(FirstPacketTimeval);

            temporaryDevice.Close();

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
