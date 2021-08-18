using Blitz.Cmn.Def;
using BlitzSniffer.Game.Event;
using BlitzSniffer.Game.Event.Player.VClam;
using BlitzSniffer.Game.Tracker.Player;
using BlitzSniffer.Network.Netcode.Clone;
using Nintendo.Sead;
using Syroot.BinaryData;
using System.IO;

namespace BlitzSniffer.Game.Tracker.Versus.VClam
{
    class VClamVersusGameStateTracker : GachiVersusGameStateTracker
    {
        public override VersusRule Rule => VersusRule.Vcl;

        public override bool HasPenalties => true;

        private VClamBasket AlphaBasket;
        private VClamBasket BravoBasket;
        private VClamBasket CurrentBrokenBasket;

        public VClamVersusGameStateTracker(ushort stage, Color4f alpha, Color4f bravo) : base(stage, alpha, bravo)
        {
            AlphaBasket = new VClamBasket(Team.Alpha);
            BravoBasket = new VClamBasket(Team.Bravo);
            CurrentBrokenBasket = null;

            AlphaBasket.OppositeBasket = BravoBasket;
            BravoBasket.OppositeBasket = AlphaBasket;

            CloneHolder holder = CloneHolder.Instance;

            holder.RegisterClone(134);

            for (uint i = 0; i < 10; i++)
            {
                holder.RegisterClone(135 + i);
            }

            GameSession session = GameSession.Instance;
            session.InGameCloneChanged += HandleTake;
            session.InGameCloneChanged += HandleBasketBreak;
            session.InGameCloneChanged += HandleBasketRepair;
            session.InGameCloneChanged += HandleReserveThrow;
            session.InGameCloneChanged += HandleScoreEvent;

        }

        public override void Dispose()
        {
            AlphaBasket.Dispose();
            BravoBasket.Dispose();

            GameSession session = GameSession.Instance;
            session.InGameCloneChanged -= HandleTake;
            session.InGameCloneChanged -= HandleBasketBreak;
            session.InGameCloneChanged -= HandleBasketRepair;
            session.InGameCloneChanged -= HandleReserveThrow;
            session.InGameCloneChanged -= HandleScoreEvent;
        }

        /* Master:
         * 
         * Take
         * ClamSpawn
         * Result
         * BasketBreak
         * BasketRepair
         * ReserveThrow
         * Sleep
         * Score
         * 
         * Player:
         * 
         * Lost
         * TakeReq
         * CreateGolden
         * Bank
         */

        public void HandleTake(object sender, CloneChangedEventArgs args)
        {
            if (args.CloneId != 134)
            {
                return;
            }

            if (args.ElementId != 1)
            {
                return;
            }

            using (MemoryStream stream = new MemoryStream(args.Data))
            using (BinaryDataReader reader = new BinaryDataReader(stream))
            {
                reader.ByteOrder = ByteOrder.LittleEndian;

                uint netActorClamId = reader.ReadUInt32();
                uint playerId = reader.ReadByte();
                byte isGolden = reader.ReadByte();
                
                Player.Player player = GameSession.Instance.PlayerTracker.GetPlayer(playerId);

                if (isGolden == 0)
                {
                    player.Clams++;
                }

                if (isGolden != 0 || player.Clams == 10)
                {
                    player.Clams = 0;
                    player.HasGoldenClam = true;
                    
                    EventTracker.Instance.AddEvent(new PlayerClamGoldenTakeEvent()
                    {
                        PlayerIdx = playerId
                    });
                }
                else
                {
                    EventTracker.Instance.AddEvent(new PlayerClamNormalCountUpdateEvent()
                    {
                        PlayerIdx = playerId,
                        Clams = player.Clams
                    });
                }
            }
        }

        public void HandleReserveThrow(object sender, CloneChangedEventArgs args)
        {
            if (args.CloneId != 134)
            {
                return;
            }

            if (args.ElementId != 6)
            {
                return;
            }

            using (MemoryStream stream = new MemoryStream(args.Data))
            using (BinaryDataReader reader = new BinaryDataReader(stream))
            {
                reader.ByteOrder = ByteOrder.LittleEndian;

                uint clamIdx = reader.ReadUInt16(); // maybe
                byte playerId = reader.ReadByte();
                byte isGolden = reader.ReadByte();
                
                Player.Player player = GameSession.Instance.PlayerTracker.GetPlayer(playerId);

                if (isGolden == 0)
                {
                    player.Clams--;
                    
                    EventTracker.Instance.AddEvent(new PlayerClamNormalCountUpdateEvent()
                    {
                        PlayerIdx = playerId,
                        Clams = player.Clams
                    });
                }
                else
                {
                    player.HasGoldenClam = false;
                    
                    EventTracker.Instance.AddEvent(new PlayerClamGoldenLostEvent()
                    {
                        PlayerIdx = playerId
                    });
                }
            }
        }

        private void HandleBasketBreak(object sender, CloneChangedEventArgs args)
        {
            if (args.CloneId != 134)
            {
                return;
            }

            if (args.ElementId != 4)
            {
                return;
            }

            using (MemoryStream stream = new MemoryStream(args.Data))
            using (BinaryDataReader reader = new BinaryDataReader(stream))
            {
                reader.ByteOrder = ByteOrder.LittleEndian;

                uint gameFrame = reader.ReadUInt32();
                Team breakerTeam = (Team)reader.ReadByte();

                VClamBasket basket;
                if (breakerTeam == Team.Alpha)
                {
                    basket = BravoBasket;
                }
                else
                {
                    basket = AlphaBasket;
                }

                if (CurrentBrokenBasket != null)
                {
                    if (CurrentBrokenBasket == basket)
                    {
                        return;
                    }

                    // If this barrier break will happen after the one we are currently tracking,
                    // we should just ignore it.
                    if (gameFrame >= CurrentBrokenBasket.GetCurrentBreakRequestTick())
                    {
                        return;
                    }

                    // Otherwise, this one will happen first and therefore should take precedence.
                    CurrentBrokenBasket.NullifyBreakRequest();
                }

                basket.RequestBreak(gameFrame);

                CurrentBrokenBasket = basket;
            }
        }

        private void HandleBasketRepair(object sender, CloneChangedEventArgs args)
        {
            if (args.CloneId != 134)
            {
                return;
            }

            if (args.ElementId != 5)
            {
                return;
            }

            if (CurrentBrokenBasket == null)
            {
                return;
            }

            using (MemoryStream stream = new MemoryStream(args.Data))
            using (BinaryDataReader reader = new BinaryDataReader(stream))
            {
                reader.ByteOrder = ByteOrder.LittleEndian;

                uint gameFrame = reader.ReadUInt32();

                uint newAlphaScore = reader.ReadByte();
                uint newBravoScore = reader.ReadByte();
                uint newAlphaPenalty = reader.ReadByte();
                uint newBravoPenalty = reader.ReadByte();

                UpdateScores(newAlphaScore, newBravoScore, newAlphaPenalty, newBravoPenalty);

                CurrentBrokenBasket.RequestRepair(gameFrame);

                CurrentBrokenBasket = null;
            }
        }

        private void HandleScoreEvent(object sender, CloneChangedEventArgs args)
        {
            if (args.CloneId != 134)
            {
                return;
            }

            if (args.ElementId != 8)
            {
                return;
            }

            using (MemoryStream stream = new MemoryStream(args.Data))
            using (BinaryDataReader reader = new BinaryDataReader(stream))
            {
                reader.ByteOrder = ByteOrder.LittleEndian;

                uint gameFrame = reader.ReadUInt32();
                uint basketLeft = reader.ReadUInt16();

                uint newAlphaScore = reader.ReadByte();
                uint newBravoScore = reader.ReadByte();
                uint newAlphaPenalty = reader.ReadByte();
                uint newBravoPenalty = reader.ReadByte();

                UpdateScores(newAlphaScore, newBravoScore, newAlphaPenalty, newBravoPenalty);

                CurrentBrokenBasket.UpdateBrokenFrames(basketLeft);
            }
        }

        protected override void HandleSystemEvent(uint eventType, BinaryDataReader reader)
        {
            if (eventType == 6) // VClam finish
            {
                // These bytes are the "result left count", so convert them to score
                HandleFinishEvent((uint)100 - reader.ReadByte(), (uint)100 - reader.ReadByte());
            }
            else if (eventType == 7) // Overtime start
            {
                if (AlphaScore == 0 && BravoScore == 0)
                {
                    // If nobody scores for the entire game, a special 3 minute Overtime starts.
                    SetOvertimeTimeout(10800); 
                }
                else
                {
                    // Otherwise, the normal Overtime consisting of 20 seconds begins.
                    SetOvertimeTimeout(1200);
                }
            }
        }

    }
}
