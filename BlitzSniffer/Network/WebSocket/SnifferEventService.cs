using BlitzSniffer.Event;
using Serilog;
using Serilog.Core;
using System.Text.Json;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace BlitzSniffer.Network.WebSocket
{
    class SnifferEventService : WebSocketBehavior
    {
        private static readonly ILogger LogContext = Serilog.Log.ForContext(Constants.SourceContextPropertyName, "SnifferEventService");

        public SnifferEventService()
        {

        }

        protected override void OnOpen()
        {
            base.OnOpen();

            EventTracker.Instance.SendEvent += BroadcastEvent;

            LogContext.Information("Client connected");
        }

        protected override void OnClose(CloseEventArgs e)
        {
            base.OnClose(e);

            EventTracker.Instance.SendEvent -= BroadcastEvent;

            LogContext.Information("Client disconnected");
        }

        private void BroadcastEvent(object sender, SendEventArgs args)
        {
            string json = JsonSerializer.Serialize(args.GameEvent, args.GameEvent.GetType(), new JsonSerializerOptions()
            {
                WriteIndented = true
            });

            Sessions.BroadcastAsync(json, null);
        }

    }
}
