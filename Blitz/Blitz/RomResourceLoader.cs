﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LibHac;
using LibHac.IO;
using Nintendo.Archive;
using Nintendo.Switch;
using Syroot.NintenTools.Yaz0;

namespace Blitz
{
    public class RomResourceLoader : IDisposable
    {
        private static byte[] Yaz0MagicNumbers = Encoding.ASCII.GetBytes("Yaz0");

        public static RomResourceLoader Instance = null;

        private NcaWrapper NcaWrapper;
        private Dictionary<string, Sarc> PackFiles;

        internal RomResourceLoader(Keyset keyset, string baseNca, string updateNca)
        {
            // Load the base and update NCAs
            NcaWrapper = new NcaWrapper(keyset, baseNca, updateNca);

            // Create dictionary to hold pack files
            PackFiles = new Dictionary<string, Sarc>();

            // Load the pack directory
            IDirectory packDirectory = NcaWrapper.Romfs.OpenDirectory("/Pack", OpenDirectoryMode.Files);

            // Read every directory entries
            foreach (DirectoryEntry directoryEntry in packDirectory.Read())
            {
                // Load the SARC
                Sarc sarc = new Sarc(GetRomFileFromRomfs(directoryEntry.FullPath));

                // Loop over every file
                foreach (string file in sarc)
                {
                    // Add this file to the pack files dictionary
                    PackFiles.Add("/" + file, sarc);
                }
            }
        }

        public static void Initialize(Keyset keyset, string baseNca, string updateNca)
        {
            if (Instance != null)
            {
                Instance.Dispose();
                Instance = null;
            }

            Instance = new RomResourceLoader(keyset, baseNca, updateNca);
        }

        public void Dispose()
        {
            NcaWrapper.Dispose();

            foreach (Sarc sarc in PackFiles.Values)
            {
                sarc.Dispose();
            }

            PackFiles = null;
        }

        public Stream GetRomFile(string romPath)
        {
            // Attempt to load from the packs first
            Stream stream;
            if (PackFiles.TryGetValue(romPath, out Sarc sarc))
            {
                string gamePath = romPath.StartsWith('/') ? romPath.Substring(1) : romPath;

                // Create a MemoryStream
                stream = new MemoryStream(sarc[gamePath]);
            }
            else
            {
                // Load from the romfs
                stream = GetRomFileFromRomfs(romPath);
            }

            // Check if this is a nisasyst file
            // .pack files are automatically considered to be not nisasyst-encrypted,
            // since a nisasyst-encrypted file can be placed at the end and trip up
            // the check. Let's hope that a pack doesn't become nisasyst encrypted.
            if (Path.GetExtension(romPath) != ".pack" && Nisasyst.IsNisasystFile(stream))
            {
                // Get the game path for Nisasyst
                string gamePath = romPath.StartsWith('/') ? romPath.Substring(1) : romPath;

                // Create a new MemoryStream
                MemoryStream memoryStream = new MemoryStream();

                // Decrypt the file
                Nisasyst.Decrypt(stream, memoryStream, gamePath);

                // Switch the streams
                stream.Dispose();
                stream = memoryStream;
            }

            // Read the first four bytes
            byte[] firstBytes = new byte[4];
            stream.Read(firstBytes, 0, 4);
            stream.Seek(0, SeekOrigin.Begin);

            // Check if the file is Yaz0 compressed
            if (firstBytes.SequenceEqual(Yaz0MagicNumbers))
            {
                // Create a new MemoryStream
                MemoryStream bufferStream = new MemoryStream();

                // Attempt to Yaz0 decompress this file
                Yaz0Compression.Decompress(stream, bufferStream);

                // Seek to the beginning
                bufferStream.Seek(0, SeekOrigin.Begin);

                // Switch the streams
                stream.Dispose();
                stream = bufferStream;
            }

            // Return the stream
            return stream;
        }

        public Stream GetRomFileFromRomfs(string romPath)
        {
            // Load the file
            IFile file = NcaWrapper.Romfs.OpenFile(romPath, OpenMode.Read);

            // Get the file as a Stream
            return file.AsStream();
        }

        public IList<string> GetFilesInDirectory(string path)
        {
            // Create a list
            List<string> filesList = new List<string>();

            if (NcaWrapper.Romfs.DirectoryExists(path))
            {
                // Load the pack directory
                IDirectory packDirectory = NcaWrapper.Romfs.OpenDirectory(path, OpenDirectoryMode.Files);

                // Read every directory entries
                foreach (DirectoryEntry directoryEntry in packDirectory.Read())
                {
                    // Add this file to the list
                    filesList.Add(directoryEntry.FullPath);
                }
            }

            // Get the number of slashes that should be in the path
            int slashCount = path.Where(c => c == '/').Count() + (path.EndsWith('/') ? 0 : 1);

            // Go through every pack file to see if there's anything that matches
            foreach (string packPath in PackFiles.Keys)
            {
                // Check if the path starts with the search path and that the slash count is equal
                // If the slash count is equal, then the file is in the directory and not within
                // a subdirectory.
                if (packPath.StartsWith(path) && packPath.Where(c => c == '/').Count() == slashCount)
                {
                    // Add this file to the list
                    filesList.Add(packPath);
                }
            }

            // Check if there's any matching 
            return filesList;
        }

    }
}