namespace BlitzSniffer.Game.Event.Versus
{
    class GachiScoreUpdateEvent : GameEvent
    {
        public override string Name => "GachiScoreUpdate";

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

        public uint AlphaPenalty
        {
            get;
            set;
        }

        public uint BravoPenalty
        {
            get;
            set;
        }

    }
}
