using BlitzSniffer.Game.Resources;
using Nintendo.Sead;
using System;

namespace BlitzSniffer.Game.Tracker
{
    abstract class GameStateTracker : IDisposable
    {
        public ushort StageId
        {
            get;
            private set;
        }

        protected dynamic StageLayout
        {
            get;
            private set;
        }

        public Color4f AlphaColor
        {
            get;
            private set;
        }

        public Color4f BravoColor
        {
            get;
            private set;
        }

        public Color4f NeutralColor
        {
            get;
            private set;
        }

        public GameStateTracker(ushort stage, Color4f alpha, Color4f bravo, Color4f neutral)
        {
            StageId = stage;
            StageLayout = StageResource.Instance.LoadStageForId((int)stage);
            AlphaColor = alpha;
            BravoColor = bravo;
            NeutralColor = neutral;
        }

        public abstract void Dispose();

    }
}
