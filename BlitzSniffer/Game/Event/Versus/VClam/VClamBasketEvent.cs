using Blitz.Cmn.Def;

namespace BlitzSniffer.Game.Event.Versus.VClam
{
    abstract class VClamBasketEvent : GameEvent
    {
        public Team Team
        {
            get;
            set;
        }

    }
}
