using BlitzSniffer.Network.Searcher;
using BlitzSniffer.Util;
using NintendoNetcode.Pia;
using SharpPcap;
using SharpPcap.LibPcap;
using System;
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

        private CancellationTokenSource TokenSource
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
            TokenSource = new CancellationTokenSource();

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
                Timeval = new PosixTimeval(FirstPacketTimeval.Seconds + (ulong)RealTimeStartOffset, FirstPacketTimeval.MicroSeconds);
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

            TokenSource.Cancel();
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
                try
                {
                    WaitHandle.WaitAny(new WaitHandle[] { TokenSource.Token.WaitHandle, ContinueSignal });
                }
                catch (ObjectDisposedException)
                {
                    ;
                }

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
