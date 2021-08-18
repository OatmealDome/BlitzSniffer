namespace BlitzSniffer.Game.Event.Player.VClam
{
    public class PlayerClamNormalCountUpdateEvent : PlayerEvent
    {
        public override string Name => "PlayerNormalClamCountUpdate";

        public uint Clams
        {
            get;
            set;
        }
        
    }
}