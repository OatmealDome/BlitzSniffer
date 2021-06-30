namespace BlitzSniffer.Game.Event.Player
{
    class PlayerCoopRescuedEvent : PlayerEvent
    {
        public override string Name => "PlayerCoopRescued";

        public int SaviourIdx
        {
            get;
            set;
        }

    }
}
