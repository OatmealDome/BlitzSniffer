using BlitzSniffer.Tracker;
using BlitzSniffer.Tracker.Versus.VLift;
using System.Collections.Generic;

namespace BlitzSniffer.Game.Event.Setup.Rule
{
    public class SetupVLiftRuleConfiguration : SetupRuleConfiguration
    {
        public List<List<uint>> VLiftCheckpoints
        {
            get;
            set;
        }

        public SetupVLiftRuleConfiguration()
        {
            VLiftCheckpoints = (GameSession.Instance.GameStateTracker as VLiftVersusGameStateTracker).BuildCheckpointListForSetup();
        }

    }
}
