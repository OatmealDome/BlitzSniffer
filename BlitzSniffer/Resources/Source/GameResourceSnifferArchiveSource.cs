using BlitzCommon.Resources;
using BlitzCommon.Resources.Source;
using BlitzSniffer.Util;
using Nintendo.Archive;
using Syroot.NintenTools.Yaz0;
using System;
using System.IO;

namespace BlitzSniffer.Resources.Source
{
    class GameResourceSnifferArchiveSource : GameResourceSource
    {
#if !DEBUG
        private readonly static string ArchivePath = "GameData.bsrsrc";
#else
        private readonly static string ArchivePath = "GameData.szs";
#endif

        private Sarc SnifferArchive;

        private GameResourceSnifferArchiveSource()
        {
            byte[] data;
#if !DEBUG
            string keyBase64 = LicenseTools.Instance.GetDataObjectString("gameDataKey");
            byte[] key = Convert.FromBase64String(keyBase64);

            SnifferResource resource = SnifferResource.Deserialize(File.ReadAllBytes(ArchivePath), key);

            data = resource.Data;
#else
            data = File.ReadAllBytes(ArchivePath);
#endif

            using (MemoryStream resourceStream = new MemoryStream(data))
            {
                MemoryStream memoryStream = new MemoryStream();
                Yaz0Compression.Decompress(resourceStream, memoryStream);

                memoryStream.Seek(0, SeekOrigin.Begin);

                SnifferArchive = new Sarc(memoryStream);
            }
        }

        public static void Initialize()
        {
            Instance = new GameResourceSnifferArchiveSource();
        }

        protected override Stream GetFileInternal(string path)
        {
            byte[] rawFile = SnifferArchive[path.StartsWith('/') ? path.Substring(1) : path];
            return new MemoryStream(rawFile);
        }

    }
}
