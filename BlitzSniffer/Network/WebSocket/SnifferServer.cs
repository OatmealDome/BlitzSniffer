using Serilog;
using Serilog.Core;
using System;
using System.Reflection;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace BlitzSniffer.Network.WebSocket
{
    public class SnifferServer
    {
        private static readonly ILogger WebSocketSharpLogContext = Log.ForContext(Constants.SourceContextPropertyName, "WebSocketSharp");

        public static SnifferServer Instance = null;

        private WebSocketServer Server
        {
            get;
            set;
        }

        private SnifferServer()
        {
            Server = new WebSocketServer(13370);

            // Get the WebSocketServer's logger
            FieldInfo loggerField = Server.GetType().GetField("_log", BindingFlags.NonPublic | BindingFlags.Instance);
            object webSocketLogger = loggerField.GetValue(Server);

            // Override the output mechanism to our own which prints to Serilog
            Type webSocketLoggerType = webSocketLogger.GetType();
            FieldInfo actionField = webSocketLoggerType.GetField("_output", BindingFlags.NonPublic | BindingFlags.Instance);
            Action<LogData, string> outputAction = (LogData logData, string path) => LogToSerilog(logData, path);
            actionField.SetValue(webSocketLogger, outputAction);

            Server.AddWebSocketService<SnifferEventService>("/Events");
            Server.Start();
        }

        public static void Initialize()
        {
            if (Instance != null)
            {
                return;
            }

            Instance = new SnifferServer();
        }

        private void LogToSerilog(LogData logData, string path)
        {
            switch (logData.Level)
            {
                case LogLevel.Trace:
                    WebSocketSharpLogContext.Verbose(logData.Message);
                    break;
                case LogLevel.Debug:
                    WebSocketSharpLogContext.Debug(logData.Message);
                    break;
                case LogLevel.Info:
                    WebSocketSharpLogContext.Information(logData.Message);
                    break;
                case LogLevel.Warn:
                    WebSocketSharpLogContext.Warning(logData.Message);
                    break;
                case LogLevel.Error:
                    WebSocketSharpLogContext.Error(logData.Message);
                    break;
                case LogLevel.Fatal:
                    WebSocketSharpLogContext.Fatal(logData.Message);
                    break;
            }
        }

    }
}
