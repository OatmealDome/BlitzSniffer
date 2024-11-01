﻿using BlitzCommon.Resources.Source;
using BlitzSniffer.Config;
using BlitzSniffer.Event;
using BlitzSniffer.Receiver;
using BlitzSniffer.Resources.Source;
using BlitzSniffer.Searcher;
using BlitzSniffer.Tracker;
using BlitzSniffer.WebSocket;
using LibHac;
using NintendoNetcode.Pia;
using Serilog;
using Serilog.Core;
using SharpPcap;
using SKM.V3;
using SKM.V3.Methods;
using SKM.V3.Models;
using System;
using System.IO;
using System.Text;

namespace BlitzSniffer
{
    class Program
    {
        private static readonly string LOG_FORMAT = "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";

        private static string CRYPTOLENS_PUBLIC_KEY = "<RSAKeyValue><Modulus>kXe0NP7Dco5g85KOziWQT+oK21VkKwp+4XeR6GOTf46u2F3UwdFK3UYA1wXxIobbWoCpvX+7Yq/gGlV03IEqjzfxePwMXKd31EIFT7fez/hKz29YRD6A9pIJwqnHfJo8Xfje/6vxj83nvlvLXLgLutJs4tKK+hM43EAKy2NEs3mF/qeu88tPX3MMkrqrN0N2/I2tPnUgiMjV/pZ02wWhZSFnsfxhpcmwUI0mTYPcYa8317oG2BoXtNiS7wpurHygZPPRpcqc/BJjR7117N3IY7GIBa7qsBhcyzjr86m+Wt2s65kt3A5vI9jAjQ7cTIPIhzvWJCoeVOwTdjJSpjZsxw==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
        private static string CRYPTOLENS_AUTH_TOKEN = "WyIyNTg2NDgiLCJueitsRVVmQWFpVWIwVHM5RzdtTjJkNjkxekxHR0czb2ROU2phNEVyIl0=";

        /// <summary>
        /// Sniffs Splatoon 2 LAN sessions.
        /// </summary>
        /// <param name="onlineSession">Sniff online sessions.</param>
        /// <param name="useRom">If a Splatoon 2 ROM should be used instead of the GameData file.</param>
        /// <param name="replayFile">A pcap file to replay.</param>
        /// <param name="replayInRealTime">If the replay file should be replayed in real-time.</param>
        /// <param name="realTimeStartOffset">When to fast-forward to in the replay file.</param>
        /// <param name="autoStartReplay">Whether to skip prompting the user to start the replay or not.</param>
        static void Main(bool onlineSession = false, bool useRom = false, FileInfo replayFile = null, bool replayInRealTime = false, int realTimeStartOffset = 0, bool autoStartReplay = false)
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

            localLogContext.Information("BlitzSniffer {Version} ({BuildType}) for EndGameTV / Catalyst Workshop", ThisAssembly.AssemblyFileVersion, buildType);
            localLogContext.Information("Copyright © 2020 - 2021 OatmealDome");

#if !DEBUG
            KeyInfoResult keyResult = Key.Activate(token: CRYPTOLENS_AUTH_TOKEN, parameters: new ActivateModel()
            {
                Key = SnifferConfig.Instance.Key,
                ProductId = 8988,
                Sign = true,
                MachineCode = Helpers.GetMachineCodePI()
            });

            if (keyResult == null || keyResult.Result == ResultType.Error || !keyResult.LicenseKey.HasValidSignature(CRYPTOLENS_PUBLIC_KEY).IsValid())
            {
                localLogContext.Error("Program validation failed. Please contact OatmealDome.");
                return;
            }
#endif

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

            PacketReceiver packetReceiver;
            if (replayFile != null)
            {
                if (replayInRealTime)
                {
                    packetReceiver = new RealTimeReplayPacketReceiver(sessionType, replayFile.FullName, realTimeStartOffset);
                }
                else
                {
                    packetReceiver = new ReplayPacketReceiver(sessionType, replayFile.FullName);
                }

                if (!autoStartReplay)
                {
                    localLogContext.Information("Waiting for user to start replay");
                    Console.ReadLine();
                }
            }
            else
            {
                packetReceiver = new LivePacketReceiver(sessionType, captureDevice);
            }

            if (sessionType == PiaSessionType.Inet)
            {
                if (replayFile == null)
                {
                    SnicomSessionSearcher.Initialize();
                }
                else
                {
                    OnlineReplaySessionSearcher.Initialize(packetReceiver.GetDevice());
                }
            }
            else
            {
                LanSessionSearcher.Initialize(packetReceiver.GetDevice());
            }

            Directory.CreateDirectory("PacketCaptures");
            string pcapDumpFile = Path.Combine("PacketCaptures", $"{dateTime}.pcap");

            packetReceiver.Start(replayFile == null ? pcapDumpFile : null);

            localLogContext.Information("This session's log files are filed under \"{DateTime}\".", dateTime);
            localLogContext.Information("Start up complete. Press any key to exit.");

            Console.ReadLine();

            SessionSearcher.Instance.Dispose();

            EventTracker.Instance.Shutdown();

            Log.CloseAndFlush();

            try
            {
                packetReceiver.Dispose();
            }
            catch (PlatformNotSupportedException)
            {
                // Forcefully exit - ICaptureDevice.Close() might throw an exception on Windows
                // "Thread abort not supported on this platform"
                Environment.Exit(0);
            }
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
