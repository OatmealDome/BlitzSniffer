using Blitz.Cmn.Def;
using BlitzSniffer.Network.Netcode.Clone;
using BlitzSniffer.Game.Event;
using BlitzSniffer.Game.Event.Versus;
using Nintendo.Sead;
using Syroot.BinaryData;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BlitzSniffer.Tracker.Versus.VArea
{
    class VAreaVersusGameStateTracker : GachiVersusGameStateTracker
    {
        private static readonly int OVERTIME_TIMEOUT_FRAMES = 600;

        public override VersusRule Rule => VersusRule.Var;

        public override bool HasPenalties => true;

        private List<VAreaPaintTargetArea> PaintTargetAreas;

        private bool HasTrailingLostControlOfAreasInOvertime;

        public VAreaVersusGameStateTracker(ushort stage, Color4f alpha, Color4f bravo) : base(stage, alpha, bravo)
        {
            PaintTargetAreas = new List<VAreaPaintTargetArea>();
            HasTrailingLostControlOfAreasInOvertime = false;

            int areaIdx = 0;
            foreach (Dictionary<string, dynamic> obj in StageLayout["Objs"])
            {
                if (obj["LayerConfigName"] != "Var")
                {
                    continue;
                }

                string unitConfigName = obj["UnitConfigName"];
                if (unitConfigName == "PaintTargetArea" || unitConfigName == "PaintTargetArea_Cylinder")
                {
                    PaintTargetAreas.Add(new VAreaPaintTargetArea(areaIdx));

                    areaIdx++;
                }
            }

            CloneHolder holder = CloneHolder.Instance;
            holder.RegisterClone(100);

            GameSession session = GameSession.Instance;
            session.InGameCloneChanged += HandleVAreaState;
        }

        public override void Dispose()
        {
            base.Dispose();

            GameSession session = GameSession.Instance;
            session.InGameCloneChanged -= HandleVAreaState;
        }

        public int GetPaintTargetAreasCount()
        {
            return PaintTargetAreas.Count;
        }

        private void HandleVAreaState(object sender, CloneChangedEventArgs args)
        {
            if (args.CloneId != 100 || args.ElementId != 2)
            {
                return;
            }

            using (MemoryStream stream = new MemoryStream(args.Data))
            using (BinaryDataReader reader = new BinaryDataReader(stream))
            {
                reader.ByteOrder = ByteOrder.LittleEndian;

                uint newAlphaScore = 100 - VAreaTicksToSeconds(reader.ReadUInt16());
                uint newBravoScore = 100 - VAreaTicksToSeconds(reader.ReadUInt16());
                uint newAlphaPenalty = VAreaTicksToSeconds(reader.ReadUInt16());
                uint newBravoPenalty = VAreaTicksToSeconds(reader.ReadUInt16());

                UpdateScores(newAlphaScore, newBravoScore, newAlphaPenalty, newBravoPenalty);

                reader.Seek(4);

                sbyte[] paintPercent = reader.ReadSBytes(4);
                byte[] unk2 = reader.ReadBytes(4);
                sbyte[] unk3 = reader.ReadSBytes(4);

                for (int i = 0; i < PaintTargetAreas.Count; i++)
                {
                    sbyte paintPercentSByte = paintPercent[i];
                    float paintPercentFloat;

                    // Convert to float - Game::PaintTargetArea::updateWallStateNet()
                    if (paintPercentSByte == -127)
                    {
                        paintPercentFloat = -1.0f;
                    }
                    else if (paintPercentSByte == 127)
                    {
                        paintPercentFloat = 1.0f;
                    }
                    else
                    {
                        paintPercentFloat = (paintPercentSByte + 127.0f + (paintPercentSByte + 127.0f)) / 254.0f + -1.0f;
                    }

                    PaintTargetAreas[i].UpdateWithState(paintPercentFloat, unk2[i], unk3[i]);
                }
            }
        }

        protected override void HandleSystemEvent(uint eventType, BinaryDataReader reader)
        {
            if (eventType == 3) // VArea change team
            {
                uint rawTeams = reader.ReadUInt32();

                for (int i = 0; i < PaintTargetAreas.Count; i++)
                {
                    uint team = ((rawTeams >> (i * 2)) & 3) - 1;
                    PaintTargetAreas[i].ChangeControl((Team)team);
                }

                if (InOvertime)
                {
                    if (PaintTargetAreas.Any(x => x.ControllingTeam != GetTrailingTeam()))
                    {
                        if (!HasTrailingLostControlOfAreasInOvertime)
                        {
                            HasTrailingLostControlOfAreasInOvertime = true;

                            SetOvertimeTimeout(OVERTIME_TIMEOUT_FRAMES);
                        }
                    }
                    else
                    {
                        HasTrailingLostControlOfAreasInOvertime = false;

                        SetOvertimeTimeout(-1);
                    }
                }
            }
            else if (eventType == 4) // VArea finish
            {
                ushort alphaTicks = reader.ReadUInt16();
                ushort bravoTicks = reader.ReadUInt16();

                uint alphaSeconds = VAreaTicksToSeconds(alphaTicks);
                uint bravoSeconds = VAreaTicksToSeconds(bravoTicks);

                // These calculations come from Game::RefereeVersusVArea::invokeEventFinish().
                //
                // The decompiled version is an absolute mess and I'm too lazy to figure out
                // what it does exactly (probably corrections for KO and if the seconds happen
                // to be equal). The below is basically a straight port of the decompiled code,
                // Hex-Rays automatic variable names and all.

                uint v14;
                if (alphaSeconds != 0)
                {
                    v14 = alphaSeconds;
                }
                else
                {
                    v14 = 1;
                }

                uint v15;
                if (alphaSeconds != 0)
                {
                    v15 = alphaSeconds - 1;
                }
                else
                {
                    v15 = 0;
                }

                uint v16;
                if (alphaSeconds != 0)
                {
                    v16 = alphaSeconds - 1;
                }
                else
                {
                    v16 = 0;
                }

                uint v17;
                if (alphaSeconds != 0)
                {
                    v17 = alphaSeconds;
                }
                else
                {
                    v17 = 1;
                }

                uint v18;
                if (alphaTicks <= bravoTicks)
                {
                    v18 = v16;
                }
                else
                {
                    v18 = v14;
                }

                uint v19;
                if (alphaTicks <= bravoTicks)
                {
                    v19 = v17;
                }
                else
                {
                    v19 = v15;
                }

                if (alphaSeconds != bravoSeconds)
                {
                    v19 = bravoSeconds;
                }

                if (alphaSeconds != bravoSeconds)
                {
                    v18 = alphaSeconds;
                }

                uint v20;
                if (v19 != 0)
                {
                    v20 = v18;
                }
                else
                {
                    v20 = 100;
                }

                uint alphaFinalScore;
                if (v18 != 0)
                {
                    alphaFinalScore = v20;
                }
                else
                {
                    alphaFinalScore = 0;
                }

                uint bravoFinalScore;
                if (v18 != 0)
                {
                    bravoFinalScore = v19;
                }
                else
                {
                    bravoFinalScore = 100;
                }


                HandleFinishEvent(100 - alphaFinalScore, 100 - bravoFinalScore);
            }
            else if (eventType == 7) // Overtime start
            {
                if (PaintTargetAreas.Any(x => x.ControllingTeam != GetTrailingTeam()))
                {
                    HasTrailingLostControlOfAreasInOvertime = true;

                    SetOvertimeTimeout(OVERTIME_TIMEOUT_FRAMES);
                }
            }
        }

        // This appears in a lot of functions
        private uint VAreaTicksToSeconds(ushort ticks)
        {
            float convertedValue = ticks * 100.0f / 36000.0f;

            uint epsilon = (uint)(convertedValue >= 0.0f ? 1 : 0);
            if ((uint)convertedValue == convertedValue)
            {
                epsilon = 0;
            }

            return epsilon + (uint)convertedValue;
        }

    }
}
