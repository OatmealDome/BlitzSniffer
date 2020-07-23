﻿using BlitzSniffer.Resources;
using Nintendo.Sead;
using System;

namespace BlitzSniffer.Tracker
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

        public GameStateTracker(ushort stage, Color4f alpha, Color4f bravo)
        {
            StageId = stage;
            StageLayout = StageResource.Instance.LoadStageForId((int)stage);
            AlphaColor = alpha;
            BravoColor = bravo;
        }

        public abstract void Dispose();

    }
}
