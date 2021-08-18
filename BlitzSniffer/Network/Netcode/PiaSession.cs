using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using BlitzSniffer.Network.Manager;
using BlitzSniffer.Network.Netcode.Clone;
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
using Serilog;
using Serilog.Core;
using Syroot.BinaryData;

namespace BlitzSniffer.Network.Netcode
{
    class PiaSession : IDisposable
    {
        private static readonly ILogger LogContext = Log.ForContext(Constants.SourceContextPropertyName, "PiaSession");

        private PiaSessionType SessionType;
        private byte[] SessionKey;
        private uint GatheringId;

        public PiaSession(PiaSessionType sessionType)
        {
            SessionType = sessionType;
            SessionKey = null;
            GatheringId = 0;

            NetworkManager.Instance.PacketReceived += HandlePacketReceived;
            NetworkManager.Instance.SessionDataFound += HandleSessionData;
        }

        public void Dispose()
        {
            NetworkManager.Instance.PacketReceived -= HandlePacketReceived;
            NetworkManager.Instance.SessionDataFound -= HandleSessionData;
        }

        private void HandlePacketReceived(object sender, PacketReceivedEventArgs e)
        {
            using (MemoryStream memoryStream = new MemoryStream(e.UdpPacket.PayloadData))
            using (BinaryDataReader reader = new BinaryDataReader(memoryStream))
            {
                reader.ByteOrder = ByteOrder.BigEndian;

                try
                {
                    if (e.UdpPacket.DestinationPort != 30000)
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
                            LogContext.Warning("Skipping packet with length {Length}, no session key", e.UdpPacket.PayloadData.Length);
                            return;
                        }

                        if (SessionType == PiaSessionType.Inet && GatheringId == 0)
                        {
                            LogContext.Warning("Skipping packet with length {Length}, no gathering ID", e.UdpPacket.PayloadData.Length);
                            return;
                        }

                        HandlePiaPacket(reader, e.IPPacket.SourceAddress.GetAddressBytes());
                    }
                }
                catch (Exception ex) when (!Debugger.IsAttached)
                {
                    LogContext.Error(ex, "Exception while processing packet");
                }
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

            if (cloneContentData != null)
            {
                CloneHolder.Instance.UpdateWithContentData(cloneContentData, cloneMessage.SourceStationId);
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

        private void HandleSessionData(object sender, SessionDataFoundEventArgs e)
        {
            if (e.FoundDataType == SessionFoundDataType.Key)
            {
                SessionKey = e.Data;
            }
            else if (e.FoundDataType == SessionFoundDataType.GatheringId)
            {
                GatheringId = BitConverter.ToUInt32(e.Data);
            }
        }

    }
}