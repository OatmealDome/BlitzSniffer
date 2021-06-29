using BlitzSniffer.TextInterface.Overview.Player;
using Terminal.Gui;

namespace BlitzSniffer.TextInterface.Overview
{
    class GameOverviewView : View
    {
        public GameOverviewView()
        {
            this.Add(new PlayerFrame()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            });
        }

    }
}
