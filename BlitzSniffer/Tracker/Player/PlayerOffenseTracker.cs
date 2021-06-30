using BlitzSniffer.Game.Event;
using BlitzSniffer.Game.Event.Player;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace BlitzSniffer.Tracker.Player
{
    public class PlayerOffenseTracker : IDisposable
    {
        private static readonly ILogger LogContext = Log.ForContext(Constants.SourceContextPropertyName, "PlayerOffenseTracker");
        private static readonly uint FRAME_DELAY = 15;

        private readonly ConcurrentDictionary<uint, PlayerDeathEvent> WaitingDeathEvents;

        public PlayerOffenseTracker()
        {
            WaitingDeathEvents = new ConcurrentDictionary<uint, PlayerDeathEvent>();

            GameSession.Instance.GameTicked += HandleGameTick;
        }

        public void Dispose()
        {
            GameSession.Instance.GameTicked -= HandleGameTick;
        }

        public PlayerDeathEvent GetDeathEventForVictim(uint victimIdx)
        {
            if (WaitingDeathEvents.ContainsKey(victimIdx))
            {
                return WaitingDeathEvents[victimIdx];
            }
            else
            {
                PlayerDeathEvent deathEvent = new PlayerDeathEvent()
                {
                    PlayerIdx = victimIdx,
                    SendDeadline = GameSession.Instance.ElapsedTicks + FRAME_DELAY
                };

                WaitingDeathEvents[victimIdx] = deathEvent;

                return deathEvent;
            }
        }

        private void HandleGameTick(object sender, GameTickedEventArgs args)
        {
            IEnumerable<KeyValuePair<uint, PlayerDeathEvent>> deathEvents = WaitingDeathEvents.Where(p => p.Value.SendDeadline <= args.ElapsedTicks);
            if (deathEvents.Count() == 0)
            {
                return;
            }

            foreach (KeyValuePair<uint, PlayerDeathEvent> pair in deathEvents.ToList())
            {
                if (pair.Value.IsComplete)
                {
                    EventTracker.Instance.AddEvent(pair.Value);
                }
                else
                {
                    LogContext.Information("Removing incomplete event for victim {VictimIdx}", pair.Key);
                }

                WaitingDeathEvents.Remove(pair.Key, out _);
            }

        }

    }
}
