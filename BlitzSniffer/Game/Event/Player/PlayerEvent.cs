namespace BlitzSniffer.Game.Event.Player
{
    public abstract class PlayerEvent : GameEvent
    {
        public uint PlayerIdx
        {
            get;
            set;
        }

    }
}
