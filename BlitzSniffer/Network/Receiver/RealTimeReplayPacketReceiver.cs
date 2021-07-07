using BlitzSniffer.Network.Searcher;
using BlitzSniffer.Util;
using NintendoNetcode.Pia;
using SharpPcap;
using SharpPcap.LibPcap;
using System.Threading;

namespace BlitzSniffer.Network.Receiver
{
    // Replays the session in real-time
    class RealTimeReplayPacketReceiver : ReplayPacketReceiver
    {
        private string ReplayPath
        {
            get;
            set;
        }

        private MicroTimer Timer
        {
            get;
            set;
        }

        private int RealTimeStartOffset
        {
            get;
            set;
        }

        private PosixTimeval Timeval
        {
            get;
            set;
        } = null;

        private PosixTimeval WaitForTimeval
        {
            get;
            set;
        } = null;

        private ManualResetEvent ContinueSignal
        {
            get;
            set;
        }

        private object TimevalLock = new object();

        public RealTimeReplayPacketReceiver(string path, int offset) : base(path)
        {
            ReplayPath = path;
            RealTimeStartOffset = offset;
            ContinueSignal = new ManualResetEvent(false);

            Timer = new MicroTimer(1000);
            Timer.MicroTimerElapsed += TimerElapsed;
        }

        public RealTimeReplayPacketReceiver(string path) : this(path, 0)
        {

        }

        public override void Start()
        {
            lock (TimevalLock)
            {
                // This is a terrible hack. We need the first packet to obtain Timeval, but
                // there is no way to peek the first packet or rewind a CaptureFileReaderDevice.
                // So, we make another device for temporary usage and use that to read the 
                // capture's first packet, allowing us to leave the original device alone.
                ICaptureDevice temporaryDevice = new CaptureFileReaderDevice(ReplayPath);

                RawCapture firstPacket = temporaryDevice.GetNextPacket();
                Timeval = firstPacket.Timeval;

                // May take a few moments to catch up, but it'll get there eventually
                Timeval.Seconds += (ulong)RealTimeStartOffset;

                temporaryDevice.Close();
            }

            Timer.Start();

            base.Start();
        }

        public override void Dispose()
        {
            base.Dispose();

            if (!Timer.StopAndWait(1000))
            {
                Timer.Abort();
            }
            
            ContinueSignal.Dispose();
        }

        private void TimerElapsed(object sender, MicroTimerEventArgs e)
        {
            lock (TimevalLock)
            {
                Timeval.MicroSeconds += 1000;
                if (Timeval.MicroSeconds >= 1000000)
                {
                    Timeval.Seconds++;
                    Timeval.MicroSeconds = Timeval.MicroSeconds % 1000000;
                }

                if (WaitForTimeval != null)
                {
                    if (WaitForTimeval < Timeval)
                    {
                        ContinueSignal.Set();
                    }
                }
            }
        }

        protected override void OnPacketArrival(object sender, CaptureEventArgs e)
        {
            PosixTimeval packetTimeval = e.Packet.Timeval;

            bool shouldWait = false;

            if (packetTimeval > Timeval)
            {
                shouldWait = true;

                lock (TimevalLock)
                {
                    WaitForTimeval = packetTimeval;
                }
            }

            if (shouldWait)
            {
                ContinueSignal.WaitOne();

                lock (TimevalLock)
                {
                    WaitForTimeval = null;

                    ContinueSignal.Reset();
                }
            }

            base.OnPacketArrival(sender, e);
        }

    }
}
