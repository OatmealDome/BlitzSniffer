﻿using Blitz.Cmn.Def;
using Nintendo.Sead;

namespace BlitzSniffer.Game.Tracker.Versus
{
    class GenericVersusGameStateTracker : VersusGameStateTracker
    {
        private VersusRule _Rule;

        public override VersusRule Rule => _Rule;

        public GenericVersusGameStateTracker(ushort stage, VersusRule rule, Color4f alpha, Color4f bravo, Color4f neutral) : base(stage, alpha, bravo, neutral)
        {
            _Rule = rule;
        }

        public override void Dispose()
        {

        }

    }
}
