namespace BlitzSniffer.Game.Event.Player
{
    class PlayerSignalEvent : PlayerEvent
    {
        public override string Name => "PlayerSignal";

        public string SignalType
        {
            get;
            set;
        }

    }
}
