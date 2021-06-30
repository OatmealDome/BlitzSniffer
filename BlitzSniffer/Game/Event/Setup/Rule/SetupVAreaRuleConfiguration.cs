using BlitzSniffer.Game.Tracker;
using BlitzSniffer.Game.Tracker.Versus.VArea;

namespace BlitzSniffer.Game.Event.Setup.Rule
{
    class SetupVAreaRuleConfiguration : SetupRuleConfiguration
    {
        public int VAreaTargetAreasCount
        {
            get;
            set;
        }

        public SetupVAreaRuleConfiguration()
        {
            VAreaTargetAreasCount = (GameSession.Instance.GameStateTracker as VAreaVersusGameStateTracker).GetPaintTargetAreasCount();
        }

    }
}
