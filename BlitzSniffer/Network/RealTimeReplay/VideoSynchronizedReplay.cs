namespace BlitzSniffer.Network.RealTimeReplay
{
    class VideoSynchronizedReplay
    {
        public string VideoFile
        {
            get;
            set;
        }

        public string CaptureFile
        {
            get;
            set;
        }

        public long VideoSyncTime
        {
            get;
            set;
        }

        public ulong CaptureSyncSeconds
        {
            get;
            set;
        }

        public ulong CaptureSyncMicroSeconds
        {
            get;
            set;
        }

    }
}
