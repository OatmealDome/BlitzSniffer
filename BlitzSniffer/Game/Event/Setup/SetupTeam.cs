using Nintendo.Sead;
using System.Collections.Generic;

namespace BlitzSniffer.Game.Event.Setup
{
    public class SetupTeam
    {
        public Color4f Color
        {
            get;
            set;
        }

        // Hack. Polymorphic serialization isn't supported. See SetupEvent. Honestly,
        // I should just switch to Newtonsoft.Json at this point.
        public List<object> Players
        {
            get;
            set;
        }

        public SetupTeam()
        {
            Players = new List<object>();
        }

    }
}
