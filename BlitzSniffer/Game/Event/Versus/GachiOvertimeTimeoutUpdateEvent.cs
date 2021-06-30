namespace BlitzSniffer.Game.Event.Versus
{
    class GachiOvertimeTimeoutUpdateEvent : GameEvent
    {
        public override string Name => "GachiOvertimeTimeoutUpdate";

        public int Length
        {
            get;
            set;
        }

    }
}
