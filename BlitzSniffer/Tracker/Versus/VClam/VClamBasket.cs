using Blitz.Cmn.Def;
using BlitzSniffer.Game.Event;
using BlitzSniffer.Game.Event.Versus.VClam;
using System;
using System.Diagnostics;

namespace BlitzSniffer.Tracker.Versus.VClam
{
    class VClamBasket : IDisposable
    {
        public Team OwningTeam
        {
            get;
            private set;
        }

        public VClamBasket OppositeBasket
        {
            get
            {
                return _OppositeBasket;
            }
            set
            {
                Trace.Assert(_OppositeBasket == null);

                _OppositeBasket = value;
            }
        }

        public VClamBasketState State
        {
            get;
            private set;
        }

        private VClamBasket _OppositeBasket;
        private uint TargetTick; // variable reused between states
        private uint LeftBrokenFrames;
        private bool InvincibleByLink;

        public VClamBasket(Team team)
        {
            OwningTeam = team;

            TargetTick = 0;
            LeftBrokenFrames = 0;
            InvincibleByLink = false;

            GameSession.Instance.GameTicked += HandleGameTick;
        }

        public void Dispose()
        {
            _OppositeBasket = null; // just in case

            GameSession.Instance.GameTicked -= HandleGameTick;
        }

        public void RequestBreak(uint tick)
        {
            Trace.Assert(State == VClamBasketState.Idle);

            TargetTick = tick;
        }

        public uint GetCurrentBreakRequestTick()
        {
            Trace.Assert(State == VClamBasketState.Idle);

            return TargetTick;
        }

        public void NullifyBreakRequest()
        {
            Trace.Assert(State == VClamBasketState.Idle);

            TargetTick = 0;
        }

        public void UpdateBrokenFrames(uint newFrames)
        {
            if (newFrames != LeftBrokenFrames)
            {
                LeftBrokenFrames = newFrames;

                EventTracker.Instance.AddEvent(new VClamBasketTimeoutUpdateEvent()
                {
                    Team = OwningTeam,
                    Timeout = newFrames
                });
            }
        }

        public void RequestRepair(uint tick)
        {
            Trace.Assert(State == VClamBasketState.Broken || State == VClamBasketState.Closed);

            TargetTick = tick;
        }

        public void RequestInvincibilityByLink()
        {
            Trace.Assert(State == VClamBasketState.Idle || State == VClamBasketState.Invincible);

            EventTracker.Instance.AddEvent(new VClamBasketVulnerabilityUpdateEvent()
            {
                Team = OwningTeam,
                IsInvincible = true
            });

            InvincibleByLink = true;

            // Clear because we could be transitioning from already invincible (i.e. this basket
            // was broken, transitioned into invincible, then the opposite team's basket was broken
            // during the invincibility period)
            TargetTick = 0;

            State = VClamBasketState.Invincible;
        }

        private void HandleGameTick(object sender, GameTickedEventArgs args)
        {
            switch (State)
            {
                case VClamBasketState.Idle:
                    if (TargetTick == 0)
                    {
                        return;
                    }

                    if (args.ElapsedTicks >= TargetTick)
                    {
                        TargetTick = 0;

                        EventTracker.Instance.AddEvent(new VClamBasketBreakEvent()
                        {
                            Team = OwningTeam
                        });

                        UpdateBrokenFrames(600); // default, 10 seconds

                        InvincibleByLink = false;

                        OppositeBasket.RequestInvincibilityByLink();

                        State = VClamBasketState.Broken;
                    }

                    break;
                case VClamBasketState.Broken:
                    if (LeftBrokenFrames > 0)
                    {
                        LeftBrokenFrames--;

                        if (LeftBrokenFrames == 0)
                        {
                            EventTracker.Instance.AddEvent(new VClamBasketClosedEvent()
                            {
                                Team = OwningTeam
                            });

                            // We could send this on the transition to our invincible state,
                            // but that is 60 ticks after it technically becomes invincible.
                            EventTracker.Instance.AddEvent(new VClamBasketVulnerabilityUpdateEvent()
                            {
                                Team = OwningTeam,
                                IsInvincible = true
                            });

                            State = VClamBasketState.Closed;
                        }
                    }

                    break;
                case VClamBasketState.Closed:
                    if (TargetTick == 0)
                    {
                        return;
                    }

                    if (args.ElapsedTicks >= TargetTick)
                    {
                        TargetTick = TargetTick + 600;
                        
                        State = VClamBasketState.Invincible;
                    }

                    break;
                case VClamBasketState.Invincible:
                    bool idleCondition;
                    if (InvincibleByLink)
                    {
                        idleCondition = OppositeBasket.State != VClamBasketState.Broken;
                    }
                    else
                    {
                        idleCondition = args.ElapsedTicks >= TargetTick;
                    }

                    if (idleCondition)
                    {
                        TargetTick = 0;

                        EventTracker.Instance.AddEvent(new VClamBasketVulnerabilityUpdateEvent()
                        {
                            Team = OwningTeam,
                            IsInvincible = false
                        });

                        State = VClamBasketState.Idle;
                    }

                    break;
            }
        }

    }
}
