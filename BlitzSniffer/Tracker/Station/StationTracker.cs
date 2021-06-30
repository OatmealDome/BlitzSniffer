using BlitzSniffer.Network.Netcode.Clone;
using BlitzSniffer.Network.Netcode.Enl;
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
        private readonly Dictionary<ulong, Station> Stations;

        public byte LastJointSeqState
        {
            get;
            private set;
        }

        private byte LastMasterSeqState
        {
            get;
            set;
        }

        public int ActivePlayerCount
        {
            get;
            private set;
        }

        public StationTracker()
        {
            Stations = new Dictionary<ulong, Station>();
            LastJointSeqState = 0;
            LastMasterSeqState = 0;
            ActivePlayerCount = 0;

            CloneHolder holder = CloneHolder.Instance;
            holder.CloneChanged += HandleStationInfo;
            holder.CloneChanged += HandlePlayerName;
            holder.CloneChanged += HandlePlayerInfo;
            holder.CloneChanged += HandleMasterSeqState;

            for (uint i = 0; i < 10; i++)
            {
                holder.RegisterClone(i + 3);
            }

             EnlHolder.Instance.SystemInfoReceived += HandleEnlSystemInfo;
        }

        public Station GetStationForSsid(ulong sourceId)
        {
            if (Stations.TryGetValue(sourceId, out Station station))
            {
                return station;
            }
            else
            {
                Station newStation = new Station(sourceId);

                Stations.Add(sourceId, newStation);

                return newStation;
            }
        }

        private void HandleSeqStateAllSame(byte seqState)
        {
            if (seqState == LastJointSeqState)
            {
                return;
            }

            switch (seqState)
            {
                case 7: // Apparently this is "ready for game start" - Game::OnlineStartGameExe::stateWaitForSeqState()
                    // SeqState 7 isn't fired until the game is ready to proceed into synchronizing the clocks
                    // and waiting for the game to start (VS intro demo)
                    GameSession.Instance.SignalSetupReady();

                    break;
                default:
                    break;
            }

            LastJointSeqState = seqState;
        }

        private void HandleSeqStateMaster(byte seqState)
        {
            if (seqState == LastMasterSeqState)
            {
                return;
            }

            switch (seqState)
            {
                case 12: // Results screen start - Game::SeqVersusResult::stateEnterStartResult()
                    GameSession.Instance.Reset();

                    break;
                default:
                    break;
            }

            LastMasterSeqState = seqState;
        }

        private void HandleEnlSystemInfo(object sender, SystemInfoReceivedEventArgs args)
        {
            EnlSystemInfoRecord record = args.Record;

            int activeIds = record.PlayerIds.Where(p => p != 0xFF).Count();

            if (activeIds == 0)
            {
                return;
            }

            int disconnectedPlayers = 0;

            for (int i = 0; i < record.Unknown3.Count(); i++)
            {
                int bitMask = 1 << i;
                if ((record.DisconnectedBitmap & (ulong)bitMask) != 0)
                {
                    disconnectedPlayers++;

                    // Only set this player as disconnected if in-game
                    if (LastMasterSeqState >= 7)
                    {
                        GameSession.Instance.PlayerTracker.SetPlayerDisconnected(record.Unknown3[i].StationId);
                    }
                }
            }

            ActivePlayerCount = activeIds - disconnectedPlayers;
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

            Station station = GetStationForSsid(args.SourceStationId);

            using (MemoryStream stream = new MemoryStream(args.Data))
            using (BinaryDataReader reader = new BinaryDataReader(stream))
            {
                reader.Seek(2); // Bitflag
                byte seqState = reader.ReadByte();

                station.SeqState = seqState;

                if (Stations.Values.Where(s => s.SeqState == seqState).Count() == ActivePlayerCount)
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

            Station station = GetStationForSsid(args.SourceStationId);

            using (MemoryStream stream = new MemoryStream(args.Data))
            using (BinaryDataReader reader = new BinaryDataReader(stream))
            {
                station.Name = reader.ReadString(BinaryStringFormat.ZeroTerminated, Encoding.Unicode);
            }
        }

        private void HandlePlayerInfo(object sender, CloneChangedEventArgs args)
        {
            uint enlId = args.CloneId - 3;
            if (enlId >= 10)
            {
                return;
            }

            if (args.ElementId != 2)
            {
                return;
            }

            Station station = GetStationForSsid(args.SourceStationId);

            using (MemoryStream stream = new MemoryStream(args.Data))
            using (BinaryDataReader reader = new BinaryDataReader(stream))
            {
                station.PlayerInfo = args.Data;
            }
        }

        private void HandleMasterSeqState(object sender, CloneChangedEventArgs args)
        {
            if (args.CloneId != 2 || args.ElementId != 1)
            {
                return;
            }

            using (MemoryStream stream = new MemoryStream(args.Data))
            using (BinaryDataReader reader = new BinaryDataReader(stream))
            {
                reader.ByteOrder = ByteOrder.LittleEndian;

                byte seqState = reader.ReadByte();

                HandleSeqStateMaster(seqState);
            }
        }

    }
}
