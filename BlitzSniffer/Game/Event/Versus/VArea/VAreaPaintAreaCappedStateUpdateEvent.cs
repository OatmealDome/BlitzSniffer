namespace BlitzSniffer.Game.Event.Versus.VArea
{
    class VAreaPaintAreaCappedStateUpdateEvent : GameEvent
    {
        public override string Name => "VAreaPaintAreaCappedStateUpdate";

        public int AreaIdx
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
