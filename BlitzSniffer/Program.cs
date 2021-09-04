using BlitzCommon.Resources.Source;
using BlitzSniffer.Config;
using BlitzSniffer.Game.Event;
using BlitzSniffer.Network.Receiver;
using BlitzSniffer.Game.Resources.Source;
using BlitzSniffer.Network.Searcher;
using BlitzSniffer.TextInterface;
using BlitzSniffer.Game.Tracker;
using BlitzSniffer.Util;
using BlitzSniffer.Network.WebSocket;
using LibHac;
using NintendoNetcode.Pia;
using Serilog;
using Serilog.Core;
using SharpPcap;
using System;
using System.IO;
using System.Text;
using System.Xml;
using BlitzSniffer.Network.Manager;
using System.Linq;
using LibVLCSharp.Shared;

namespace BlitzSniffer
{
    class Program
    {
        private static readonly string LOG_FORMAT = "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";

        public static readonly string[] BLITZ_SUPPORTED_VERSIONS = new string[] { "64", "65" }; // string because XML

        /// <summary>
        /// Sniffs Splatoon 2 LAN sessions.
        /// </summary>
        /// <param name="consoleOnly">Disables the fancy user interface.</param>
        /// <param name="onlineSession">Sniff online sessions.</param>
        /// <param name="useRom">If a Splatoon 2 ROM should be used instead of the GameData file. This option has no effect in Release builds.</param>
        /// <param name="replayFile">A pcap file to replay.</param>
        /// <param name="replayInRealTime">If the replay file should be replayed in real-time.</param>
        /// <param name="videoReplay">If the specified replay file is a configuration file for a video-synchronized replay.</param>
        /// <param name="realTimeStartOffset">When to fast-forward to in the replay file.</param>
        /// <param name="autoStartReplay">Whether to skip prompting the user to start the replay or not.</param>
        static void Main(bool consoleOnly = false, bool onlineSession = false, bool useRom = false, FileInfo replayFile = null, bool replayInRealTime = false, bool videoReplay = false, int realTimeStartOffset = 0, bool autoStartReplay = false)
        {
            Console.OutputEncoding = Encoding.UTF8;

            SnifferConfig.Load();

            ICaptureDevice captureDevice = GetCaptureDevice();

            if (captureDevice == null && replayFile == null)
            {
                if (CaptureDeviceList.Instance.Count == 0)
                {
                    Console.WriteLine("Error: No capture devices found. Ensure that Npcap (or equivalent) is installed correctly.");
                    Console.WriteLine("Press any key to exit.");
                    Console.ReadKey();

                    return;
                }

                if (!string.IsNullOrEmpty(SnifferConfig.Instance.DefaultDevice) && SnifferConfig.Instance.DefaultDevice != "none")
                {
                    Console.WriteLine("Error: Could not find the configured capture device. You will be asked to choose a new device.");
                    Console.WriteLine("If you do not wish to choose a new device, close BlitzSniffer. Ensure that Npcap (or equivalent)");
                    Console.WriteLine("is installed correctly and that the capture device is connected to the computer.");
                    Console.WriteLine($"\nConfigured device: \"{SnifferConfig.Instance.DefaultDevice}\"\n");
                    Console.WriteLine("Press any key to continue.");
                    Console.ReadKey();

                    Console.WriteLine();
                }

                while (true)
                {
                    for (int i = 0; i < CaptureDeviceList.Instance.Count; i++)
                    {
                        Console.WriteLine($"Device #{i + 1}\n{CaptureDeviceList.Instance[i]}");
                    }

                    Console.Write("Enter the device number to set as default: ");

                    int deviceNumber;
                    if (int.TryParse(Console.ReadLine(), out int result))
                    {
                        deviceNumber = result - 1;
                    }
                    else
                    {
                        Console.WriteLine("\nInvalid selection.\n");
                        continue;
                    }

                    if (deviceNumber < 0 || deviceNumber >= CaptureDeviceList.Instance.Count)
                    {
                        Console.WriteLine("\nInvalid selection.\n");
                        continue;
                    }

                    captureDevice = CaptureDeviceList.Instance[deviceNumber];
                    SnifferConfig.Instance.DefaultDevice = captureDevice.Name;

                    break;
                }
            }

            SnifferConfig.Instance.Save();

            Console.Clear();

            Directory.CreateDirectory("Logs");
            string dateTime = DateTime.Now.ToString("s").Replace(':', '_');
            string logFile = Path.Combine("Logs", $"{dateTime}.log");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Async(c => c.Console(outputTemplate: LOG_FORMAT))
                .WriteTo.Async(c => c.File(logFile, outputTemplate: LOG_FORMAT, encoding: Encoding.UTF8))
                .CreateLogger();

            ILogger localLogContext = Log.ForContext(Constants.SourceContextPropertyName, "Program");

#if !DEBUG
            if (!LicenseTools.Instance.LoadAndVerifyLicense())
            {
                localLogContext.Error("Program validation failed. Please contact OatmealDome.");

                Log.CloseAndFlush();

                return;
            }
#endif

            // The "Unreachable code" warning is suppressed here as the compiler will see that IsPrerelease
            // is a constant boolean and tell us that one of the blocks will not be executed.
#pragma warning disable CS0162
            string buildType;
            if (ThisAssembly.IsPrerelease)
            {
                buildType = "beta";
            }
            else
            {
                buildType = "stable";
            }
#pragma warning restore CS0162

#if !DEBUG
            string licensee = LicenseTools.Instance.GetDataObjectString("licensedTo");
            if (licensee == null)
            {
                licensee = "Unknown Licensee";
            }
#else
            string licensee = "Local Debug Build";
#endif

            localLogContext.Information("BlitzSniffer {Version} ({BuildType}) for {Licensee}", ThisAssembly.AssemblyFileVersion, buildType, licensee);
            localLogContext.Information("Copyright © 2020 - 2021 OatmealDome");

            // LibVLC initialization
            try
            {
                Core.Initialize();
            }
            catch (Exception e)
            {
#if DEBUG
                localLogContext.Warning("Skipped VLC initialization.");
#endif
            }
#if DEBUG
            if (useRom)
            {
                RomConfig romConfig = SnifferConfig.Instance.Rom;
                Keyset keyset = ExternalKeys.ReadKeyFile(romConfig.ProdKeys, romConfig.TitleKeys);
                GameResourceRomSource.Initialize(keyset, romConfig.BaseNca, romConfig.UpdateNca);
            }
            else
            {
                GameResourceSnifferArchiveSource.Initialize();
            }
#else
            GameResourceSnifferArchiveSource.Initialize();
#endif

            // Verify we're working with the right version
            XmlDocument gameConfigDocument = new XmlDocument();
            using (Stream stream = GameResourceSource.Instance.GetFile("/System/GameConfigSetting.xml"))
            {
                gameConfigDocument.Load(stream);
            }

            XmlNode node = gameConfigDocument.SelectSingleNode("//parameter[@name='AppVersion']");
            if (node == null || !BLITZ_SUPPORTED_VERSIONS.Contains(node.Attributes.GetNamedItem("defaultValue").Value))
            {
                localLogContext.Error("Incompatible Splatoon 2 version found. Please upgrade to the latest BlitzSniffer.");

                Log.CloseAndFlush();

                return;
            }

            SnifferServer.Initialize();

            GameSession.Initialize();
            GameSession.Instance.Reset();

            LocalLog.RegisterConsoleDebug();

            PiaSessionType sessionType;
            if (onlineSession)
            {
                sessionType = PiaSessionType.Inet;
            }
            else
            {
                sessionType = PiaSessionType.Lan;
            }

            Directory.CreateDirectory("PacketCaptures");
            string pcapDumpFile = Path.Combine("PacketCaptures", $"{dateTime}.pcap");

            if (replayFile != null)
            {
                if (replayInRealTime)
                {
                    ulong microsecondsOffset = (ulong)realTimeStartOffset * PosixTimevalExtensions.ONE_SECOND_IN_MICROSECONDS;

                    if (videoReplay)
                    {
                        NetworkManager.Instance.LoadRealTimeVideoSynchronizedReplay(sessionType, replayFile.FullName, microsecondsOffset);
                    }
                    else
                    {
                        NetworkManager.Instance.LoadRealTimeReplay(sessionType, replayFile.FullName, microsecondsOffset);
                    }
                }
                else
                {
                    NetworkManager.Instance.LoadReplay(sessionType, replayFile.FullName);
                }
            }
            else
            {
                NetworkManager.Instance.LoadLive(sessionType, captureDevice, pcapDumpFile);
            }

            localLogContext.Information("This session's log files are filed under \"{DateTime}\".", dateTime);

            if (replayFile != null)
            {
                localLogContext.Information("Press return to start the replay.");

                Console.ReadKey();
            }

            NetworkManager.Instance.Start();

            if (!consoleOnly)
            {
                Terminal.Gui.Application.Run<SnifferTextApplication>();
            }
            else
            {
                localLogContext.Information("Start up complete. Press return to exit.");

                Console.ReadKey();
            }

            NetworkManager.Instance.Reset();

            EventTracker.Instance.Shutdown();

            Log.CloseAndFlush();

        }

        static ICaptureDevice GetCaptureDevice()
        {
            if (SnifferConfig.Instance.DefaultDevice == "none")
            {
                return null;
            }

            foreach (ICaptureDevice device in CaptureDeviceList.Instance)
            {
                if (SnifferConfig.Instance.DefaultDevice == device.Name)
                {
                    return device;
                }
            }

            return null;
        }

    }
}
