using BlitzSniffer.Game.Event;
using BlitzSniffer.Game.Event.Player;
using BlitzSniffer.Game.Event.Player.VClam;
using BlitzSniffer.Game.Event.Player.VGoal;
using BlitzSniffer.Game.Event.Player.VLift;
using BlitzSniffer.Game.Event.Versus;
using BlitzSniffer.Game.Event.Versus.VArea;
using BlitzSniffer.Game.Event.Versus.VClam;
using BlitzSniffer.Game.Event.Versus.VGoal;
using BlitzSniffer.Game.Event.Versus.VLift;
using BlitzSniffer.Game.Tracker;
using BlitzSniffer.Game.Tracker.Player;
using BlitzSniffer.Game.Tracker.Versus;
using Serilog;
using Serilog.Core;
using System.Text.Json;

namespace BlitzSniffer
{
    class LocalLog
    {
        private static readonly ILogger LogContext = Log.ForContext(Constants.SourceContextPropertyName, "LocalLog");

        public static void RegisterConsoleDebug()
        {
            EventTracker.Instance.SendEvent += PrintEventToConsole;
        }

        static void PrintEventToConsole(object sender, SendEventArgs args)
        {
            switch (args.Event)
            {
                case PlayerDeathEvent deathEvent:
                    PlayerTracker tracker = GameSession.Instance.PlayerTracker;
                    Player victimPlayer = tracker.GetPlayer(deathEvent.PlayerIdx);
                    Player attackerPlayer = deathEvent.AttackerIdx != -1 ? tracker.GetPlayer((uint)deathEvent.AttackerIdx) : null;
                    Player assisterPlayer = deathEvent.AssisterIdx != -1 ? tracker.GetPlayer((uint)deathEvent.AssisterIdx) : null;

                    string deathStr;
                    if (!GameSession.Instance.IsCoop)
                    {
                        if (attackerPlayer != null)
                        {
                            deathStr = $"{victimPlayer.Name} was killed by {attackerPlayer.Name} using {deathEvent.Cause}";
                        }
                        else
                        {
                            deathStr = $"{victimPlayer.Name} died by {deathEvent.Cause}";
                        }

                        if (assisterPlayer != null)
                        {
                            deathStr += $" with help from {assisterPlayer.Name}";
                        }
                    }
                    else
                    {
                        deathStr = $"{victimPlayer.Name} was killed by {deathEvent.Cause}";
                    }

                    LogContext.Information("PlayerDeath: {DeathString}", deathStr);

                    break;
                case PlayerRespawnEvent respawnEvent:
                    LogContext.Information("PlayerRespawn: {Name} respawned", GameSession.Instance.PlayerTracker.GetPlayer(respawnEvent.PlayerIdx).Name);

                    break;
                case PlayerSignalEvent signalEvent:
                    string signal;
                    switch (signalEvent.SignalType)
                    {
                        case "ThisWay":
                            signal = "This way!";
                            break;
                        case "Ouch":
                            signal = "Ouch...";
                            break;
                        case "Booyah":
                            signal = "Booyah!";
                            break;
                        case "Help":
                            signal = "Help!";
                            break;
                        default:
                            signal = "Unknown signal";
                            break;
                    }

                    LogContext.Information("PlayerSignal: {Name} says {Signal}", GameSession.Instance.PlayerTracker.GetPlayer(signalEvent.PlayerIdx).Name, signal);

                    break;
                case PlayerSpecialActivateEvent specialEvent:
                    LogContext.Information("PlayerSpecialActivate: {Name} activated their special weapon.", GameSession.Instance.PlayerTracker.GetPlayer(specialEvent.PlayerIdx).Name);

                    break;
                case PlayerGetGachihokoEvent getGachihokoEvent:
                    LogContext.Information("PlayerGetGachihokoEvent: {Name} got the Rainmaker", GameSession.Instance.PlayerTracker.GetPlayer(getGachihokoEvent.PlayerIdx).Name);

                    break;
                case PlayerLostGachihokoEvent lostGachihokoEvent:
                    LogContext.Information("PlayerLostGachihokoEvent: {Name} lost the Rainmaker", GameSession.Instance.PlayerTracker.GetPlayer(lostGachihokoEvent.PlayerIdx).Name);

                    break;
                case PlayerRidingVLiftEvent ridingEvent:
                    LogContext.Information("PlayerRidingVLift: {Name} is riding the Tower", GameSession.Instance.PlayerTracker.GetPlayer(ridingEvent.PlayerIdx).Name);

                    break;
                case PlayerLeftVLiftEvent leftEvent:
                    LogContext.Information("PlayerLeftVLift: {Name} is no longer riding the Tower", GameSession.Instance.PlayerTracker.GetPlayer(leftEvent.PlayerIdx).Name);

                    break;
                case PlayerClamGoldenTakeEvent goldenTakeEvent:
                    LogContext.Information("PlayerClamGoldenTake: {Name} now has a Power Clam", GameSession.Instance.PlayerTracker.GetPlayer(goldenTakeEvent.PlayerIdx).Name);

                    break;
                case PlayerClamGoldenLostEvent goldenLostEvent:
                    LogContext.Information("PlayerClamGoldenLost: {Name} lost their Power Clam", GameSession.Instance.PlayerTracker.GetPlayer(goldenLostEvent.PlayerIdx).Name);

                    break;
                case PlayerCoopRescuedEvent coopRescuedEvent:
                    PlayerTracker coopRescuePlayerTracker = GameSession.Instance.PlayerTracker;
                    Player coopVictimPlayer = coopRescuePlayerTracker.GetPlayer(coopRescuedEvent.PlayerIdx);
                    Player coopSaviourPlayer = coopRescuedEvent.SaviourIdx != -1 ? coopRescuePlayerTracker.GetPlayer((uint)coopRescuedEvent.SaviourIdx) : null;

                    if (coopSaviourPlayer != null)
                    {
                        LogContext.Information("PlayerCoopRescued: {VictimName} was rescued by {SaviourName}", coopVictimPlayer.Name, coopSaviourPlayer.Name);
                    }
                    else
                    {
                        LogContext.Information("PlayerCoopRescued: {VictimName} was automatically revived", coopVictimPlayer.Name);
                    }
                    
                    break;
                case PlayerDisconnectEvent disconnectEvent:
                    Player disconnectedPlayer = GameSession.Instance.PlayerTracker.GetPlayer(disconnectEvent.PlayerIdx);

                    LogContext.Warning("PlayerDisconnect: {Name} disconnected", disconnectedPlayer.Name);

                    break;
                case PaintFinishEvent paintFinishEvent:
                    LogContext.Information("PaintFinish: game finish, {AlphaScore}p - {BravoScore}p", paintFinishEvent.AlphaPoints, paintFinishEvent.BravoPoints);

                    break;
                case GachiScoreUpdateEvent scoreUpdateEvent:
                    GachiVersusGameStateTracker gachiTracker = GameSession.Instance.GameStateTracker as GachiVersusGameStateTracker;
                    
                    if (gachiTracker.HasPenalties)
                    {
                        LogContext.Information("GachiScoreUpdate: alpha {AlphaScore} + {AlphaPenalty}, bravo {BravoScore} + {BravoPenalty}", scoreUpdateEvent.AlphaScore, scoreUpdateEvent.AlphaPenalty, scoreUpdateEvent.BravoScore, scoreUpdateEvent.BravoPenalty);
                    }
                    else
                    {
                        LogContext.Information("GachiScoreUpdate: alpha {AlphaScore}, bravo {BravoScore}", scoreUpdateEvent.AlphaScore, scoreUpdateEvent.BravoScore);
                    }
                    
                    break;
                case GachiOvertimeStartEvent overtimeStartEvent:
                    LogContext.Information("GachiOvertimeStart: overtime is starting now");
                    
                    break;
                case GachiOvertimeTimeoutUpdateEvent overtimeTimeoutEvent:
                    LogContext.Information("GachiOvertimeTimeoutUpdate: overtime will end in {Ticks} ticks", overtimeTimeoutEvent.Length);
                    
                    break;
                case GachiFinishEvent finishEvent:
                    LogContext.Information("GachiFinish: game finish, {AlphaScore} - {BravoScore}", finishEvent.AlphaScore, finishEvent.BravoScore);

                    break;
                case VGoalBarrierBreakEvent barrierBreakEvent:
                    LogContext.Information("VGoalBarrierBreak: Rainmaker barrier was broken by {Name}", GameSession.Instance.PlayerTracker.GetPlayer(barrierBreakEvent.BreakerPlayerIdx).Name);

                    break;
                /*case VLiftPositionUpdateEvent positionEvent:
                    LogContext.Information("VLiftPositionUpdate: alpha {AlphaPosition}%, bravo {BravoPosition}%", positionEvent.AlphaPosition, positionEvent.BravoPosition);
                
                    break;*/
                case VLiftCheckpointUpdateEvent checkpointUpdateEvent:
                    LogContext.Information("VLiftCheckpointUpdate: team {Team}, checkpoint {Index}, HP {Hp}, best HP {BestHp}", checkpointUpdateEvent.Team, checkpointUpdateEvent.Idx, checkpointUpdateEvent.Hp, checkpointUpdateEvent.BestHp);

                    break;
                case VAreaPaintAreaCappedStateUpdateEvent cappedEvent:
                    LogContext.Information("VAreaPaintAreaCappedStateUpdate: area {AreaIndex}, capped team has {Paint}% of the area", cappedEvent.AreaIdx, cappedEvent.PaintPercentage * 100f);

                    break;
                case VAreaPaintAreaContestedStateUpdateEvent contestedEvent:
                    LogContext.Information("VAreaPaintAreaContestedStateUpdate: area {AreaIndex}, paint {Paint}% in favour of {FavouredTeam}", contestedEvent.AreaIdx, contestedEvent.PaintPercentage * 100f, contestedEvent.FavouredTeam);
                    
                    break;
                case VAreaPaintAreaControlChangeEvent changeEvent:
                    LogContext.Information("VAreaPaintAreaControlChange: area {AreaIndex}, controlled team {Team}", changeEvent.AreaIdx, changeEvent.Team);

                    break;
                case VClamBasketBreakEvent basketBreakEvent:
                    LogContext.Information("VClamBasketBreak: {Team}'s basket was broken", basketBreakEvent.Team);

                    break;
                case VClamBasketClosedEvent basketClosedEvent:
                    LogContext.Information("VClamBasketClosed: {Team}'s basket has closed", basketClosedEvent.Team);

                    break;
                case VClamBasketTimeoutUpdateEvent basketTimeoutEvent:
                    LogContext.Information("VClamBasketTimeoutUpdate: {Team}'s basket will now close in {Ticks} ticks", basketTimeoutEvent.Team, basketTimeoutEvent.Timeout);

                    break;
                case VClamBasketVulnerabilityUpdateEvent basketVulnUpdateEvent:
                    LogContext.Information("VClamBasketVulnerabilityUpdate: {Team}'s basket is now {BasketState}", basketVulnUpdateEvent.Team, basketVulnUpdateEvent.IsInvincible ? "invincible" : "vulnerable");

                    break;
                case PlayerClamNormalCountUpdateEvent clamCountEvent:
                case PlayerGaugeUpdateEvent gaugeEvent:
                    break;
                default:
                    string json = JsonSerializer.Serialize(args.Event, args.Event.GetType(), new JsonSerializerOptions()
                    {
                        WriteIndented = true
                    });

                    string linePrefix = args.Event.Name + ": {Json}";
                    LogContext.Information(linePrefix, json);

                    break;
            }
        }

    }
}
