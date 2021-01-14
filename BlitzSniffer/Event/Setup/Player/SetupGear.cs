﻿using BlitzCommon.Blitz.Cmn.Def;
using BlitzSniffer.Resources;
using System;

namespace BlitzSniffer.Event.Setup.Player
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
            Id = GearResource.Instance.GetGear(kind, (int)gear.Id);
            Main = Enum.GetName(typeof(GearSkill), gear.MainSkill);
            SecondaryOne = Enum.GetName(typeof(GearSkill), gear.SecondarySkillOne);
            SecondaryTwo = Enum.GetName(typeof(GearSkill), gear.SecondarySkillTwo);
            SecondaryThree = Enum.GetName(typeof(GearSkill), gear.SecondarySkillThree);
        }

    }
}
