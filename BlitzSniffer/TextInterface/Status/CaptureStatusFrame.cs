using BlitzSniffer.Network.Manager;
using BlitzSniffer.Util;
using SharpPcap;
using Terminal.Gui;

namespace BlitzSniffer.TextInterface.Status
{
    class CaptureStatusFrame : FrameView
    {
        private ProgressBar CaptureProgressBar;
        private Label TimeLabel;

        public CaptureStatusFrame() : base("Capture Status")
        {
            CaptureProgressBar = new ProgressBar()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = 1,
                Fraction = 0.0f,
                ColorScheme = Colors.Error
            };

            CaptureProgressBar.MouseClick += HandleBarClicked;

            this.Add(CaptureProgressBar);

            TimeLabel = new Label()
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                Height = 1,
                Text = "Waiting for capture to start."
            };

            this.Add(TimeLabel);

            NetworkManager.Instance.PacketReceived += HandlePacketReceived;
        }

        private void HandleBarClicked(MouseEventArgs args)
        {
            if (args.MouseEvent.Flags != MouseFlags.Button1Released)
            {
                return;
            }

            double fraction = (float)args.MouseEvent.X / CaptureProgressBar.Frame.Width;
            ulong microseconds = (ulong)(NetworkManager.Instance.GetTotalReplayLength().ToTotalMicroseconds() * fraction);

            NetworkManager.Instance.SeekReplay(microseconds);
        }

        private void HandlePacketReceived(object sender, PacketReceivedEventArgs args)
        {
            if (!NetworkManager.Instance.IsRealTimeReplayLoaded())
            {
                Application.MainLoop?.Invoke(() =>
                {
                    TimeLabel.Text = "Capturing.";
                    CaptureProgressBar.Pulse();
                });
            }
            else
            {
                PosixTimeval firstPacketTimeval = NetworkManager.Instance.GetFirstPacketTimeReplay();
                PosixTimeval totalReplayLengthTimeval = NetworkManager.Instance.GetTotalReplayLength();
                PosixTimeval correctedCaptureTimeval = args.RawCapture.Timeval.Subtract(firstPacketTimeval);

                Application.MainLoop?.Invoke(() =>
                {
                    TimeLabel.Text = $"{correctedCaptureTimeval.Seconds} / {totalReplayLengthTimeval.Seconds}";
                    CaptureProgressBar.Fraction = (float)correctedCaptureTimeval.ToTotalMicroseconds() / totalReplayLengthTimeval.ToTotalMicroseconds();
                });
            }
        }

    }
}
