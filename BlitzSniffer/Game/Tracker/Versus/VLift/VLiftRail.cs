using Blitz.Cmn.Def;
using BlitzSniffer.Game.Event;
using BlitzSniffer.Game.Event.Versus.VLift;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace BlitzSniffer.Game.Tracker.Versus.VLift
{
    class VLiftRail
    {
        private List<VLiftCheckpoint> AlphaCheckpoints; // in Bravo base
        private List<VLiftCheckpoint> BravoCheckpoints; // in Alpha base

        private int AlphaCurrentCheckpoint = 0;
        private int BravoCurrentCheckpoint = 0;

        private uint AlphaLastTotalHp = 0;
        private uint BravoLastTotalHp = 0;

        public VLiftRail()
        {
            AlphaCheckpoints = new List<VLiftCheckpoint>();
            BravoCheckpoints = new List<VLiftCheckpoint>();
        }

        public void AddCheckpoint(uint hp)
        {
            AlphaLastTotalHp += hp;
            BravoLastTotalHp += hp;

            AlphaCheckpoints.Add(new VLiftCheckpoint(hp));
            BravoCheckpoints.Add(new VLiftCheckpoint(hp));
        }

        public List<uint> GetCheckpointsForSetup(Team team)
        {
            List<VLiftCheckpoint> checkpoints;
            if (team == Team.Alpha)
            {
                checkpoints = AlphaCheckpoints;
            }
            else
            {
                checkpoints = BravoCheckpoints;
            }

            return checkpoints.Select(x => x.BaseHp).ToList();
        }

        // This is a mess
        public void UpdateCheckpoints(uint totalHp, Team team)
        {
            uint lastTotalHp;
            if (team == Team.Alpha)
            {
                lastTotalHp = AlphaLastTotalHp;
            }
            else
            {
                lastTotalHp = BravoLastTotalHp;
            }

            int difference = (int)lastTotalHp - (int)totalHp;

            if (difference == 0)
            {
                return;
            }
            else
            {
                if (team == Team.Alpha)
                {
                    AlphaLastTotalHp -= (uint)difference;
                }
                else
                {
                    BravoLastTotalHp -= (uint)difference;
                }
            }

            do
            {
                VLiftCheckpoint checkpoint;
                int UpdateCheckpoint;

                if (team == Team.Alpha)
                {
                    checkpoint = AlphaCheckpoints[AlphaCurrentCheckpoint];
                    UpdateCheckpoint = AlphaCurrentCheckpoint;
                }
                else
                {
                    checkpoint = BravoCheckpoints[BravoCurrentCheckpoint];
                    UpdateCheckpoint = BravoCurrentCheckpoint;
                }

                if (difference > 0)
                {
                    if (difference > checkpoint.Hp)
                    {
                        uint rollover = (uint)difference - checkpoint.Hp;
                        checkpoint.Hp = 0;
                        difference = (int)rollover;
                    }
                    else
                    {
                        checkpoint.Hp -= (uint)difference;
                        difference = 0;
                    }

                    if (checkpoint.Hp == 0)
                    {
                        if (team == Team.Alpha)
                        {
                            AlphaCurrentCheckpoint++;
                        }
                        else
                        {
                            BravoCurrentCheckpoint++;
                        }
                    }
                }
                else if (difference < 0)
                {
                    uint newHp = checkpoint.Hp + (uint)(-difference);
                    if (newHp > checkpoint.BaseHp)
                    {
                        if (team == Team.Alpha)
                        {
                            AlphaCurrentCheckpoint--;
                        }
                        else
                        {
                            BravoCurrentCheckpoint--;
                        }

                        continue;
                    }

                    checkpoint.Hp = newHp;
                    difference = 0;
                }

                EventTracker.Instance.AddEvent(new VLiftCheckpointUpdateEvent()
                {
                    Team = team,
                    Idx = UpdateCheckpoint,
                    Hp = checkpoint.Hp,
                    BestHp = checkpoint.BestHp
                });
            }
            while (difference != 0);
        }

        public override string ToString()
        {
            return $"VLiftRail al:[ {string.Join(", ", AlphaCheckpoints.Select(x => x.Hp))} ], br:[ {string.Join(", ", BravoCheckpoints.Select(x => x.Hp))} ], al total:{AlphaLastTotalHp}, br total:{BravoLastTotalHp}, al current:{AlphaCurrentCheckpoint}, br current:{BravoCurrentCheckpoint}";
        }

    }
}
