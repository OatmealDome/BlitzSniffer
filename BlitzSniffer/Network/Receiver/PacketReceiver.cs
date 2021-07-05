using BlitzSniffer.Network.Netcode.Clone;
using BlitzSniffer.Config;
using BlitzSniffer.Network.Netcode.Enl;
using BlitzSniffer.Network.Searcher;
using NintendoNetcode.Enl;
using NintendoNetcode.Pia;
using NintendoNetcode.Pia.Clone;
using NintendoNetcode.Pia.Clone.Content;
using NintendoNetcode.Pia.Clone.Element.Data;
using NintendoNetcode.Pia.Clone.Element.Data.Event;
using NintendoNetcode.Pia.Clone.Element.Data.Reliable;
using NintendoNetcode.Pia.Clone.Element.Data.Unreliable;
using NintendoNetcode.Pia.Encryption;
using NintendoNetcode.Pia.Unreliable;
using PacketDotNet;
using Serilog;
using Serilog.Core;
using SharpPcap;
using SharpPcap.LibPcap;
using Syroot.BinaryData;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Threading;

namespace BlitzSniffer.Network.Receiver
{
    public abstract class PacketReceiver : IDisposable
    {
        private static readonly ILogger LogContext = Log.ForContext(Constants.SourceContextPropertyName, "PacketReceiver");

        protected PiaSessionType SessionType
        {
            get;
            set;
        }

        protected ICaptureDevice Device
        {
            get;
            set;
        }

        private byte[] SessionKey
        {
            get;
            set;
        } = null;

        private uint GatheringId
        {
            get;
            set;
        }

        private CaptureFileWriterDevice WriterDevice
        {
            get;
            set;
        }

        private BlockingCollection<RawCapture> CaptureQueue
        {
            get;
            set;
        }

        private Thread DumperThread
        {
            get;
            set;
        }

        private CancellationTokenSource DumperCancellationTokenSource
        {
            get;
            set;
        }

        protected PacketReceiver(PiaSessionType sessionType)
        {
            SessionType = sessionType;
        }

        public virtual void Start(SessionSearcher searcher, string outputFile = null)
        {
            if (outputFile != null)
            {
                WriterDevice = new CaptureFileWriterDevice(outputFile);
                WriterDevice.Open();

                CaptureQueue = new BlockingCollection<RawCapture>(new ConcurrentQueue<RawCapture>());
                DumperCancellationTokenSource = new CancellationTokenSource();
                DumperThread = new Thread(DumpPackets);
                DumperThread.Start();
            }

            searcher.SessionFound += SessionFound;

            Device.OnPacketArrival += OnPacketArrival;
            Device.StartCapture();
        }

        public virtual void Dispose()
        {
            if (WriterDevice != null)
            {
                DumperCancellationTokenSource.Cancel();
                DumperThread.Join();

                WriterDevice.Close();
            }

            Device.Close();
        }

        private void SessionFound(object sender, SessionDataFoundEventArgs e)
        {
            if (e.FoundDataType == SessionFoundDataType.Key)
            {
                SessionKey = e.Data;
            }
            else if (e.FoundDataType == SessionFoundDataType.GatheringId)
            {
                GatheringId = BitConverter.ToUInt32(e.Data);
            }

            // If we don't write this out now, then we won't be able to decrypt the packets
            // when replaying the capture.
            if (SessionType == PiaSessionType.Inet && WriterDevice != null)
            {
                byte[] packetPayload = new byte[4 + 1 + e.Data.Length];

                packetPayload[0] = (byte)'S';
                packetPayload[1] = (byte)'J';
                packetPayload[2] = (byte)'4';
                packetPayload[3] = (byte)'E';

                packetPayload[4] = (byte)e.FoundDataType;

                Array.Copy(e.Data, 0, packetPayload, 5, e.Data.Length);

                UdpPacket udpPacket = new UdpPacket(13390, 13390);
                udpPacket.PayloadData = packetPayload;

                IPAddress sourceAddress = IPAddress.Parse(SnifferConfig.Instance.Snicom.IpAddress);
                IPAddress destAddress = IPAddress.Parse("255.255.255.255");
                IPv4Packet ipPacket = new IPv4Packet(sourceAddress, destAddress);

                PhysicalAddress sourcePhysAddress = PhysicalAddress.Parse("0E-00-53-4A-34-45");
                PhysicalAddress destPhysAddress = PhysicalAddress.Parse("FF-FF-FF-FF-FF-FF");
                EthernetPacket ethernetPacket = new EthernetPacket(sourcePhysAddress, destPhysAddress, EthernetType.None);

                ipPacket.PayloadPacket = udpPacket;
                ethernetPacket.PayloadPacket = ipPacket;

                RawCapture rawCapture = new RawCapture(LinkLayers.Ethernet, new PosixTimeval(), ethernetPacket.Bytes);
                CaptureQueue.Add(rawCapture);
            }
        }

        protected virtual void OnPacketArrival(object sender, CaptureEventArgs e)
        {
            Packet packet = Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);

            UdpPacket udpPacket = packet.Extract<UdpPacket>();
            IPPacket ipPacket = packet.Extract<IPPacket>();

            using (MemoryStream memoryStream = new MemoryStream(udpPacket.PayloadData))
            using (BinaryDataReader reader = new BinaryDataReader(memoryStream))
            {
                reader.ByteOrder = ByteOrder.BigEndian;

                try
                {
                    if (udpPacket.DestinationPort != 30000)
                    {
                        using (reader.TemporarySeek())
                        {
                            if (reader.ReadUInt32() != PiaPacket.PACKET_MAGIC)
                            {
                                return;
                            }
                        }

                        if (SessionKey == null)
                        {
                            LogContext.Warning("Skipping packet with length {Length}, no session key", udpPacket.PayloadData.Length);
                            return;
                        }

                        if (SessionType == PiaSessionType.Inet && GatheringId == 0)
                        {
                            LogContext.Warning("Skipping packet with length {Length}, no gathering ID", udpPacket.PayloadData.Length);
                            return;
                        }

                        HandlePiaPacket(reader, ipPacket.SourceAddress.GetAddressBytes());
                    }
                }
                catch (Exception ex) when (!Debugger.IsAttached)
                {
                    LogContext.Error(ex, "Exception while processing packet");
                }
            }

            if (WriterDevice != null)
            {
                CaptureQueue.Add(e.Packet);
            }
        }

        private void HandlePiaPacket(BinaryDataReader reader, byte[] sourceAddress)
        {
            PiaEncryptionArgs encryptionArgs;
            if (SessionType == PiaSessionType.Lan)
            {
                encryptionArgs = new PiaLanEncryptionArgs(SessionKey, sourceAddress);
            }
            else if (SessionType == PiaSessionType.Inet)
            {
                encryptionArgs = new PiaInetEncryptionArgs(SessionKey, GatheringId);
            }
            else
            {
                LogContext.Error("LDN sessions not supported");
                return;
            }

            PiaPacket piaPacket;
            try
            {
                piaPacket = new PiaPacket(reader, encryptionArgs);
            }
            catch (CryptographicException)
            {
                // Just skip... We probably don't have the correct key yet or someone is running
                // another game/session on this network.
                return;
            }

            foreach (PiaMessage message in piaPacket.Messages)
            {
                if (message.ProtocolId == PiaProtocol.Clone)
                {
                    HandleCloneData(message as CloneMessage);
                }
                else if (message.ProtocolId == PiaProtocol.Unreliable && message.ProtocolPort == 0x01) // Enl
                {
                    HandleEnlPacket(message as UnreliableMessage);
                }
            }
        }

        private void HandleCloneData(CloneMessage cloneMessage)
        {
            CloneContentData cloneContentData = cloneMessage.Content as CloneContentData;

            if (cloneContentData == null || !CloneHolder.Instance.IsCloneRegistered(cloneContentData.CloneId))
            {
                return;
            }

            foreach (CloneElementData cloneElementData in cloneContentData.ElementData)
            {
                byte[] data;
                uint clock;

                switch (cloneElementData)
                {
                    case CloneElementDataEventData eventData:
                        data = eventData.Data;
                        clock = eventData.Clock;

                        break;
                    case CloneElementDataReliableData reliableData:
                        data = reliableData.Data;
                        clock = reliableData.Clock;
                        
                        break;
                    case CloneElementDataUnreliable unreliableData:
                        data = unreliableData.Data;
                        clock = unreliableData.Clock;
                        
                        break;
                    default:
                        continue;
                }

                CloneHolder.Instance.UpdateCloneClock(clock);
                CloneHolder.Instance.UpdateElementInClone(cloneContentData.CloneId, cloneElementData.Id, data, cloneMessage.SourceStationId);
            }
        }

        private void HandleEnlPacket(UnreliableMessage unreliableMessage)
        {
            using (MemoryStream innerStream = new MemoryStream(unreliableMessage.Data))
            using (BinaryDataReader innerReader = new BinaryDataReader(innerStream))
            {
                innerReader.ByteOrder = ByteOrder.LittleEndian;

                EnlMessage enlMessage = new EnlMessage(innerReader, 10, 0);
                EnlHolder.Instance.EnlMessageReceived(enlMessage);
            }
        }

        private void DumpPackets()
        {
            while (!DumperCancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    if (CaptureQueue.TryTake(out RawCapture capture, -1, DumperCancellationTokenSource.Token))
                    {
                        WriterDevice.Write(capture);
                    }
                }
                catch (OperationCanceledException)
                {
                    ;
                }
            }
        }

        public ICaptureDevice GetDevice()
        {
            return Device;
        }

    }
}
