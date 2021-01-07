using BlitzSniffer.Clone;
using BlitzSniffer.Enl;
using BlitzSniffer.Tracker.Player;
using NintendoNetcode.Enl.Record;
using Syroot.BinaryData;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BlitzSniffer.Tracker.Station
{
    class StationTracker
    {
        private readonly Dictionary<uint, Station> Stations;

        public StationTracker()
        {
            Stations = new Dictionary<uint, Station>();
            for (uint i = 0; i < 10; i++)
            {
                Stations[i] = new Station(i);
            }

            CloneHolder holder = CloneHolder.Instance;
            holder.CloneChanged += HandleStationInfo;
            holder.CloneChanged += HandlePlayerName;

            for (uint i = 0; i < 10; i++)
            {
                holder.RegisterClone(i + 3);
            }

             EnlHolder.Instance.SystemInfoReceived += HandleEnlSystemInfo;
        }

        private void HandleSeqStateAllSame(byte seqState)
        {
            switch (seqState)
            {
                case 7: // Apparently this is "ready for game start" - Game::OnlineStartGameExe::stateWaitForSeqState()
                    PlayerTracker playerTracker = GameSession.Instance.PlayerTracker;

                    // Set default values for PlayerTracker
                    foreach (Station station in Stations.Values)
                    {
                        if (station.PlayerId == 0xFF)
                        {
                            continue;
                        }

                        Player.Player player = playerTracker.GetPlayer(station.PlayerId);
                        player.Name = station.Name;
                        player.IsActive = true;
                        player.IsAlive = true;
                    }

                    playerTracker.ApplyTeamBits();

                    // Everything should be ready now, so fire SetupEvent
                    // Previously this was done on receiving Cnet::PacketSeqEventVersusSetting, but SeqState 7 isn't
                    // fired until the game is ready to proceed into synchronizing the clocks and waiting for the
                    // game to start (VS intro demo), so this is probably the best place to do this
                    GameSession.Instance.FireSetupEvent();
                    
                    break;
                case 12: // Results screen start - Game::SeqVersusResult::stateEnterStartResult()
                    GameSession.Instance.Reset();

                    break;
                default:
                    break;
            }
        }

        private void HandleEnlSystemInfo(object sender, SystemInfoReceivedEventArgs args)
        {
            EnlSystemInfoRecord record = args.Record;

            // WTF is happening here?
            if (record.PlayerIds.All(x => x == 0xFF))
            {
                return;
            }

            for (uint i = 0; i < record.PlayerIds.Count; i++)
            {
                Station station = Stations[i];
                uint playerId = record.PlayerIds[(int)i];

                if (station.PlayerId != 0xFF && playerId == 0xFF)
                {
                    station.Reset();
                }

                station.PlayerId = playerId;
            }
        }

        private void HandleStationInfo(object sender, CloneChangedEventArgs args)
        {
            uint enlId = args.CloneId - 3;
            if (enlId >= 10)
            {
                return;
            }

            if (args.ElementId != 0)
            {
                return;
            }

            using (MemoryStream stream = new MemoryStream(args.Data))
            using (BinaryDataReader reader = new BinaryDataReader(stream))
            {
                // Bitflag
                reader.Seek(2);

                byte seqState = reader.ReadByte();
                Stations[enlId].SeqState = seqState;

                // byte enlConnectedNum = reader.ReadByte();
                
                // Check if all the stations have the same Cnet::Def::SeqState
                IEnumerable<Station> connectedStations = Stations.Values.Where(s => s.PlayerId != 0xFF);
                if (connectedStations.Count() > 0 && connectedStations.All(s => s.SeqState == seqState))
                {
                    HandleSeqStateAllSame(seqState);
                }
            }
        }

        private void HandlePlayerName(object sender, CloneChangedEventArgs args)
        {
            uint enlId = args.CloneId - 3;
            if (enlId >= 10)
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
                Stations[enlId].Name = reader.ReadString(BinaryStringFormat.ZeroTerminated, Encoding.Unicode);
            }
        }

    }
}
