using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using BlitzSniffer.Config;
using BlitzSniffer.Network.Searcher;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;

namespace BlitzSniffer.Network.Manager
{
    class LiveWriteOut : IDisposable
    {
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

        public LiveWriteOut(string outputFile)
        {
            WriterDevice = new CaptureFileWriterDevice(outputFile);
            WriterDevice.Open();

            CaptureQueue = new BlockingCollection<RawCapture>(new ConcurrentQueue<RawCapture>());
            DumperCancellationTokenSource = new CancellationTokenSource();
            DumperThread = new Thread(DumpPackets);
            DumperThread.Start();

            NetworkManager.Instance.PacketReceived += HandlePacketReceived;
            NetworkManager.Instance.SessionDataFound += HandleSessionDataFound;
        }

        public void Dispose()
        {
            NetworkManager.Instance.PacketReceived -= HandlePacketReceived;
            NetworkManager.Instance.SessionDataFound -= HandleSessionDataFound;

            if (WriterDevice != null)
            {
                DumperCancellationTokenSource.Cancel();
                DumperThread.Join();

                WriterDevice.Close();
            }
        }

        private void HandlePacketReceived(object sender, PacketReceivedEventArgs e)
        {
            CaptureQueue.Add(e.RawCapture);
        }

        private void HandleSessionDataFound(object sender, SessionDataFoundEventArgs e)
        {
            // If we don't write this out now, then we won't be able to decrypt the packets
            // when replaying the capture.
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
        
    }
}