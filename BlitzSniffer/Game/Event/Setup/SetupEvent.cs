using Blitz.Cmn.Def;
using BlitzSniffer.Game.Event.Setup.Player;
using BlitzSniffer.Game.Event.Setup.Rule;
using BlitzSniffer.Game.Resources;
using BlitzSniffer.Game.Tracker;
using System.Collections.Generic;
using System.Diagnostics;

namespace BlitzSniffer.Game.Event.Setup
{
    public abstract class SetupEvent : SnifferEvent
    {
        public override string Name => "Setup";

        public abstract string Rule
        {
            get;
        }

        public string StageId
        {
            get;
            set;
        }

        // Hack: Serialization of a polymorphic type hierarchy not supported, but can be forced if the type is
        // object. So, the actual SetupRuleConfiguration is stored in a private variable, and a property with
        // type object is exposed to the serializer.
        // https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-how-to#serialize-properties-of-derived-classes
        private SetupRuleConfiguration _RuleConfiguration;

        public object RuleConfiguration
        {
            get
            {
                return _RuleConfiguration;
            }
            set
            {
                Trace.Assert(value is SetupRuleConfiguration);

                _RuleConfiguration = value as SetupRuleConfiguration;
            }
        }

        public List<SetupTeam> Teams
        {
            get;
            set;
        }

        protected SetupEvent()
        {
            Teams = new List<SetupTeam>();
            for (int i = 0; i < 2; i++)
            {
                Teams.Add(new SetupTeam());
            }

            GameStateTracker stateTracker = GameSession.Instance.GameStateTracker;

            StageId = StageResource.Instance.GetStageNameForId(stateTracker.StageId);
            Teams[0].Color = stateTracker.AlphaColor;
            Teams[1].Color = stateTracker.BravoColor;

            for (uint i = 0; i != 10; i++)
            {
                Tracker.Player.Player trackedPlayer = GameSession.Instance.PlayerTracker.GetPlayer(i);

                if (!trackedPlayer.IsActive || trackedPlayer.Team == Team.Neutral)
                {
                    continue;
                }

                SetupPlayer setupPlayer = GetSetupPlayer(i, trackedPlayer);

                if (trackedPlayer.Team == Team.Alpha)
                {
                    Teams[0].Players.Add(setupPlayer);
                }
                else
                {
                    Teams[1].Players.Add(setupPlayer);
                }
            }
        }

        protected abstract SetupPlayer GetSetupPlayer(uint playerId, Tracker.Player.Player player);

    }
}
