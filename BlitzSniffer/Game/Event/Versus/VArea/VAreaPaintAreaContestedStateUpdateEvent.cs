using Blitz.Cmn.Def;

namespace BlitzSniffer.Game.Event.Versus.VArea
{
    class VAreaPaintAreaContestedStateUpdateEvent : GameEvent
    {
        public override string Name => "VAreaPaintAreaContestedStateUpdate";

        public int AreaIdx
        {
            get;
            set;
        }

        public Team FavouredTeam
        {
            get;
            set;
        }

        public float PaintPercentage
        {
            get;
            set;
        }

    }
}
