using BlitzSniffer.Network.Manager;
using BlitzSniffer.Util;
using LibVLCSharp.Shared;
using SharpPcap;
using SharpPcap.LibPcap;
using System;

namespace BlitzSniffer.Network.RealTimeReplay
{
    class VideoSynchronizer : IDisposable
    {
        private VideoSynchronizedReplay Config;

        private LibVLC Vlc;
        private Media Media;
        private MediaPlayer Player;

        private PosixTimeval CaptureSyncPoint;
        private PosixTimeval CaptureStartSyncPoint;
        private long VideoStartSyncPoint;

        private bool HasDoneInitialPause;

        public VideoSynchronizer(VideoSynchronizedReplay config)
        {
            Config = config;

            Vlc = new LibVLC();
            Media = new Media(Vlc, config.VideoFile);
            Player = new MediaPlayer(Media);

            CaptureSyncPoint = new PosixTimeval(config.CaptureSyncSeconds, config.CaptureSyncMicroSeconds);

            Seek(NetworkManager.Instance.GetFirstPacketTimeReplay());

            HasDoneInitialPause = false;

            Player.Playing += HandlePlayerPlaying;
            Player.Play();

            NetworkManager.Instance.PacketReceived += HandlePacketReceived;
        }

        public void Dispose()
        {
            NetworkManager.Instance.PacketReceived -= HandlePacketReceived;

            Player.Dispose();
            Media.Dispose();
            Vlc.Dispose();
        }

        private void HandlePacketReceived(object sender, PacketReceivedEventArgs args)
        {
            if (Player.IsPlaying)
            {
                return;
            }

            if (CaptureStartSyncPoint <= args.RawCapture.Timeval)
            {
                Player.Play();
                Player.Time = VideoStartSyncPoint;
            }
        }

        private void HandlePlayerPlaying(object sender, EventArgs e)
        {
            if (HasDoneInitialPause)
            {
                return;
            }

            Player.Pause();

            // We can only fetch the media size after Play() is first called.
            uint mediaWidth = 0;
            uint mediaHeight = 0;
            bool result = Player.Size(0, ref mediaWidth, ref mediaHeight);

            if (!result)
            {
                throw new Exception("Unable to fetch media size");
            }

            // Calculate scale for 960x540 (assuming 16:9 video)
            Player.Scale = 960.0f / mediaWidth;

            HasDoneInitialPause = true;
        }

        public void Seek(PosixTimeval targetTimeval)
        {
            ICaptureDevice device = new CaptureFileReaderDevice(Config.CaptureFile);

            // Seek to this packet in the CaptureFileReaderDevice
            while (device.GetNextPacket().Timeval != targetTimeval)
            {
                ;
            }

            PosixTimeval capturePoint = null;
            long videoPoint;

            do
            {
                if (capturePoint == null)
                {
                    capturePoint = targetTimeval;
                }
                else
                {
                    RawCapture nextPacket = device.GetNextPacket();

                    if (nextPacket == null)
                    {
                        throw new Exception("Can't seek to this point");
                    }

                    capturePoint = nextPacket.Timeval;
                }

                videoPoint = (Config.VideoSyncTime * 1000) - ((long)CaptureSyncPoint.ToTotalMicroseconds() - (long)capturePoint.ToTotalMicroseconds());
            }
            while (videoPoint < 0);

            CaptureStartSyncPoint = capturePoint;
            VideoStartSyncPoint = videoPoint / 1000;

            device.Close();
        }

    }
}
