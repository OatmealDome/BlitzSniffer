using Blitz.Cmn.Def;
using BlitzCommon.Blitz.Cmn.Def;
using BlitzSniffer.Clone;
using BlitzSniffer.Event;
using BlitzSniffer.Event.Player;
using BlitzSniffer.Event.Player.VGoal;
using BlitzSniffer.Event.Player.VLift;
using BlitzSniffer.Resources;
using BlitzSniffer.Tracker.Station;
using BlitzSniffer.Tracker.Versus.VLift;
using BlitzSniffer.Util;
using Syroot.BinaryData;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace BlitzSniffer.Tracker.Player
{
    class PlayerTracker : IDisposable
    {
        private static readonly uint SIGNAL_TIMEOUT = 40;

        private readonly Dictionary<uint, Player> Players;
        private readonly PlayerOffenseTracker OffenseTracker;

        private uint TeamBits;

        public PlayerTracker()
        {
            Players = new Dictionary<uint, Player>();
            OffenseTracker = new PlayerOffenseTracker();

            CloneHolder holder = CloneHolder.Instance;
            holder.CloneChanged += UpdatePlayerDetails; // we need player info ASAP so don't use in-game clone event

            for (uint i = 0; i < 10; i++)
            {
                holder.RegisterClone(i + 111);

                Players[i] = new Player($"Player {i}");
            }

            GameSession session = GameSession.Instance;
            session.InGameCloneChanged += HandlePlayerEvent;
            session.InGameCloneChanged += HandlePlayerNetState;
            session.InGameCloneChanged += HandlePlayerSignalEvent;

            session.GameTicked += HandleGameTick;
        }

        public void Dispose()
        {
            CloneHolder holder = CloneHolder.Instance;
            holder.CloneChanged -= UpdatePlayerDetails;

            GameSession session = GameSession.Instance;
            session.InGameCloneChanged -= HandlePlayerEvent;
            session.InGameCloneChanged -= HandlePlayerNetState;
            session.InGameCloneChanged -= HandlePlayerSignalEvent;

            session.GameTicked -= HandleGameTick;
        }

        public Player GetPlayer(uint idx)
        {
            return Players[idx];
        }

        public Player GetPlayerWithGachihoko()
        {
            IEnumerable<Player> gachihokoPlayers = Players.Values.Where(p => p.HasGachihoko);
            Trace.Assert(gachihokoPlayers.Count() <= 1, "More than one player with Gachihoko");

            return gachihokoPlayers.FirstOrDefault();
        }

        public int GetAcivePlayers()
        {
            return Players.Values.Where(p => p.IsActive && !p.IsDisconnected).Count();
        }

        public int GetPlayersOnVLift()
        {
            return Players.Values.Where(p => p.IsOnVLift).Count();
        }

        public void SetTeamBits(uint teamBits)
        {
            TeamBits = teamBits;
        }

        public void SetPlayerDisconnected(ulong stationId)
        {
            KeyValuePair<uint, Player> pair = Players.Where(p => p.Value.SourceStationId == stationId).FirstOrDefault();

            Player player = pair.Value;
            if (player != null && !player.IsDisconnected)
            {
                player.IsDisconnected = true;

                EventTracker.Instance.AddEvent(new PlayerDisconnectEvent()
                {
                    PlayerIdx = pair.Key
                });
            }
        }

        private void ApplyTeamBits()
        {
            uint neutralBits = TeamBits >> 16;
            uint actualTeamBits = TeamBits & 0xFFFF;

            for (uint i = 0; i != 10; i++)
            {
                uint mask = (uint)(1 << (int)i);

                Player player = Players[i];
                if ((neutralBits & mask) != 0 || !player.IsActive)
                {
                    player.Team = Team.Neutral;
                }
                else
                {
                    player.Team = (actualTeamBits & mask) != 0 ? Team.Bravo : Team.Alpha;
                }
            }
        }

        private void UpdatePlayerDetails(object sender, CloneChangedEventArgs args)
        {
            uint playerId = args.CloneId - 111;
            if (playerId >= 10)
            {
                return;
            }

            StationTracker tracker = GameSession.Instance.StationTracker;

            if (tracker.LastJointSeqState != 7)
            {
                return;
            }

            Player player = Players[playerId];

            if (!player.IsActive && !player.IsDisconnected)
            {
                
                Station.Station station = tracker.GetStationForSsid(args.SourceStationId);
                if (!station.IsSetup)
                {
                    return;
                }

                player.Name = station.Name;
                player.IsAlive = true;
                player.SourceStationId = args.SourceStationId;
                player.IsActive = true;

                using (MemoryStream stream = new MemoryStream(station.PlayerInfo))
                using (BinaryDataReader reader = new BinaryDataReader(stream))
                {
                    reader.Seek(8, SeekOrigin.Begin);

                    Weapon weapon = new Weapon();
                    weapon.Id = reader.ReadUInt32();
                    weapon.SubId = reader.ReadUInt32();
                    weapon.SpecialId = reader.ReadUInt32();
                    weapon.TurfInked = reader.ReadUInt32();

                    player.Weapon = weapon;
                    
                    Gear ReadGear()
                    {
                        Gear gear = new Gear();
                        gear.Id = reader.ReadUInt32();
                        gear.MainSkill = (GearSkill)reader.ReadUInt16();
                        gear.SecondarySkillOne = (GearSkill)reader.ReadUInt16();
                        gear.SecondarySkillTwo = (GearSkill)reader.ReadUInt16();
                        gear.SecondarySkillThree = (GearSkill)reader.ReadUInt16();
                        gear.Unk1 = reader.ReadUInt16();
                        gear.Unk2 = reader.ReadUInt16();
                        gear.ExpPoints = 0; // not transmitted

                        return gear;
                    }

                    reader.Seek(28, SeekOrigin.Begin);
                    player.Shoes = ReadGear();
                    player.Clothes = ReadGear();
                    player.Headgear = ReadGear();
                }

                if (Players.Values.Where(p => p.IsActive && !p.IsDisconnected).Count() == tracker.ActivePlayerCount)
                {
                    // Apply all team bits now that all players have been marked as active
                    ApplyTeamBits();

                    GameSession.Instance.SignalSetupReady();
                }
            }
        }

        private void HandlePlayerEvent(object sender, CloneChangedEventArgs args)
        {
            uint playerId = args.CloneId - 111;
            if (playerId >= 10)
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

                reader.Seek(15, SeekOrigin.Begin);
                uint unk7 = reader.ReadUInt32();
                uint unk8 = reader.ReadUInt32();

                BitReader bitReader = new BitReader(reader);
                uint eventId = bitReader.ReadVariableBits(7);
                uint unk10 = bitReader.ReadVariableBits(4) - 1;

                Player player = Players[playerId];

                // from Game::PlayerCloneHandle::unpackStateEvent()
                switch (eventId)
                {
                    case 1: // Killed
                    case 2: // OOB
                    case 3: // Water Hazard
                        if (!player.IsAlive)
                        {
                            return;
                        }

                        player.IsAlive = false;
                        player.IsInSpecial = false;
                        player.Deaths++;

                        if (player.HasGachihoko)
                        {
                            // Not anymore.
                            player.HasGachihoko = false;

                            EventTracker.Instance.AddEvent(new PlayerLostGachihokoEvent()
                            {
                                PlayerIdx = playerId
                            });
                        }

                        int attackerIdx = -1;
                        string cause = "Unknown";
                        
                        if (eventId == 1)
                        {
                            uint type = (unk8 & 0xFF0000) >> 16;
                            uint id = unk8 & 0xFFFF;

                            if (!GameSession.Instance.IsCoop)
                            {
                                cause = GetDeathCauseForAttackedPlayer(type, id);
                                
                                // Only set the attacker ID if it isn't the victim player (i.e. don't do it for suicides).
                                if ((int)unk10 != playerId)
                                {
                                    attackerIdx = (int)unk10;
                                }
                            }
                            else
                            {
                                cause = GetDeathCauseForAttackedPlayerCoop(type, id);
                            }
                        }
                        else if (eventId == 2)
                        {
                            cause = "Stg_OutOfBounds";
                        }
                        else if (eventId == 3)
                        {
                            cause = "Stg_Water";
                        }

                        PlayerDeathEvent deathEvent = OffenseTracker.GetDeathEventForVictim(playerId);
                        deathEvent.AttackerIdx = attackerIdx;
                        deathEvent.Cause = cause;
                        deathEvent.IsComplete = true;

                        break;
                    case 4: // Revival (after being splatted)
                    case 5: // Recover (after entering out of bounds or water)
                        if (GameSession.Instance.IsCoop)
                        {
                            // During Coop, we listen for the rescue events instead.
                            return;
                        }
                        
                        if (player.IsAlive)
                        {
                            return;
                        }

                        player.IsAlive = true;

                        EventTracker.Instance.AddEvent(new PlayerRespawnEvent()
                        {
                            PlayerIdx = playerId
                        });

                        break;
                    case 6: // Assist
                        PlayerDeathEvent assistDeathEvent = OffenseTracker.GetDeathEventForVictim(unk10);
                        assistDeathEvent.AssisterIdx = (int)playerId;

                        break;
                    case 22: // PerformSpecial
                        // TODO: what happens when internal specials like BigLaser are activated?
                        
                        if (player.IsInSpecial || !player.IsAlive)
                        {
                            return;
                        }

                        if (player.SpecialGaugeCharge < 80)
                        {
                            return;
                        }

                        EventTracker.Instance.AddEvent(new PlayerSpecialActivateEvent()
                        {
                            PlayerIdx = playerId
                        });

                        player.IsInSpecial = true;

                        break;
                    case 52: // Coop_???
                    case 53: // Coop_GetRescued
                    case 54: // Coop_GetRescuedZombie
                        if (player.IsAlive)
                        {
                            return;
                        }

                        player.IsAlive = true;

                        EventTracker.Instance.AddEvent(new PlayerCoopRescuedEvent()
                        {
                            PlayerIdx = playerId,
                            SaviourIdx = (int)unk10
                        });
                        
                        break;
                    default:
                        break;
                }
            }
        }

        private void HandlePlayerNetState(object sender, CloneChangedEventArgs args)
        {
            uint playerId = args.CloneId - 111;
            if (playerId >= 10)
            {
                return;
            }

            if (args.ElementId != 0)
            {
                return;
            }

            Player player = Players[playerId];

            using (MemoryStream stream = new MemoryStream(args.Data))
            using (BinaryDataReader reader = new BinaryDataReader(stream))
            {
                reader.ByteOrder = ByteOrder.LittleEndian;

                BitReader bitReader = new BitReader(reader);

                // TODO: clean up

                // mPos
                bitReader.Seek(16 * 3);
                bitReader.Seek(1 * 3);

                // mMoveVel
                bitReader.Seek(11);
                bitReader.Seek(12);
                bitReader.Seek(12);

                // mJumpVel
                bitReader.Seek(12);

                // mJumpVel_Leak
                bitReader.Seek(12);

                // mPosY_Leap
                bitReader.Seek(17);

                // mReactVel
                bitReader.Seek(11);
                bitReader.Seek(12);
                bitReader.Seek(12);

                // mGndNrm_Raw
                bitReader.Seek(11);
                bitReader.Seek(12);

                // mAttDirZ
                bitReader.Seek(11);
                bitReader.Seek(12);

                // mShotDirXZ
                bitReader.Seek(14);

                // mUnk4
                bitReader.Seek(6);

                // mUnk5
                bitReader.Seek(2 + 10);

                // mUnk6
                bitReader.Seek(8);

                // special gauge
                uint charge = bitReader.ReadVariableBits(7);
                if (player.SpecialGaugeCharge != charge)
                {
                    player.SpecialGaugeCharge = charge;

                    if (charge == 0)
                    {
                        player.IsInSpecial = false;
                    }

                    EventTracker.Instance.AddEvent(new PlayerGaugeUpdateEvent()
                    {
                        PlayerIdx = playerId,
                        Charge = player.SpecialGaugeCharge
                    });
                }

                // mUnk8
                bitReader.Seek(10);

                // mUnk9
                bitReader.Seek(10);

                // mUnk10
                bitReader.Seek(17);

                // mUnk?? (not in 3.1.0)
                bitReader.Seek(17);

                // mUnk11
                bitReader.Seek(2 + 10);

                // mUnk12
                bitReader.Seek(9);

                // mUnk13
                bitReader.Seek(4);

                // flags
                uint flags = bitReader.ReadVariableBits(32);
                if (GameSession.Instance.GameStateTracker is VLiftVersusGameStateTracker)
                {
                    UpdateVLiftRidingStatus(playerId, (flags & 0x8000000) != 0);
                }
            }
        }

        private void HandlePlayerSignalEvent(object sender, CloneChangedEventArgs args)
        {
            uint playerId = args.CloneId - 111;
            if (playerId >= 10)
            {
                return;
            }

            if (args.ElementId != 3)
            {
                return;
            }

            Player player = Players[playerId];

            using (MemoryStream stream = new MemoryStream(args.Data))
            using (BinaryDataReader reader = new BinaryDataReader(stream))
            {
                reader.ByteOrder = ByteOrder.LittleEndian;

                ushort gameSignalType = reader.ReadUInt16();

                PlayerSignal eventSignalType;
                if (gameSignalType == 0)
                {
                    if (player.IsAlive)
                    {
                        eventSignalType = PlayerSignal.ThisWay;
                    }
                    else
                    {
                        if (GameSession.Instance.IsCoop)
                        {
                            // Help!
                            eventSignalType = PlayerSignal.Help;
                        }
                        else
                        {
                            // Ouch!
                            eventSignalType = PlayerSignal.Ouch;
                        }
                    }
                }
                else
                {
                    // Booyah!
                    eventSignalType = PlayerSignal.Booyah;
                }

                if (eventSignalType == player.LastSignalType)
                {
                    return;
                }

                player.LastSignalType = eventSignalType;
                player.LastSignalExpirationTick = GameSession.Instance.ElapsedTicks + SIGNAL_TIMEOUT;

                EventTracker.Instance.AddEvent(new PlayerSignalEvent()
                {
                    PlayerIdx = playerId,
                    SignalType = Enum.GetName(typeof(PlayerSignal), eventSignalType)
                });
            }
        }

        private void HandleGameTick(object sender, GameTickedEventArgs args)
        {
            foreach (Player player in Players.Values.Where(p => p.LastSignalType.HasValue))
            {
                if (player.LastSignalExpirationTick <= args.ElapsedTicks)
                {
                    player.LastSignalType = null;
                    player.LastSignalExpirationTick = 0;
                }
            }
        }

        // from Game::VersusBeatenPage::start()
        private string GetDeathCauseForAttackedPlayer(uint type, uint id)
        {
            string cause = $"Unknown ({type} - {id})";
            switch (type)
            {
                case 0:
                    cause = WeaponResource.Instance.GetMainWeapon((int)id);
                    break;
                case 1:
                    cause = WeaponResource.Instance.GetSubWeapon((int)id);
                    break;
                case 2:
                    cause = WeaponResource.Instance.GetSpecialWeapon((int)id);
                    break;
                case 3:
                    switch (id)
                    {
                        // Cases 4 to 7 were found in the array used for Cui::TextSetter::setLayoutMsg
                        case 4: // Gachihoko explosion (holder?)
                        case 6: // Gachihoko explosion (others?)
                            cause = "Wsp_Shachihoko_Explosion";
                            break;
                        case 5: // Gachihoko bullet
                            cause = "Wsp_Shachihoko";
                            break;
                        case 7: // Gachihoko barrier
                            cause = $"Wsp_Shachihoko_Barrier";
                            break;
                        case 8:
                            cause = "Wot_PCFan";
                            break;
                        case 11:
                            cause = "Wot_Geyser";
                            break;
                        case 12:
                            cause = "Wot_Takodozer";
                            break;
                        case 13:
                            cause = "Wot_RollingBarrel";
                            break;
                        case 14:
                            cause = "Wot_Blowouts";
                            break;
                        case 15:
                            cause = "Wsp_BigLaser";
                            break;
                        case 16:
                            cause = "Wot_IidaBomb";
                            break;
                    }
                    break;
                case 4: // Crushed
                    cause = WeaponResource.Instance.GetMainWeapon((int)id);
                    break;
                case 5:
                    cause = $"Wsp_Jetpack_Exhaust";
                    break;
                case 6: // Squished
                    cause = WeaponResource.Instance.GetMainWeapon((int)id);
                    break;
                case 7: // Inksploded
                    cause = WeaponResource.Instance.GetSpecialWeapon((int)id);
                    break;
                case 9:
                case 13:
                case 14:
                    cause = WeaponResource.Instance.GetMainWeapon((int)id);
                    break;
                case 10:
                    cause = WeaponResource.Instance.GetSubWeapon((int)id);
                    break;
                case 11:
                case 12:
                    cause = WeaponResource.Instance.GetSpecialWeapon((int)id);
                    break;
            }

            return cause;
        }

        private string GetDeathCauseForAttackedPlayerCoop(uint type, uint id)
        {
            // This should always be the case for Coop
            Trace.Assert(type == 8);

            switch (id)
            {
                case 0:
                    return "Coop_SakelienStandard";
                case 1:
                    return "Coop_SakelienLarge";
                case 2:
                    return "Coop_SakelienSmall";
                case 3:
                    return "Coop_SakelienGolden";
                case 4:
                    return "Coop_SakelienBagman";
                case 5:
                    return "Coop_SakelienBagmanLarge";
                case 6:
                    return "Coop_SakelienBomber";
                case 7:
                    return "Coop_SakelienCannon";
                case 8:
                    return "Coop_SakelienCup";
                case 9:
                    return "Coop_SakelienCupTwins";
                case 10:
                    return "Coop_SakelienEscape";
                case 11:
                    return "Coop_SakelienGeyser";
                case 12:
                    return "Coop_SakelienShield";
                case 13:
                    return "Coop_SakelienSnake";
                case 14:
                    return "Coop_SakelienTower";
                case 15:
                    return "Coop_Sakediver";
                case 16:
                    return "Coop_Sakedozer";
                case 17:
                    return "Coop_Sakeflyer";
                case 18:
                    return "Coop_Sakepuncher";
                case 19:
                    return "Coop_SakepuncherBulletSimpl";
                case 20:
                    return "Coop_SakepuncherBulletPunch";
                case 21:
                    return "Coop_Sakerocket";
                case 22:
                    return "Coop_SakerocketBullet";
                default:
                    return $"Unknown - Coop type ({id})";
            }
        }

        private void UpdateVLiftRidingStatus(uint id, bool riding)
        {
            Player player = Players[id];

            if (player.IsOnVLift != riding)
            {
                player.IsOnVLift = riding;

                if (riding)
                {
                    EventTracker.Instance.AddEvent(new PlayerRidingVLiftEvent()
                    {
                        PlayerIdx = id
                    });
                }
                else
                {
                    EventTracker.Instance.AddEvent(new PlayerLeftVLiftEvent()
                    {
                        PlayerIdx = id
                    });
                }

                VLiftVersusGameStateTracker tracker = GameSession.Instance.GameStateTracker as VLiftVersusGameStateTracker;
                tracker.UpdateOvertimeTimeoutState();
            }
        }

    }
}
