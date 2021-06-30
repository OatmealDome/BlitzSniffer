namespace BlitzSniffer.Game.Event.Versus
{
    class GachiFinishEvent : GameEvent
    {
        public override string Name => "GachiFinish";

        public uint AlphaScore
        {
            get;
            set;
        }

        public uint BravoScore
        {
            get;
            set;
        }

    }
}
