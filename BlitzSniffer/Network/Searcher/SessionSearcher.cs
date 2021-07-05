using Serilog;
using Serilog.Core;
using System;

namespace BlitzSniffer.Network.Searcher
{
    public abstract class SessionSearcher : IDisposable
    {
        private static readonly ILogger LogContext = Log.ForContext(Constants.SourceContextPropertyName, "SessionSearcher");

        public delegate void SessionDataFoundHandler(object sender, SessionDataFoundEventArgs args);
        public event SessionDataFoundHandler SessionDataFound;

        protected SessionSearcher()
        {

        }

        public abstract void Dispose();

        protected void NotifySessionDataFound(SessionFoundDataType type, byte[] data)
        {
            SessionDataFound?.Invoke(this, new SessionDataFoundEventArgs(type, data));

            if (type == SessionFoundDataType.Key)
            {
                LogContext.Information("Key found: {Key}", BitConverter.ToString(data).Replace("-", "").ToLower());
            }
            else if (type == SessionFoundDataType.GatheringId)
            {
                LogContext.Information("Gathering ID found: {GatheringId}", BitConverter.ToString(data).Replace("-", "").ToLower());
            }
        }

    }
}
