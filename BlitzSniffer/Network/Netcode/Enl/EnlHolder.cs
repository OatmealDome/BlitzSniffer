using NintendoNetcode.Enl;
using NintendoNetcode.Enl.Record;

namespace BlitzSniffer.Network.Netcode.Enl
{
    public class EnlHolder
    {
        private static EnlHolder _Instance = null;

        public static EnlHolder Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new EnlHolder();
                }

                return _Instance;
            }
        }

        public delegate void SystemInfoReceivedEventHandler(object sender, SystemInfoReceivedEventArgs args);
        public event SystemInfoReceivedEventHandler SystemInfoReceived;

        public EnlHolder()
        {

        }

        public void EnlMessageReceived(EnlMessage message)
        {
            foreach (EnlRecord record in message.Records)
            {
                if (record is EnlSystemInfoRecord)
                {
                    EnlSystemInfoRecord systemInfoRecord = record as EnlSystemInfoRecord;
                    SystemInfoReceived(this, new SystemInfoReceivedEventArgs(systemInfoRecord));
                }
            }
        }

    }
}
