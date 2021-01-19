using BlitzCommon.Util;
using Syroot.BinaryData;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace BlitzCommon.Resources
{
    public class SnifferResource
    {
        private static string MAGIC = "BZSNRSRC";
        private static byte VERSION = 0x1;

        public byte[] Key
        {
            get;
            private set;
        }

        public byte[] Data
        {
            get;
            private set;
        }

        public SnifferResource(byte[] data)
        {
            Key = null;
            Data = data;
        }

        public SnifferResource(byte[] data, byte[] key)
        {
            Trace.Assert(key != null && key.Length == 16);

            Key = key;
            Data = data;
        }

        public static SnifferResource Deserialize(byte[] rawData, byte[] key)
        {
            using MemoryStream memoryStream = new MemoryStream(rawData);
            using BinaryDataReader reader = new BinaryDataReader(memoryStream);

            reader.ByteOrder = ByteOrder.BigEndian;

            if (reader.ReadString(8) != MAGIC)
            {
                throw new Exception("Invalid magic");
            }

            if (reader.ReadUInt16() == 0xFFFE)
            {
                reader.ByteOrder = ByteOrder.LittleEndian;
            }

            byte version = reader.ReadByte();

            if (version != VERSION)
            {
                throw new Exception("Unsupported version");
            }

            reader.Seek(1); // padding

            byte[] ReadBytesAtOfs(uint ofs, uint size)
            {
                using (reader.TemporarySeek(ofs, SeekOrigin.Begin))
                {
                    return reader.ReadBytes((int)size);
                }
            }

            uint dataOfs = reader.ReadUInt32();
            uint dataSize = reader.ReadUInt32();

            uint nonceOfs = reader.ReadUInt32();
            uint tagOfs = reader.ReadUInt32();

            byte[] ciphertext = ReadBytesAtOfs(dataOfs, dataSize);
            byte[] nonce = ReadBytesAtOfs(nonceOfs, 12);
            byte[] tag = ReadBytesAtOfs(tagOfs, 16);

            byte[] plaintext = new byte[dataSize];

            AesGcm aesGcm = new AesGcm(key);
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);

            SnifferResource resource = new SnifferResource(plaintext, key);

            return resource;
        }

        public byte[] Serialize()
        {
            using MemoryStream memoryStream = new MemoryStream();
            using BinaryDataWriter writer = new BinaryDataWriter(memoryStream);
            using RNGCryptoServiceProvider rngCrypto = new RNGCryptoServiceProvider();

            byte[] nonce = new byte[12];

            if (Key == null)
            {
                Key = new byte[16];
                rngCrypto.GetBytes(Key);
            }

            rngCrypto.GetBytes(nonce);

            byte[] ciphertext = new byte[Data.Length];
            byte[] tag = new byte[16];

            AesGcm aesGcm = new AesGcm(Key);
            aesGcm.Encrypt(nonce, Data, ciphertext, tag);

            void SeekToNext16()
            {
                long length = MathUtil.RoundUpToMultiple((int)writer.Position, 0x10) - writer.Position;

                if (length == 0)
                {
                    return;
                }

                byte[] randomBytes = new byte[length];
                rngCrypto.GetBytes(randomBytes);

                writer.Write(randomBytes);
            }

            writer.ByteOrder = ByteOrder.BigEndian;

            writer.Write(MAGIC, BinaryStringFormat.NoPrefixOrTermination, Encoding.ASCII);

            writer.Write((ushort)0xFEFF); // BOM, big-endian

            writer.Write(VERSION); // Version

            writer.Seek(1); // unused byte

            Offset dataOfs = writer.ReserveOffset();

            writer.Write(Data.Length);

            Offset nonceOfs = writer.ReserveOffset();
            Offset tagOfs = writer.ReserveOffset();

            SeekToNext16();

            dataOfs.Satisfy();

            writer.Write(ciphertext);

            SeekToNext16();

            nonceOfs.Satisfy();

            writer.Write(nonce);

            SeekToNext16();

            tagOfs.Satisfy();

            writer.Write(tag);

            return memoryStream.ToArray();
        }

    }

}
