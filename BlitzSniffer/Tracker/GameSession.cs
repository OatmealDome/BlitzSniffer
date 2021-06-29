using Blitz.Cmn.Def;
using BlitzSniffer.Clone;
using BlitzSniffer.Event;
using BlitzSniffer.Event.Setup;
using BlitzSniffer.Tracker.Coop;
using BlitzSniffer.Tracker.Player;
using BlitzSniffer.Tracker.Station;
using BlitzSniffer.Tracker.Versus;
using BlitzSniffer.Tracker.Versus.Paint;
using BlitzSniffer.Tracker.Versus.VArea;
using BlitzSniffer.Tracker.Versus.VClam;
using BlitzSniffer.Tracker.Versus.VGoal;
using BlitzSniffer.Tracker.Versus.VLift;
using BlitzSniffer.Util;
using Nintendo.Sead;
using SKM.V3;
using Syroot.BinaryData;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static BlitzSniffer.Clone.CloneHolder;

namespace BlitzSniffer.Tracker
{
    class GameSession
    {
        // Increment this if additional synchronization is needed before SetupEvent can fire!
        private static readonly int SETUP_NECESSARY_SIGNALS = 2;

        public static GameSession Instance
        {
            get;
            private set;
        }

        public StationTracker StationTracker
        {
            get;
            private set;
        }

        public PlayerTracker PlayerTracker
        {
            get;
            private set;
        }

        public GameStateTracker GameStateTracker
        {
            get;
            private set;
        }

        public uint ElapsedTicks
        {
            get;
            set;
        }

        private Task SetupEventTask
        {
            get;
            set;
        }

        private CancellationTokenSource SetupEventCts
        {
            get;
            set;
        }

        private CountdownEvent SetupEventCountdown
        {
            get;
            set;
        }

        public bool IsSetup
        {
            get;
            private set;
        }

        public bool IsCoop
        {
            get
            {
                return GameStateTracker is CoopGameStateTracker;
            }
        }

        private bool ClockReady;
        private uint StartClock;
        private uint CurrentClock;

        public event CloneChangedEventHandler InGameCloneChanged;

        public delegate void GameTickedHandler(object sender, GameTickedEventArgs args);
        public event GameTickedHandler GameTicked;

        private GameSession()
        {
            PlayerTracker = null;
            StationTracker = new StationTracker(); 
            GameStateTracker = null;

            SetupEventTask = null;
            SetupEventCts = new CancellationTokenSource();
            SetupEventCountdown = new CountdownEvent(SETUP_NECESSARY_SIGNALS);

            CloneHolder holder = CloneHolder.Instance;
            holder.CloneChanged += HandleSeqEventSetting;
            holder.CloneChanged += HandleSystemEvent;
            holder.CloneChanged += HandleCloneChanged;

            holder.ClockChanged += HandleClockChanged;

            holder.RegisterClone(2);
            holder.RegisterClone(100);
        }

        public static void Initialize()
        {
            Instance = new GameSession();
        }

        public void Reset()
        {
            if (SetupEventTask != null && SetupEventTask.Status == TaskStatus.Running)
            {
                SetupEventCts.Cancel();

                SetupEventTask.Wait();
            }

            SetupEventCountdown.Reset();

            if (PlayerTracker != null)
            {
                PlayerTracker.Dispose();
            }
            
            PlayerTracker = new PlayerTracker();

            if (GameStateTracker != null)
            {
                GameStateTracker.Dispose();
                GameStateTracker = null;
            }

            if (IsSetup)
            {
                EventTracker.Instance.AddEvent(new SessionResetEvent());
            }

            IsSetup = false;
            ElapsedTicks = 0;
            ClockReady = false;
            StartClock = 0;
            CurrentClock = 0;

            SetupEventTask = Task.Run(FireSetupEvent);
        }

        public void SignalSetupReady()
        {
            SetupEventCountdown.Signal();
        }

        private void FireSetupEvent()
        {
            try
            {
                SetupEventCountdown.Wait(SetupEventCts.Token);

                IsSetup = true;

                if (GameStateTracker is VersusGameStateTracker)
                {
                    EventTracker.Instance.AddEvent(new SetupVersusEvent());
                }
                else if (GameStateTracker is CoopGameStateTracker)
                {
                    EventTracker.Instance.AddEvent(new SetupCoopEvent());
                }
                else
                {
                    throw new SnifferException("No setup event for GameStateTracker");
                }
            }
            catch (OperationCanceledException)
            {
                ;
            }
        }

        private void HandleSeqEventSetting(object sender, CloneChangedEventArgs args)
        {
            if (args.CloneId != 2 || args.ElementId != 5)
            {
                return;
            }

            if (GameStateTracker != null)
            {
                return;
            }

            using (MemoryStream stream = new MemoryStream(args.Data))
            using (BinaryDataReader reader = new BinaryDataReader(stream))
            {
                reader.ByteOrder = ByteOrder.LittleEndian;

                bool isCoop = reader.ReadByte() == 0x1;

                reader.Seek(1); // unknown

                ushort stage = reader.ReadUInt16();

                // TODO verify these
                reader.Seek(52, SeekOrigin.Begin);
                Color4f alpha = new Color4f(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), 1.0f);

                reader.Seek(72, SeekOrigin.Begin);
                Color4f bravo = new Color4f(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), 1.0f);

                if (!isCoop)
                {
                    reader.Seek(14, SeekOrigin.Begin);

                    uint teamBits = reader.ReadUInt32();
                    PlayerTracker.SetTeamBits(teamBits);

                    reader.Seek(4, SeekOrigin.Begin);

                    VersusRule rule = (VersusRule)reader.ReadUInt16();
                    switch (rule)
                    {
                        case VersusRule.Pnt:
                            GameStateTracker = new PaintVersusGameStateTracker(stage, alpha, bravo);
                            break;
                        case VersusRule.Vgl:
                            GameStateTracker = new VGoalVersusGameStateTracker(stage, alpha, bravo);
                            break;
                        case VersusRule.Var:
                            GameStateTracker = new VAreaVersusGameStateTracker(stage, alpha, bravo);
                            break;
                        case VersusRule.Vlf:
                            GameStateTracker = new VLiftVersusGameStateTracker(stage, alpha, bravo);
                            break;
                        case VersusRule.Vcl:
                            GameStateTracker = new VClamVersusGameStateTracker(stage, alpha, bravo);
                            break;
                        default:
                            GameStateTracker = new GenericVersusGameStateTracker(stage, rule, alpha, bravo);
                            break;
                    }
                }
                else
                {
                    GameStateTracker = new CoopGameStateTracker(stage, alpha, bravo);
                }
            }
        }

        private void HandleSystemEvent(object sender, CloneChangedEventArgs args)
        {
            if (args.CloneId != 100 || args.ElementId != 1)
            {
                return;
            }

            using (MemoryStream stream = new MemoryStream(args.Data))
            using (BinaryDataReader reader = new BinaryDataReader(stream))
            {
                reader.ByteOrder = ByteOrder.LittleEndian;

                uint eventType = reader.ReadUInt32();

                if (eventType == 0) // Synchronize game start using clone clock
                {
#if !DEBUG
                    LicenseKey licenseKey = LicenseTools.Instance.GetLoadedLicense();
                    if (licenseKey == null || !licenseKey.HasValidSignature(LicenseTools.CRYPTOLENS_PUBLIC_KEY).IsValid())
                    {
                        return;
                    }
#endif

                    StartClock = reader.ReadUInt32();
                    // int mapId = reader.ReadInt32();
                    // uint unknown = reader.ReadUInt32();

                    ClockReady = true;
                }
            }
        }

        private void HandleCloneChanged(object sender, CloneChangedEventArgs args)
        {
            if (!IsSetup || !ClockReady)
            {
                return;
            }

            if (CurrentClock < StartClock)
            {
                return;
            }

            InGameCloneChanged?.Invoke(sender, args);
        }

        private void HandleClockChanged(object sender, ClockChangedEventArgs args)
        {
            if (!ClockReady)
            {
                return;
            }

            if (CurrentClock >= args.Clock)
            {
                return;
            }

            // Only tick if we've passed the start
            if (args.Clock >= StartClock)
            {
                uint startFrames = Blitz.Lp.Utl.CloneClockToGameFrame(StartClock);
                uint currentFrames = Blitz.Lp.Utl.CloneClockToGameFrame(args.Clock);
                uint totalFrames = currentFrames - startFrames;

                for (uint i = 0; i < totalFrames - ElapsedTicks; i++)
                {
                    ElapsedTicks++;

                    GameTicked(this, new GameTickedEventArgs(ElapsedTicks));
                }
            }

            CurrentClock = args.Clock;
        }

    }
}
