using Blitz.Cmn.Def;
using BlitzSniffer.Network.Netcode.Clone;
using BlitzSniffer.Event;
using BlitzSniffer.Event.Versus;
using Nintendo.Sead;
using Syroot.BinaryData;
using System.IO;

namespace BlitzSniffer.Tracker.Versus
{
    abstract class GachiVersusGameStateTracker : VersusGameStateTracker
    {
        public abstract bool HasPenalties
        {
            get;
        }

        public uint AlphaScore
        {
            get;
            private set;
        }

        public uint BravoScore
        {
            get;
            private set;
        }

        public uint AlphaPenalty
        {
            get;
            private set;
        }

        public uint BravoPenalty
        {
            get;
            private set;
        }

        public bool InOvertime
        {
            get;
            private set;
        }

        public bool InOvertimeTimeout
        {
            get;
            private set;
        }

        public bool GameFinished
        {
            get;
            private set;
        }

        public GachiVersusGameStateTracker(ushort stage, Color4f alpha, Color4f bravo) : base(stage, alpha, bravo)
        {
            AlphaScore = 0;
            BravoScore = 0;
            AlphaPenalty = 0;
            BravoPenalty = 0;
            InOvertime = false;
            InOvertimeTimeout = false;
            GameFinished = false;

            CloneHolder holder = CloneHolder.Instance;
            holder.RegisterClone(100);

            GameSession session = GameSession.Instance;
            session.InGameCloneChanged += HandleSystemEventInternal;
        }

        public override void Dispose()
        {
            GameSession session = GameSession.Instance;
            session.InGameCloneChanged -= HandleSystemEventInternal;
        }

        protected void UpdateScores(uint alphaScore, uint bravoScore, uint alphaPenalty = 0, uint bravoPenalty = 0)
        {
            if (GameFinished)
            {
                return;
            }

            if (alphaScore != AlphaScore || bravoScore != BravoScore || alphaPenalty != AlphaPenalty || bravoPenalty != BravoPenalty)
            {
                AlphaScore = alphaScore;
                BravoScore = bravoScore;
                AlphaPenalty = alphaPenalty;
                BravoPenalty = bravoPenalty;

                EventTracker.Instance.AddEvent(new GachiScoreUpdateEvent()
                {
                    AlphaScore = AlphaScore,
                    BravoScore = BravoScore,
                    AlphaPenalty = AlphaPenalty,
                    BravoPenalty = BravoPenalty
                });
            }
        }

        protected void HandleFinishEvent(uint alphaScore, uint bravoScore)
        {
            if (GameFinished)
            {
                return;
            }

            EventTracker.Instance.AddEvent(new GachiFinishEvent()
            {
                AlphaScore = alphaScore,
                BravoScore = bravoScore
            });

            GameFinished = true;
        }

        protected void SetOvertimeTimeout(int timeout)
        {
            if (!InOvertime || GameFinished)
            {
                return;
            }

            if (timeout == -1)
            {
                InOvertimeTimeout = false;
            }
            else
            {
                InOvertimeTimeout = true;
            }

            EventTracker.Instance.AddEvent(new GachiOvertimeTimeoutUpdateEvent()
            {
                Length = timeout
            });
        }

        protected Team GetLeadingTeam()
        {
            if (AlphaScore > BravoScore)
            {
                return Team.Alpha; 
            }
            else if (BravoScore > AlphaScore)
            {
                return Team.Bravo;
            }
            else
            {
                return Team.Neutral;
            }
        }

        protected Team GetTrailingTeam()
        {
            if (AlphaScore > BravoScore)
            {
                return Team.Bravo;
            }
            else if (BravoScore > AlphaScore)
            {
                return Team.Alpha;
            }
            else
            {
                return Team.Neutral;
            }
        }

        private void HandleSystemEventInternal(object sender, CloneChangedEventArgs args)
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

                if (eventType == 7) // Overtime start
                {
                    if (InOvertime)
                    {
                        return;
                    }

                    InOvertime = true;

                    EventTracker.Instance.AddEvent(new GachiOvertimeStartEvent());
                }
                
                HandleSystemEvent(eventType, reader);
            }
        }

        protected abstract void HandleSystemEvent(uint eventType, BinaryDataReader reader);

    }
}
