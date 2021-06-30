using Blitz.Cmn.Def;
using BlitzSniffer.Network.Netcode.Clone;
using BlitzSniffer.Event;
using BlitzSniffer.Event.Versus;
using BlitzSniffer.Util;
using Nintendo.Sead;
using Serilog;
using Syroot.BinaryData;
using System;
using System.Collections.Generic;
using System.IO;

namespace BlitzSniffer.Tracker.Versus.Paint
{
    class PaintVersusGameStateTracker : VersusGameStateTracker
    {
        public override VersusRule Rule => VersusRule.Pnt;

        private Dictionary<uint, Tuple<uint, uint>> ResultTuples;

        public PaintVersusGameStateTracker(ushort stage, Color4f alpha, Color4f bravo) : base(stage, alpha, bravo)
        {
            ResultTuples = new Dictionary<uint, Tuple<uint, uint>>();

            CloneHolder holder = CloneHolder.Instance;

            for (uint i = 0; i < 10; i++)
            {
                holder.RegisterClone(i + 101);
            }

            GameSession session = GameSession.Instance;
            session.InGameCloneChanged += HandlePaintResult;
        }

        public override void Dispose()
        {
            GameSession session = GameSession.Instance;
            session.InGameCloneChanged -= HandlePaintResult;
        }

        private void HandlePaintResult(object sender, CloneChangedEventArgs args)
        {
            uint playerId = args.CloneId - 101;
            if (playerId >= 10)
            {
                return;
            }

            // This is the only element ID, but check just in case
            if (args.ElementId != 0)
            {
                return;
            }

            if (ResultTuples.ContainsKey(playerId))
            {
                return;
            }

            using (MemoryStream stream = new MemoryStream(args.Data))
            using (BinaryDataReader reader = new BinaryDataReader(stream))
            {
                reader.ByteOrder = ByteOrder.LittleEndian;

                uint playerTotal = reader.ReadUInt32();
                uint alphaTotal = reader.ReadUInt32();
                uint bravoTotal = reader.ReadUInt32();

                ResultTuples.Add(playerId, new Tuple<uint, uint>(alphaTotal, bravoTotal));
            }

            if (ResultTuples.Count == GameSession.Instance.PlayerTracker.GetAcivePlayers())
            {
                // Average out all totals
                uint alphaTotals = 0;
                uint bravoTotals = 0;

                foreach (Tuple<uint, uint> tuple in ResultTuples.Values)
                {
                    alphaTotals += tuple.Item1;
                    bravoTotals += tuple.Item2;
                }

                float averageAlpha = (float)alphaTotals / ResultTuples.Count;
                float averageBravo = (float)bravoTotals / ResultTuples.Count;


                float pixelToPoint(float pixel)
                {
                    return (pixel * 0.015625f) / 3.3f;
                }

                EventTracker.Instance.AddEvent(new PaintFinishEvent()
                {
                    AlphaPoints = pixelToPoint(averageAlpha),
                    BravoPoints = pixelToPoint(averageBravo)
                });
            }
        }

    }
}
