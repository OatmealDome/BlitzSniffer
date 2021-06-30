using Blitz.Cmn.Def;
using BlitzSniffer.Game.Event;
using BlitzSniffer.Game.Event.Versus.VArea;

namespace BlitzSniffer.Game.Tracker.Versus.VArea
{
    class VAreaPaintTargetArea
    {
        private int AreaIdx
        {
            get;
            set;
        }

        public Team ControllingTeam
        {
            get;
            private set;
        }

        public float PaintPercentage
        {
            get;
            private set;
        }

        public byte Unk2
        {
            get;
            private set;
        }

        public sbyte Unk3
        {
            get;
            private set;
        }

        private bool StateEventWaitForMax
        {
            get;
            set;
        }

        public VAreaPaintTargetArea(int idx)
        {
            AreaIdx = idx;
            ControllingTeam = Team.Neutral;
            PaintPercentage = 0;
            Unk2 = 0;
            Unk3 = 0;
            StateEventWaitForMax = false;
        }

        public void ChangeControl(Team team)
        {
            if (ControllingTeam != team)
            {
                EventTracker.Instance.AddEvent(new VAreaPaintAreaControlChangeEvent()
                {
                    AreaIdx = AreaIdx,
                    Team = team
                });
            }


            if (ControllingTeam == Team.Neutral && team != Team.Neutral)
            {
                // This flag tells UpdateWitHState to wait for PaintPercentage to be updated to
                // 1.0f before sending new events. Otherwise, a VAreaPaintAreaControlChangeEvent
                // may be followed by a VAreaPaintAreaCappedStateUpdateEvent with a PaintPercentage
                // of less than 1.0f (typically around 0.7f since that's the capture percentage).
                StateEventWaitForMax = true;
            }

            ControllingTeam = team;

        }

        public void UpdateWithState(float paintPercent, byte unk2, sbyte unk3)
        {
            if (ControllingTeam == Team.Alpha)
            {
                // Alpha paint percentage is always positive
                if (paintPercent <= 0)
                {
                    return;
                }

                if (PaintPercentage == paintPercent)
                {
                    return;
                }

                PaintPercentage = paintPercent;

                if (StateEventWaitForMax)
                {
                    if (PaintPercentage != 1.0f)
                    {
                        return;
                    }
                    else
                    {
                        StateEventWaitForMax = false;
                    }
                }

                EventTracker.Instance.AddEvent(new VAreaPaintAreaCappedStateUpdateEvent()
                {
                    AreaIdx = AreaIdx,
                    PaintPercentage = PaintPercentage
                });
            }
            else if (ControllingTeam == Team.Bravo)
            {
                // Bravo paint percentage is always negative
                if (paintPercent >= 0)
                {
                    return;
                }

                if ((PaintPercentage * -1) == paintPercent)
                {
                    return;
                }

                PaintPercentage = paintPercent * -1;

                if (StateEventWaitForMax)
                {
                    if (PaintPercentage != 1.0f)
                    {
                        return;
                    }
                    else
                    {
                        StateEventWaitForMax = false;
                    }
                }

                EventTracker.Instance.AddEvent(new VAreaPaintAreaCappedStateUpdateEvent()
                {
                    AreaIdx = AreaIdx,
                    PaintPercentage = PaintPercentage
                });
            }
            else if (ControllingTeam == Team.Neutral)
            {
                float newPaintPercent = paintPercent;

                Team favouredTeam;
                if (paintPercent > 0)
                {
                    favouredTeam = Team.Alpha;
                }
                else if (paintPercent < 0)
                {
                    favouredTeam = Team.Bravo;
                    newPaintPercent *= -1; // make positive
                }
                else
                {
                    favouredTeam = Team.Neutral;
                }

                if (PaintPercentage == newPaintPercent)
                {
                    return;
                }

                PaintPercentage = newPaintPercent;

                EventTracker.Instance.AddEvent(new VAreaPaintAreaContestedStateUpdateEvent()
                {
                    AreaIdx = AreaIdx,
                    FavouredTeam = favouredTeam,
                    PaintPercentage = PaintPercentage
                });
            }

            Unk2 = unk2;
            Unk3 = unk3;
        }

    }
}
