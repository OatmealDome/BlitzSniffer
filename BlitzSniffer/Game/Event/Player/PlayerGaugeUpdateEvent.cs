namespace BlitzSniffer.Game.Event.Player
{
    public class PlayerGaugeUpdateEvent : PlayerEvent
    {
        public override string Name => "PlayerGaugeUpdate";

        public uint Charge
        {
            get;
            set;
        }

    }
}
