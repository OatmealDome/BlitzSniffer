using Blitz.Cmn.Def;
using BlitzSniffer.Network.Netcode.Clone;
using BlitzSniffer.Game.Event;
using BlitzSniffer.Game.Event.Versus;
using BlitzSniffer.Game.Event.Versus.VLift;
using Nintendo.Sead;
using Syroot.BinaryData;
using System.Collections.Generic;
using System.IO;
using BlitzSniffer.Util;
using Serilog;

namespace BlitzSniffer.Game.Tracker.Versus.VLift
{
    class VLiftVersusGameStateTracker : GachiVersusGameStateTracker
    {
        private static readonly ILogger LogContext = LogUtil.GetLogger("VLift");
        public override VersusRule Rule => VersusRule.Vlf;

        public override bool HasPenalties => false;

        private VLiftRail Rail;

        public VLiftVersusGameStateTracker(ushort stage, Color4f alpha, Color4f bravo, Color4f neutral) : base(stage, alpha, bravo, neutral)
        {
            Rail = new VLiftRail();

            // Find the VLift rail
            List<dynamic> railPoints = null;
            foreach (Dictionary<string, dynamic> rail in StageLayout["Rails"])
            {
                if (rail["UnitConfigName"] == "Rail_VLift")
                {
                    railPoints = rail["RailPoints"];
                    break;
                }
            }

            if (railPoints == null)
            {
                throw new SnifferException("Cannot find VLift rail");
            }

            // Find the starting rail index
            int startIdx = (railPoints.Count - 1) / 2;

            // Get all the checkpoints
            for (int i = startIdx; i < railPoints.Count; i++)
            {
                Dictionary<string, dynamic> railPoint = railPoints[i];
                uint checkpointHp = (uint)railPoint["CheckPointHP"];

                if (checkpointHp != 0)
                {
                    Rail.AddCheckpoint(checkpointHp);
                }
            }

            CloneHolder holder = CloneHolder.Instance;
            holder.RegisterClone(121);

            GameSession session = GameSession.Instance;
            session.InGameCloneChanged += HandleVLiftState;
        }

        public override void Dispose()
        {
            base.Dispose();

            GameSession session = GameSession.Instance;
            session.InGameCloneChanged -= HandleVLiftState;
        }

        public List<List<uint>> BuildCheckpointListForSetup()
        {
            return new List<List<uint>>()
            {
                Rail.GetCheckpointsForSetup(Team.Alpha),
                Rail.GetCheckpointsForSetup(Team.Bravo)
            };
        }

        public void UpdateOvertimeTimeoutState()
        {
            if (!InOvertime)
            {
                return;
            }

            int playersOnVLift = GameSession.Instance.PlayerTracker.GetPlayersOnVLift();
            if (InOvertimeTimeout && playersOnVLift > 0)
            {
                SetOvertimeTimeout(-1);
            }
            else if (!InOvertimeTimeout && playersOnVLift == 0)
            {
                SetOvertimeTimeout(300);
            }
        }

        private void HandleVLiftState(object sender, CloneChangedEventArgs args)
        {
            if (args.CloneId != 121 || args.ElementId != 4)
            {
                return;
            }

            using (MemoryStream stream = new MemoryStream(args.Data))
            using (BinaryDataReader reader = new BinaryDataReader(stream))
            {
                reader.ByteOrder = ByteOrder.LittleEndian;

                //reader.Seek(8);
                ulong data = reader.ReadUInt64();
                LogContext.Information("data {Data}", data.ToString("x"));

                uint ReadScore()
                {
                    return (uint)(reader.ReadSingle() * 100);
                }

                reader.Seek(8); // skip best VLift position
                uint alphaBestScore = ReadScore();
                uint bravoBestScore = ReadScore();

                UpdateScores(alphaBestScore, bravoBestScore);

                reader.Seek(8); // skip best total checkpoint HP since we keep track of this internally
                uint alphaCurrentTotalCheckpointHp = (uint)reader.ReadSingle();
                uint bravoCurrentTotalCheckpointHp = (uint)reader.ReadSingle();

                Rail.UpdateCheckpoints(alphaCurrentTotalCheckpointHp, Team.Alpha);
                Rail.UpdateCheckpoints(bravoCurrentTotalCheckpointHp, Team.Bravo);
            }
        }

        protected override void HandleSystemEvent(uint eventType, BinaryDataReader reader)
        {
            if (eventType == 1) // VLift finish
            {
                // These bytes are the "result left count", so convert them to score
                HandleFinishEvent((uint)100 - reader.ReadByte(), (uint)100 - reader.ReadByte());
            }
        }

    }
}
