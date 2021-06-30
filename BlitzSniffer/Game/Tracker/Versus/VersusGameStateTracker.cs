using Blitz.Cmn.Def;
using Nintendo.Sead;

namespace BlitzSniffer.Game.Tracker.Versus
{
    abstract class VersusGameStateTracker : GameStateTracker
    {
        public abstract VersusRule Rule
        {
            get;
        }

        protected VersusGameStateTracker(ushort stage, Color4f alpha, Color4f bravo) : base(stage, alpha, bravo)
        {
        }

    }
}
