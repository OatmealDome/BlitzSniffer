namespace BlitzSniffer.Game.Event.Versus.VClam
{
    class VClamBasketTimeoutUpdateEvent : VClamBasketEvent
    {
        public override string Name => "VClamBasketTimeoutUpdate";

        public uint Timeout
        {
            get;
            set;
        }

    }
}
