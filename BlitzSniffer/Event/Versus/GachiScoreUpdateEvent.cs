﻿namespace BlitzSniffer.Event.Versus
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

    }
}