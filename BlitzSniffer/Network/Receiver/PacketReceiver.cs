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
            Device.Close();
        }

        protected virtual void OnPacketArrival(object sender, CaptureEventArgs e)
        {
            NetworkManager.Instance.HandlePacketReceivedFromHandler(e);
        }

        public ICaptureDevice GetDevice()
        {
            return Device;
        }

    }
}
