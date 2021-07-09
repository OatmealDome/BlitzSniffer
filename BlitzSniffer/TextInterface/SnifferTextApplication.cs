using BlitzSniffer.TextInterface.Overview;
using BlitzSniffer.TextInterface.Status;
using BlitzSniffer.Util;
using System;
using Terminal.Gui;

namespace BlitzSniffer.TextInterface
{
    class SnifferTextApplication : Window
    {
        public SnifferTextApplication() : base("BlitzSniffer")
        {
            TabView tabView = new TabView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(4) // space left for CaptureStatusFrame
            };

            // Console tab

            TextView textView = new TextView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            tabView.AddTab(new TabView.Tab("Console", textView), true);

            Console.SetOut(new TextViewWriter(textView));

            // Game Overview tab

            GameOverviewView overviewView = new GameOverviewView()
            {
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            tabView.AddTab(new TabView.Tab("Game Overview", overviewView), false);

            this.Add(tabView);

            // Capture Status

            CaptureStatusFrame statusFrame = new CaptureStatusFrame()
            {
                X = 0,
                Y = Pos.Bottom(tabView),
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            this.Add(statusFrame);
        }

    }
}
