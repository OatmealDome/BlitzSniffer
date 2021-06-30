namespace BlitzSniffer.Game.Event.Versus.VLift
{
    class VLiftPositionUpdateEvent : GameEvent
    {
        public override string Name => "VLiftPositionUpdate";

        public uint AlphaPosition
        {
            get;
            set;
        }

        public uint BravoPosition
        {
            get;
            set;
        }

    }
}