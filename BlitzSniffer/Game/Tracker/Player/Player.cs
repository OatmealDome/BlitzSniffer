using Blitz.Cmn.Def;
using BlitzCommon.Blitz.Cmn.Def;

namespace BlitzSniffer.Game.Tracker.Player
{
    public class Player
    {
        public ulong SourceStationId
        {
            get;
            set;
        }

        public bool IsActive
        {
            get;
            set;
        }

        public bool IsDisconnected
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public Team Team
        {
            get;
            set;
        }

        public bool IsAlive
        {
            get;
            set;
        }

        public Gear Headgear
        {
            get;
            set;
        }

        public Gear Clothes
        {
            get;
            set;
        }

        public Gear Shoes
        {
            get;
            set;
        }

        public Weapon Weapon
        {
            get;
            set;
        }

        public uint SpecialGaugeCharge
        {
            get;
            set;
        }

        public bool IsInSpecial
        {
            get;
            set;
        }

        public uint Kills
        {
            get;
            set;
        }

        public uint Deaths
        {
            get;
            set;
        }

        public uint Assists
        {
            get;
            set;
        }

        public bool HasGachihoko
        {
            get;
            set;
        }

        public bool IsOnVLift
        {
            get;
            set;
        }

        public uint Clams
        {
            get;
            set;
        }

        public bool HasGoldenClam
        {
            get;
            set;
        }

        public PlayerSignal? LastSignalType
        {
            get;
            set;
        }

        public uint LastSignalExpirationTick
        {
            get;
            set;
        }

        public Player(string name)
        {
            SourceStationId = 0;
            IsActive = false;
            IsDisconnected = false;
            Name = name;
            Team = Team.Neutral;
            IsAlive = false;
            Weapon = null;
            SpecialGaugeCharge = 0;
            Kills = 0;
            Deaths = 0;
            Assists = 0;
            HasGachihoko = false;
            IsOnVLift = false;
            Clams = 0;
            HasGoldenClam = false;
            LastSignalType = null;
            LastSignalExpirationTick = 0;
        }

    }

}
