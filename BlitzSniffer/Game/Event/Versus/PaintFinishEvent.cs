namespace BlitzSniffer.Game.Event.Versus
{
    class PaintFinishEvent : GameEvent
    {
        public override string Name => "PaintFinish";

        public float AlphaPoints
        {
            get;
            set;
        }

        public float BravoPoints
        {
            get;
            set;
        }

    }
}
