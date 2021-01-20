using System.IO;
using System.Text.Json;

namespace BlitzSniffer.Config
{
    public class SnifferConfig
    {
        public static SnifferConfig Instance
        {
            get;
            private set;
        }

        private static readonly string ConfigPath = "config.json";

#if DEBUG
        public RomConfig Rom
        {
            get;
            set;
        }
#endif

        public SnicomConfig Snicom
        {
            get;
            set;
        }

        public string DefaultDevice
        {
            get;
            set;
        }

        public string Key
        {
            get;
            set;
        }

        public SnifferConfig()
        {
#if DEBUG
            Rom = new RomConfig();
#endif
            Snicom = new SnicomConfig();
            DefaultDevice = "none";
            Key = "ABCDE-FGHIJ-KLMNO-PQRST";
        }

        public static void Load()
        {
            if (File.Exists(ConfigPath))
            {
                Instance = JsonSerializer.Deserialize<SnifferConfig>(File.ReadAllText(ConfigPath));
            }
            else
            {
                Instance = new SnifferConfig();
            }
        }

        public void Save()
        {
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize<SnifferConfig>(this, new JsonSerializerOptions()
            {
                WriteIndented = true
            }));
        }

    }
}
