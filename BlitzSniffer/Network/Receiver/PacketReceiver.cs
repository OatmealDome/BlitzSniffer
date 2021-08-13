using BlitzSniffer.Network.Manager;
using Serilog;
using Serilog.Core;
using SharpPcap;
using System;

namespace BlitzSniffer.Network.Receiver
{
    public abstract class PacketReceiver : IDisposable
    {
        private static readonly ILogger LogContext = Log.ForContext(Constants.SourceContextPropertyName, "PacketReceiver");

        protected ICaptureDevice Device
        {
            get;
            set;
        }

        public PosixTimeval LastReceivedTimeval
        {
            get;
            private set;
        }

        protected PacketReceiver()
        {

        }

        public virtual void Start()
        {
            Device.OnPacketArrival += OnPacketArrival;
            Device.StartCapture();
        }

        public virtual void Dispose()
        {
            Device.OnPacketArrival -= OnPacketArrival;

            try
            {
                Device.Close();
            }
            catch (PlatformNotSupportedException)
            {
                // ICaptureDevice.Close() might throw an exception on Windows:
                // "Thread abort not supported on this platform"
            }
        }

        protected virtual void OnPacketArrival(object sender, CaptureEventArgs e)
        {
            LastReceivedTimeval = e.Packet.Timeval;

            NetworkManager.Instance.HandlePacketReceivedFromHandler(e);
        }

        public ICaptureDevice GetDevice()
        {
            return Device;
        }

    }
}
