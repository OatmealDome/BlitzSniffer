using BlitzSniffer.Game.Event.Setup.Player;
using BlitzSniffer.Game.Event.Setup.Rule;

namespace BlitzSniffer.Game.Event.Setup
{
    class SetupCoopEvent : SetupEvent
    {
        public override string Rule
        {
            get
            {
                return "Coop";
            }
        }

        public SetupCoopEvent() : base()
        {
            RuleConfiguration = new SetupGenericRuleConfiguration();
        }

        protected override SetupPlayer GetSetupPlayer(uint playerId, Tracker.Player.Player player)
        {
            return new SetupPlayer()
            {
                Id = playerId,
                Name = player.Name
            };
        }

    }
}
