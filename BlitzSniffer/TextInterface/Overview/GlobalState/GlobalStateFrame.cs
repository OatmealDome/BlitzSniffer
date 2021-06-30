using BlitzSniffer.Game.Event;
using BlitzSniffer.Game.Event.Setup;
using BlitzSniffer.Game.Event.Versus;
using BlitzSniffer.Tracker;
using Terminal.Gui;

namespace BlitzSniffer.TextInterface.Overview.GlobalState
{
    class GlobalStateFrame : FrameView
    {
        enum MatchState
        {
            PreMatch,
            IntroDemo,
            Normal,
            Overtime
        }

        private const string STATUS_NO_EVENTS = "Waiting for the match to start.";
        private const string STATUS_WAIT_FOR_START = "Intro demo.";
        private const string STATUS_IN_GAME_NORMAL = "In-game. ({0} / {1} ticks)";
        private const string STATUS_IN_GAME_NO_TIMEOUT = "In-game.";
        private const string STATUS_IN_GAME_OVERTIME_TIMEOUT = "Overtime. ({0} / {1} ticks)";
        private const string STATUS_IN_GAME_OVERTIME_INFINITE = "Overtime.";

        private MatchState State;

        private uint TotalTicks;
        private uint CurrentTicks;
        private uint OvertimeTimeoutStartTicks;

        private Label WaitLabel;
        private Label StatusLabel;
        private ProgressBar GameProgressBar;

        public GlobalStateFrame() : base("State")
        {
            WaitLabel = new Label(STATUS_NO_EVENTS)
            {
                X = Pos.Center(),
                Y = Pos.Center(),
                TextAlignment = TextAlignment.Centered
            };

            this.Add(WaitLabel);

            StatusLabel = new Label(STATUS_NO_EVENTS)
            {
                X = Pos.Center(),
                Y = Pos.Center() - 1,
                Width = Dim.Fill(),
                Height = 1,
                TextAlignment = TextAlignment.Centered
            };

            this.Add(StatusLabel);

            GameProgressBar = new ProgressBar()
            {
                X = Pos.Center(),
                Y = Pos.Center() + 1,
                Width = Dim.Percent(95.0f),
                Height = 1,
                Fraction = 0.25f,
                ColorScheme = Colors.Error
            };

            this.Add(GameProgressBar);

            ChangeState(MatchState.PreMatch);

            EventTracker.Instance.SendEvent += HandleGameEvent;
            GameSession.Instance.GameTicked += HandleGameTicked;
        }

        private void ChangeState(MatchState state)
        {
            switch (state)
            {
                case MatchState.PreMatch:
                    WaitLabel.Text = STATUS_NO_EVENTS;
                    WaitLabel.Visible = true;

                    StatusLabel.Visible = false;

                    GameProgressBar.Fraction = 0.0f;
                    GameProgressBar.Visible = false;

                    TotalTicks = 0;
                    CurrentTicks = 0;
                    OvertimeTimeoutStartTicks = 0;

                    break;
                case MatchState.IntroDemo:
                    WaitLabel.Visible = false;

                    StatusLabel.Text = STATUS_WAIT_FOR_START;
                    StatusLabel.Visible = true;

                    GameProgressBar.Visible = true;

                    break;
                case MatchState.Normal:
                    break;
                case MatchState.Overtime:
                    break;
            }

            State = state;
        }

        private void UpdateStatus()
        {
            if (TotalTicks != 0)
            {
                uint currentTicks = CurrentTicks;
                uint totalTicks = TotalTicks;

                if (State == MatchState.Overtime)
                {
                    currentTicks -= OvertimeTimeoutStartTicks;
                }

                string formatStr = State == MatchState.Normal ? STATUS_IN_GAME_NORMAL : STATUS_IN_GAME_OVERTIME_TIMEOUT;
                StatusLabel.Text = string.Format(formatStr, currentTicks, totalTicks);
             
                GameProgressBar.Fraction = (float)currentTicks / totalTicks;
            }
            else
            {
                StatusLabel.Text = State == MatchState.Normal ? STATUS_IN_GAME_NO_TIMEOUT : STATUS_IN_GAME_OVERTIME_INFINITE;

                GameProgressBar.Pulse();
            }
        }

        private void HandleGameEvent(object sender, SendEventArgs args)
        {
            switch (args.GameEvent)
            {
                case SetupEvent setupEvent:
                    Application.MainLoop?.Invoke(() =>
                    {
                        // TOOD: Coop
                        if (setupEvent.Rule != "Paint")
                        {
                            TotalTicks = 5 * 60 * 60; // 5 mins * 60 secs/min * 60 ticks/sec
                        }
                        else
                        {
                            TotalTicks = 3 * 60 * 60; // 3 mins * 60 secs/min * 60 ticks/sec
                        }

                        ChangeState(MatchState.IntroDemo);
                    });

                    break;
                case SessionResetEvent resetEvent:
                    Application.MainLoop?.Invoke(() =>
                    {
                        ChangeState(MatchState.PreMatch);
                    });

                    break;
                case GachiOvertimeStartEvent overtimeStartEvent:
                    Application.MainLoop?.Invoke(() =>
                    {
                        ChangeState(MatchState.Overtime);
                    });

                    break;
                case GachiOvertimeTimeoutUpdateEvent overtimeTimeoutEvent:
                    Application.MainLoop?.Invoke(() =>
                    {
                        if (overtimeTimeoutEvent.Length == -1)
                        {
                            TotalTicks = 0;
                            OvertimeTimeoutStartTicks = 0;
                        }
                        else
                        {
                            TotalTicks = (uint)overtimeTimeoutEvent.Length;
                            OvertimeTimeoutStartTicks = GameSession.Instance.ElapsedTicks;
                        }
                    });

                    break;
            }
        }

        private void HandleGameTicked(object sender, GameTickedEventArgs args)
        {
            CurrentTicks = args.ElapsedTicks;

            if (State == MatchState.IntroDemo)
            {
                ChangeState(MatchState.Normal);
            }

            Application.MainLoop?.Invoke(() => UpdateStatus());
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
