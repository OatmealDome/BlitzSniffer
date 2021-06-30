using Blitz.Cmn.Def;

namespace BlitzSniffer.Game.Event.Versus.VLift
{
    class VLiftCheckpointUpdateEvent : GameEvent
    {
        public override string Name => "VLiftCheckpointUpdate";

        public Team Team
        {
            get;
            set;
        }

        public int Idx
        {
            get;
            set;
        }

        public uint Hp
        {
            get;
            set;
        }

        public uint BestHp
        {
            get;
            set;
        }

    }
}
