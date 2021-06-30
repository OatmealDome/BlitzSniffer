using Blitz.Cmn.Def;
using BlitzCommon.Blitz.Cmn.Def;
using BlitzSniffer.Game.Event.Setup.Player;
using BlitzSniffer.Game.Event.Setup.Rule;
using BlitzSniffer.Resources;
using BlitzSniffer.Game.Tracker;
using BlitzSniffer.Game.Tracker.Versus;

namespace BlitzSniffer.Game.Event.Setup
{
    public class SetupVersusEvent : SetupEvent
    {
        private VersusRule _Rule;

        public override string Rule
        {
            get
            {
                switch (_Rule)
                {
                    case VersusRule.Pnt:
                        return "Paint";
                    case VersusRule.Vgl:
                        return "VGoal";
                    case VersusRule.Var:
                        return "VArea";
                    case VersusRule.Vlf:
                        return "VLift";
                    case VersusRule.Vcl:
                        return "VClam";
                    default:
                        return "Unknown";
                }
            }
        }

        public SetupVersusEvent()
        {
            VersusGameStateTracker stateTracker = GameSession.Instance.GameStateTracker as VersusGameStateTracker;

            _Rule = stateTracker.Rule;

            switch (_Rule)
            {
                case VersusRule.Var:
                    RuleConfiguration = new SetupVAreaRuleConfiguration();
                    break;
                case VersusRule.Vlf:
                    RuleConfiguration = new SetupVLiftRuleConfiguration();
                    break;
                default:
                    RuleConfiguration = new SetupGenericRuleConfiguration();
                    break;
            }
        }

        protected override SetupPlayer GetSetupPlayer(uint playerId, Tracker.Player.Player player)
        {
            return new SetupVersusPlayer()
            {
                Id = playerId,
                Name = player.Name,
                Weapon = new SetupWeapon()
                {
                    Id = WeaponResource.Instance.GetMainWeapon((int)player.Weapon.Id),
                    Sub = WeaponResource.Instance.GetSubWeapon((int)player.Weapon.SubId),
                    Special = WeaponResource.Instance.GetSpecialWeapon((int)player.Weapon.SpecialId)
                },
                Headgear = new SetupGear(GearKind.Head, player.Headgear),
                Clothes = new SetupGear(GearKind.Clothes, player.Clothes),
                Shoes = new SetupGear(GearKind.Shoes, player.Shoes)
            };
        }

    }
}
