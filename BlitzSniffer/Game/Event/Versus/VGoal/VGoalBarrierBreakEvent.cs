namespace BlitzSniffer.Game.Event.Versus.VGoal
{
    class VGoalBarrierBreakEvent : GameEvent
    {
        public override string Name => "VGoalBarrierBreak";
    
        public uint BreakerPlayerIdx
        {
            get;
            set;
        }

    }
}
