using BlitzSniffer.Game.Tracker;

namespace BlitzSniffer.Game.Event
{
    public abstract class GameEvent : SnifferEvent
    {
        public uint GameTick
        {
            get;
            set;
        }
        
        public GameEvent()
        {
            GameTick = GameSession.Instance.ElapsedTicks;
        }
        
    }
}
