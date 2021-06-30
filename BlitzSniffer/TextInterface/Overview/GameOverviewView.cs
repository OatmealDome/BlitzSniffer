using BlitzSniffer.TextInterface.Overview.GlobalState;
using BlitzSniffer.TextInterface.Overview.Player;
using Terminal.Gui;

namespace BlitzSniffer.TextInterface.Overview
{
    class GameOverviewView : View
    {
        public GameOverviewView()
        {
            PlayerFrame playerFrame = new PlayerFrame()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Percent(50.0f)
            };

            this.Add(playerFrame);

            GlobalStateFrame stateFrame = new GlobalStateFrame()
            {
                X = 0,
                Y = Pos.Bottom(playerFrame),
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            this.Add(stateFrame);
        }

    }
}
