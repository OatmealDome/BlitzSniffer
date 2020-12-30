﻿using BlitzSniffer.Config;
using Serilog;
using Serilog.Core;
using System;
using System.Net.Sockets;
using System.Threading;

namespace BlitzSniffer.Searcher
{
    class SnicomSessionSearcher : SessionSearcher
    {
        private static readonly ILogger LogContext = Log.ForContext(Constants.SourceContextPropertyName, "SnicomSessionSearcher");

        private Thread ConnectionThread;
        private CancellationTokenSource StopToken;

        public SnicomSessionSearcher() : base()
        {
            StopToken = new CancellationTokenSource();
            ConnectionThread = new Thread(ConnectToSnicom);

            ConnectionThread.Start();
        }

        public static void Initialize()
        {
            Instance = new SnicomSessionSearcher();
        }

        public override void Dispose()
        {
            if (!StopToken.IsCancellationRequested)
            {
                StopToken.Cancel();
            }
        }

        // This is terrible.
        private void ConnectToSnicom()
        {
            while (!StopToken.IsCancellationRequested)
            {
                try
                {
                    using TcpClient client = new TcpClient(SnifferConfig.Instance.Snicom.IpAddress, 13380);
                    using NetworkStream stream = client.GetStream();

                    LogContext.Information("Connection established with sys-snicom");

                    while (!StopToken.IsCancellationRequested)
                    {
                        if (!stream.DataAvailable)
                        {
                            continue;
                        }

                        int magic = stream.ReadByte();
                        if (magic == -1)
                        {
                            continue;
                        }

                        switch (magic)
                        {
                            case 'p': // ping
                                continue;
                            case 'k': // key
                                byte[] key = new byte[16];
                                stream.Read(key, 0, 16);

                                NotifySessionFound(key);

                                break;
                            default:
                                LogContext.Warning("Unknown packet received with magic '{Magic}'", magic.ToString("x"));

                                break;
                        }
                    }
                }
                catch (Exception e)
                {
                    LogContext.Error("Exception: {Exception}", e);
                }
            }
        }

    }
}
