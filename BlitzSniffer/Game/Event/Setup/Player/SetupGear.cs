using BlitzCommon.Blitz.Cmn.Def;
using BlitzSniffer.Game.Resources;
using System;

namespace BlitzSniffer.Game.Event.Setup.Player
{
    public class SetupGear
    {
        public string Id
        {
            get;
            set;
        }

        public string Main
        {
            get;
            set;
        }

        public string SecondaryOne
        {
            get;
            set;
        }

        public string SecondaryTwo
        {
            get;
            set;
        }

        public string SecondaryThree
        {
            get;
            set;
        }

        public SetupGear(GearKind kind, Gear gear)
        {
            string name = GearResource.Instance.GetGear(kind, (int)gear.Id);

            string kindStr;
            switch (kind)
            {
                case GearKind.Head:
                    kindStr = "Hed";
                    break;
                case GearKind.Clothes:
                    kindStr = "Clt";
                    break;
                case GearKind.Shoes:
                    kindStr = "Shs";
                    break;
                default:
                    kindStr = "Unk";
                    break;
            }

            Id = $"{kindStr}_{name}";
            Main = Enum.GetName(typeof(GearSkill), gear.MainSkill);
            SecondaryOne = Enum.GetName(typeof(GearSkill), gear.SecondarySkillOne);
            SecondaryTwo = Enum.GetName(typeof(GearSkill), gear.SecondarySkillTwo);
            SecondaryThree = Enum.GetName(typeof(GearSkill), gear.SecondarySkillThree);
        }

    }
}
