using BlitzSniffer.Network.Manager;
using BlitzSniffer.Util;
using NintendoNetcode.Pia;
using NintendoNetcode.Pia.Lan.Content.Browse;
using PacketDotNet;
using Serilog;
using SharpPcap;
using SharpPcap.LibPcap;
using Syroot.BinaryData;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using static SharpPcap.LibPcap.Sockaddr;

namespace BlitzSniffer.Network.Searcher
{
    class LanSessionSearcher : SessionSearcher
    {
        private static readonly ILogger LogContext = LogUtil.GetLogger("LanSessionSearcher");

        private static byte[] SEARCH_CRITERIA = new byte[] { 0x00, 0x01, 0x00, 0x01, 0x00, 0x0A, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0A, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0F, 0xFF };
        
        private ICaptureDevice Device;
        private CancellationTokenSource BroadcastToken;
        private bool DidPrintVersionWarning;
        private long QueryCount;

        public LanSessionSearcher(ICaptureDevice device) : base()
        {
            Device = device;
            BroadcastToken = new CancellationTokenSource();
            QueryCount = 0;

            NetworkManager.Instance.PacketReceived += OnPacketArrival;

            if (device is LibPcapLiveDevice)
            {
                new Thread(BroadcastBrowseRequest).Start();
            }
        }

        public override void Dispose()
        {
            if (!BroadcastToken.IsCancellationRequested)
            {
                BroadcastToken.Cancel();
            }
        }

        protected virtual void OnPacketArrival(object sender, PacketReceivedEventArgs e)
        {
            UdpPacket udpPacket = e.UdpPacket;
            if (udpPacket.DestinationPort != 30000)
            {
                return;
            }

            using (MemoryStream memoryStream = new MemoryStream(udpPacket.PayloadData))
            using (BinaryDataReader reader = new BinaryDataReader(memoryStream))
            {
                byte firstByte = reader.ReadByte();

                if (firstByte == 0x1)
                {
                    LanContentBrowseReply browseReply = new LanContentBrowseReply(reader);

                    if (!Program.BLITZ_SUPPORTED_VERSIONS.Contains(browseReply.SessionInfo.AppCommunicationVersion.ToString()) && !DidPrintVersionWarning)
                    {
                        LogContext.Warning("AppVersion mismatch - session is running {Version}, expected any of {SupportedVersions}", browseReply.SessionInfo.AppCommunicationVersion, string.Join(',', Program.BLITZ_SUPPORTED_VERSIONS));

                        DidPrintVersionWarning = true;
                    }

                    byte[] key = PiaEncryptionUtil.GenerateLanSessionKey(browseReply.SessionInfo.SessionParam, PiaEncryptionUtil.BlitzGameKey);

                    NotifySessionDataFound(SessionFoundDataType.Key, key);

                    BroadcastToken.Cancel();
                }
            }
        }

        private void BroadcastBrowseRequest()
        {
            LibPcapLiveDevice liveDevice = Device as LibPcapLiveDevice;
            PcapAddress pcapInetAddress = liveDevice.Addresses.Where(a => a.Addr.type == AddressTypes.AF_INET_AF_INET6 && a.Addr.ipAddress.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault();

            byte[] ipv4Address;
            byte[] mask;

            if (pcapInetAddress != null)
            {
                // GetAddressBytes returns an array in network order
                ipv4Address = pcapInetAddress.Addr.ipAddress.GetAddressBytes();
                mask = pcapInetAddress.Netmask.ipAddress.GetAddressBytes();
            }
            else
            {
                IPAddress address = liveDevice.Interface.GatewayAddresses.FirstOrDefault();
                if (address == null)
                {
                    throw new SnifferException("Failed to find an address to calculate the broadcast address from");
                }

                ipv4Address = address.GetAddressBytes();
                mask = new byte[] { 255, 255, 255, 0 }; // usually the case, but not always
            }

            for (int i = 0; i < ipv4Address.Length; i++)
            {
                ipv4Address[i] |= (byte)~mask[i];
            }

            IPAddress broadcastAddress = new IPAddress(ipv4Address);
            IPEndPoint endPoint = new IPEndPoint(broadcastAddress, 30000);

            while (!BroadcastToken.IsCancellationRequested)
            {
                try
                {
                    using (UdpClient client = new UdpClient(30000))
                    {
                        client.EnableBroadcast = true;

                        while (!BroadcastToken.IsCancellationRequested)
                        {
                            // Used for nonce
                            QueryCount++;

                            byte[] rawRequest;

                            using (MemoryStream memoryStream = new MemoryStream())
                            using (BinaryDataWriter writer = new BinaryDataWriter(memoryStream))
                            {
                                writer.ByteOrder = ByteOrder.BigEndian; // network order

                                // Browse Request magic number
                                writer.Write((byte)0);

                                LanContentBrowseRequest browseRequest = new LanContentBrowseRequest()
                                {
                                    SessionSearchCriteria = SEARCH_CRITERIA,
                                    CryptoChallenge = PiaEncryptionUtil.GenerateLanCryptoChallenge(ipv4Address, QueryCount, PiaEncryptionUtil.BlitzGameKey)
                                };

                                browseRequest.Serialize(writer);

                                rawRequest = memoryStream.ToArray();
                            }

                            client.Send(rawRequest, rawRequest.Length, endPoint);

                            Thread.Sleep(1000);
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
