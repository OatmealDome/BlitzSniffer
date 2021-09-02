using BlitzSniffer.Game.Event;
using BlitzSniffer.Game.Event.Player;
using BlitzSniffer.Game.Event.Player.VClam;
using BlitzSniffer.Game.Event.Setup;
using BlitzSniffer.Game.Event.Setup.Player;
using System.Collections.Generic;
using Terminal.Gui;

namespace BlitzSniffer.TextInterface.Overview.Player
{
    class PlayerFrame : FrameView
    {
        private const int PLAYER_LABEL_ALPHA_Y_START = 1;
        private const int PLAYER_LABEL_BRAVO_Y_START = 5;

        // 2 (spacing) + 3 (player status, spacing) + 10 (characters of name) 
        private const int PLAYER_LABEL_LENGTH = 2 + 3 + 10;

        private Dictionary<uint, int> PidToUiId;
        private Dictionary<int, string> PlayerNames;
        private Dictionary<int, bool> PlayerIsInSpecial;

        private List<Label> NameLabels;
        private List<Label> SpecialGaugeLabels;
        private List<Label> ModeStatusLabels;

        private Label AlphaLabel;
        private Label BravoLabel;
        private Label WaitingLabel;

        public PlayerFrame() : base("Players")
        {
            PidToUiId = new Dictionary<uint, int>();
            PlayerNames = new Dictionary<int, string>();
            PlayerIsInSpecial = new Dictionary<int, bool>();

            NameLabels = new List<Label>();
            SpecialGaugeLabels = new List<Label>();
            ModeStatusLabels = new List<Label>();

            AlphaLabel = new Label("Alpha")
            {
                X = 2,
                Y = PLAYER_LABEL_ALPHA_Y_START + 1,
                Visible = false
            };
            
            this.Add(AlphaLabel);

            BravoLabel = new Label("Bravo")
            {
                X = 2,
                Y = PLAYER_LABEL_BRAVO_Y_START + 1,
                Visible = false
            };

            this.Add(BravoLabel);

            WaitingLabel = new Label("Waiting for player data...")
            {
                X = 2,
                Y = 1,
                Visible = true
            };

            this.Add(WaitingLabel);

            for (int i = 0; i < 8; i++)
            {
                // 2 (spacing) + 5 (team name) + 3 (spacing)
                int x = 2 + 5 + 3 + (PLAYER_LABEL_LENGTH * (i < 4 ? i : i - 4));

                // IDs 0 ~ 3 are Alpha, 4 ~ 8 are Bravo
                int y = i < 4 ? PLAYER_LABEL_ALPHA_Y_START : PLAYER_LABEL_BRAVO_Y_START;

                Label nameLabel = new Label()
                {
                    X = x,
                    Y = y,
                    Width = PLAYER_LABEL_LENGTH,
                    Height = 1,
                };

                Label specialLabel = new Label()
                {
                    X = x,
                    Y = y + 1,
                    Width = PLAYER_LABEL_LENGTH,
                    Height = 1,
                };

                Label statusLabel = new Label()
                {
                    X = x,
                    Y = y + 2,
                    Width = PLAYER_LABEL_LENGTH,
                    Height = 1,
                };

                NameLabels.Add(nameLabel);
                SpecialGaugeLabels.Add(specialLabel);
                ModeStatusLabels.Add(statusLabel);

                this.Add(nameLabel);
                this.Add(specialLabel);
                this.Add(statusLabel);

                ResetPlayer(i);
            }

            EventTracker.Instance.SendEvent += HandleGameEvent;
        }

        private void ResetPlayer(int idx)
        {
            PlayerNames[idx] = $"InvalName{idx}";
            PlayerIsInSpecial[idx] = false;

            Label nameLabel = NameLabels[idx];
            nameLabel.Text = $"E  PlayerLbl{idx}";
            nameLabel.Visible = false;

            Label specialLabel = SpecialGaugeLabels[idx];
            specialLabel.Text = "   SP   ERR ";
            specialLabel.Visible = false;

            Label modeLabel = ModeStatusLabels[idx];
            modeLabel.Text = "   ERROR";
            modeLabel.Visible = false;
        }

        private void ActivatePlayer(int idx, string name)
        {
            PlayerNames[idx] = name;

            Label nameLabel = NameLabels[idx];
            nameLabel.Text = $"A  {name}";
            nameLabel.Visible = true;

            Label specialLabel = SpecialGaugeLabels[idx];
            specialLabel.Text = "   SP   0%";
            specialLabel.Visible = true;

            Label modeLabel = ModeStatusLabels[idx];
            modeLabel.Text = "";
            modeLabel.Visible = true;
        }

        private void SetPlayerSmallStatus(int idx, char c)
        {
            string playerName = PlayerNames[idx];

            Label nameLabel = NameLabels[idx];
            nameLabel.Text = $"{c}  {playerName}";
        }

        private void SetPlayerGauge(int idx, uint gauge)
        {
            Label gaugeLabel = SpecialGaugeLabels[idx];
            gaugeLabel.Text = $"   SP   {gauge}%";
        }

        private void SetPlayerInSpecial(int idx)
        {
            Label gaugeLabel = SpecialGaugeLabels[idx];
            gaugeLabel.Text = $"   SP   ACT";
        }

        private void SetPlayerClamCount(int idx, uint count)
        {
            Label modeLabel = ModeStatusLabels[idx];
            modeLabel.Text = $"   CLAM  {count}";
        }

        private void SetPlayerClamGolden(int idx)
        {
            Label modeLabel = ModeStatusLabels[idx];
            modeLabel.Text = "   CLAM POW"; 
        }

        private void HandleGameEvent(object sender, SendEventArgs args)
        {
            switch (args.Event)
            {
                case SetupEvent setupEvent:
                    Application.MainLoop?.Invoke(() =>
                    {
                        for (int i = 0; i < setupEvent.Teams[0].Players.Count; i++)
                        {
                            SetupPlayer player = setupEvent.Teams[0].Players[i] as SetupPlayer;

                            PidToUiId[player.Id] = i;
                            ActivatePlayer(i, player.Name);
                        }

                        for (int i = 0; i < setupEvent.Teams[1].Players.Count; i++)
                        {
                            SetupPlayer player = setupEvent.Teams[1].Players[i] as SetupPlayer;

                            PidToUiId[player.Id] = i + 4;
                            ActivatePlayer(i + 4, player.Name);
                        }

                        AlphaLabel.Visible = true;
                        BravoLabel.Visible = true;
                        WaitingLabel.Visible = false;
                    });

                    break;
                case SessionResetEvent resetEvent:
                    Application.MainLoop?.Invoke(() =>
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            ResetPlayer(i);
                        }

                        PidToUiId.Clear();
                        PlayerNames.Clear();

                        AlphaLabel.Visible = false;
                        BravoLabel.Visible = false;
                        WaitingLabel.Visible = true;
                    });

                    break;
                case PlayerDeathEvent deathEvent:
                    int deathUid = PidToUiId[deathEvent.PlayerIdx];

                    PlayerIsInSpecial[deathUid] = false;

                    Application.MainLoop?.Invoke(() =>
                    {
                        SetPlayerSmallStatus(deathUid, 'D');
                        SetPlayerGauge(deathUid, 0);
                    });
                    break;
                case PlayerRespawnEvent respawnEvent:
                    Application.MainLoop?.Invoke(() => SetPlayerSmallStatus(PidToUiId[respawnEvent.PlayerIdx], 'A'));
                    break;
                case PlayerGaugeUpdateEvent gaugeEvent:
                    int gaugeUid = PidToUiId[gaugeEvent.PlayerIdx];

                    if (PlayerIsInSpecial[gaugeUid])
                    {
                        if (gaugeEvent.Charge == 0)
                        {
                            PlayerIsInSpecial[gaugeUid] = false;
                        }
                        else
                        {
                            return;
                        }
                    }

                    Application.MainLoop?.Invoke(() =>
                    {
                        SetPlayerGauge(gaugeUid, gaugeEvent.Charge);
                    });
                    break;
                case PlayerSpecialActivateEvent activateEvent:
                    int activateUid = PidToUiId[activateEvent.PlayerIdx];

                    Application.MainLoop?.Invoke(() =>
                    {
                        PlayerIsInSpecial[activateUid] = true;
                        SetPlayerInSpecial(activateUid);
                    });

                    break;
                case PlayerClamNormalCountUpdateEvent clamCountEvent:
                    int clamCountUid = PidToUiId[clamCountEvent.PlayerIdx];

                    Application.MainLoop?.Invoke(() =>
                    {
                        SetPlayerClamCount(clamCountUid, clamCountEvent.Clams);
                    });

                    break;
                case PlayerClamGoldenTakeEvent goldenTakeEvent:
                    int goldenTakeUid = PidToUiId[goldenTakeEvent.PlayerIdx];
                    
                    Application.MainLoop?.Invoke(() =>
                    {
                        SetPlayerClamGolden(goldenTakeUid);
                    });

                    break;
                case PlayerClamGoldenLostEvent goldenLostEvent:
                    int goldenLostUid = PidToUiId[goldenLostEvent.PlayerIdx];
                    
                    Application.MainLoop?.Invoke(() =>
                    {
                        SetPlayerClamCount(goldenLostUid, 0);
                    });

                    break;
                default:
                    return;
            }
        }

        // Workaround for bug where TabView doesn't redraw contents
        // https://github.com/migueldeicaza/gui.cs/issues/1353
        public override void Redraw(Rect bounds)
        {
            base.Redraw(bounds);

            if (this.SuperView != null)
            {
                SuperView.SetNeedsDisplay();
            }
        }

    }
}
