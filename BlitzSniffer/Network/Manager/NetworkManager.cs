using BlitzSniffer.Game.Tracker;
using BlitzSniffer.Game.Tracker.Station;
using BlitzSniffer.Network.Netcode;
using BlitzSniffer.Network.Netcode.Clone;
using BlitzSniffer.Network.RealTimeReplay;
using BlitzSniffer.Network.Receiver;
using BlitzSniffer.Network.Searcher;
using BlitzSniffer.Util;
using NintendoNetcode.Pia;
using PacketDotNet;
using SharpPcap;
using System;
using System.IO;
using System.Text.Json;

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
        private VideoSynchronizer VideoSync;

        private string CaptureFile;

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

            if (VideoSync != null)
            {
                VideoSync.Dispose();
                VideoSync = null;
            }

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

            CaptureFile = null;
        }

        public void LoadReplay(PiaSessionType type, string file)
        {
            Load(type, new ReplayPacketReceiver(file), true);

            CaptureFile = file;
        }

        public void LoadRealTimeReplay(PiaSessionType type, string file, ulong offset = 0)
        {
            Load(type, new RealTimeReplayPacketReceiver(file, offset), true);

            CaptureFile = file;
        }

        public void LoadRealTimeVideoSynchronizedReplay(PiaSessionType type, string file, ulong offset = 0)
        {
            VideoSynchronizedReplay replayConfig = JsonSerializer.Deserialize<VideoSynchronizedReplay>(File.ReadAllText(file));
            RealTimeReplayPacketReceiver receiver = new RealTimeReplayPacketReceiver(replayConfig.CaptureFile, offset);

            Load(type, receiver, true);

            VideoSync = new VideoSynchronizer(replayConfig);

            VideoSync.Seek(GetFirstPacketTimeReplay(), receiver.GetTemporaryReplayDevice());

            CaptureFile = replayConfig.CaptureFile;
        }

        // Controls

        public void Start()
        {
            if (Started)
            {
                return;
            }

            Receiver.Start();

            Started = true;
        }

        // Replay

        public void SeekReplay(ulong microseconds)
        {
            if (!IsRealTimeReplayLoaded())
            {
                throw new Exception("Can't seek a regular replay or live device");
            }

            RealTimeReplayPacketReceiver realTimeReceiver = Receiver as RealTimeReplayPacketReceiver;
            ICaptureDevice device = realTimeReceiver.GetTemporaryReplayDevice();

            PosixTimeval equivalentTimeval = (microseconds + realTimeReceiver.FirstPacketTimeval.ToTotalMicroseconds()).ToPosixTimeval();
            PosixTimeval targetTimeval;
            
            // Find first packet after the equivalent timeval
            do
            {
                RawCapture capture = device.GetNextPacket();

                if (capture == null)
                {
                    throw new Exception("Can't find target timeval for seek target");
                }

                targetTimeval = capture.Timeval;
            }
            while (targetTimeval < equivalentTimeval);

            realTimeReceiver.Seek(equivalentTimeval);

            device.Close();

            if (VideoSync != null)
            {
                VideoSync.Seek(equivalentTimeval, device);
            }

            // Reset game netcode handlers
            if (Session != null)
            {
                Session.Dispose();
                Session = new PiaSession(SessionType);
            }

            GameSession.Instance.Reset();

            GameSession.Instance.StationTracker.Reset();

            CloneHolder.Instance.Reset();

            // Restart the receiver
            realTimeReceiver.Start();
        }

        public PosixTimeval GetFirstPacketTimeReplay()
        {
            if (!IsReplayLoaded())
            {
                throw new Exception("Can't get first packet time of non-replay device");
            }

            return (Receiver as ReplayPacketReceiver).FirstPacketTimeval;
        }

        public PosixTimeval GetTotalReplayLength()
        {
            if (!IsReplayLoaded())
            {
                throw new Exception("Can't get length of non-replay device");
            }

            return (Receiver as ReplayPacketReceiver).TotalLengthTimeval;
        }

        // Utility

        public bool IsLiveLoaded()
        {
            return Receiver is LivePacketReceiver;
        }

        public bool IsReplayLoaded()
        {
            return Receiver is ReplayPacketReceiver;
        }

        public bool IsRealTimeReplayLoaded()
        {
            return Receiver is RealTimeReplayPacketReceiver;
        }

    }
}
