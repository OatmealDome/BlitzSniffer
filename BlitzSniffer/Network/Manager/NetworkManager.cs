using BlitzSniffer.Network.Netcode;
using BlitzSniffer.Network.Receiver;
using BlitzSniffer.Network.Searcher;
using NintendoNetcode.Pia;
using PacketDotNet;
using SharpPcap;

namespace BlitzSniffer.Network.Manager
{
    class NetworkManager
    {
        private static NetworkManager _Instance;

        public static NetworkManager Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new NetworkManager();
                }

                return _Instance;
            }
        }

        private PiaSessionType SessionType;
        private PacketReceiver Receiver;
        private SessionSearcher Searcher;
        private PiaSession Session;
        private LiveWriteOut WriteOut;

        private bool Started;

        public delegate void PacketReceivedHandler(object sender, PacketReceivedEventArgs args);
        public event PacketReceivedHandler PacketReceived;

        public delegate void SessionDataFoundHandler(object sender, SessionDataFoundEventArgs args);
        public event SessionDataFoundHandler SessionDataFound; 

        private NetworkManager()
        {
            Started = false;
        }

        // Data intake

        public void HandlePacketReceivedFromHandler(CaptureEventArgs e)
        {
            Packet packet = Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);

            UdpPacket udpPacket = packet.Extract<UdpPacket>();
            IPPacket ipPacket = packet.Extract<IPPacket>();

            PacketReceived?.Invoke(this, new PacketReceivedEventArgs(e.Packet, ipPacket, udpPacket));
        }

        public void HandleSessionDataReceived(SessionDataFoundEventArgs e)
        {
            SessionDataFound?.Invoke(this, e);
        }

        // Load / Reset

        public void Reset()
        {
            Started = false;

            if (Session != null)
            {
                Session.Dispose();
                Session = null;
            }

            if (Searcher != null)
            {
                Searcher.Dispose();
                Searcher = null;
            }

            if (WriteOut != null)
            {
                WriteOut.Dispose();
                WriteOut = null;
            }

            if (Receiver != null)
            {
                Receiver.Dispose();
                Receiver = null;
            }
        }
        
        private void Load(PiaSessionType type, PacketReceiver receiver, bool replay = false, string liveOutFile = null)
        {
            Reset();

            SessionType = type;
            Receiver = receiver;

            if (SessionType == PiaSessionType.Inet)
            {
                if (replay)
                {
                    Searcher = new OnlineReplaySessionSearcher();
                }
                else
                {
                    Searcher = new SnicomSessionSearcher();
                }
            }
            else
            {
                Searcher = new LanSessionSearcher(Receiver.GetDevice());
            }

            Session = new PiaSession(SessionType);

            if (!replay)
            {
                WriteOut = new LiveWriteOut(liveOutFile);
            }
        }

        public void LoadLive(PiaSessionType type, ICaptureDevice device, string liveOutFile)
        {
            Load(type, new LivePacketReceiver(device), liveOutFile: liveOutFile);
        }

        public void LoadReplay(PiaSessionType type, string file)
        {
            Load(type, new ReplayPacketReceiver(file), true);
        }

        public void LoadRealTimeReplay(PiaSessionType type, string file, int offset = 0)
        {
            Load(type, new RealTimeReplayPacketReceiver(file, offset), true);
        }
        
        // Start

        public void Start()
        {
            if (Started)
            {
                return;
            }

            Receiver.Start();

            Started = true;
        }

    }
}
