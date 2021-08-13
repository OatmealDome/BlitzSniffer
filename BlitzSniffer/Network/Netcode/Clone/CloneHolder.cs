﻿using System.Collections.Generic;
using System.Linq;

namespace BlitzSniffer.Network.Netcode.Clone
{
    class CloneHolder
    {
        private static CloneHolder _Instance = null;

        public static CloneHolder Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new CloneHolder();
                }

                return _Instance;
            }
        }

        public Dictionary<uint, Dictionary<uint, byte[]>> Clones
        {
            get;
            set;
        }

        public delegate void CloneChangedEventHandler(object sender, CloneChangedEventArgs args);
        public event CloneChangedEventHandler CloneChanged;

        public delegate void ClockChangedEventHandler(object sender, ClockChangedEventArgs args);
        public event ClockChangedEventHandler ClockChanged;

        private object OperationLock = new object();

        private CloneHolder()
        {
            Clones = new Dictionary<uint, Dictionary<uint, byte[]>>();
        }

        public void RegisterClone(uint id)
        {
            // There is potential for a race condition here, but all operations that access RegisterClone
            // and IsCloneRegistered all occur on the same thread, so it shouldn't be an issue for now.

            if (IsCloneRegistered(id))
            {
                return;
            }

            lock (OperationLock)
            {
                Clones[id] = new Dictionary<uint, byte[]>();
            }
        }

        public bool IsCloneRegistered(uint id)
        {
            lock (OperationLock)
            {
                return Clones.ContainsKey(id);
            }
        }

        public Dictionary<uint, byte[]> GetClone(uint id)
        {
            lock (OperationLock)
            {
                if (Clones.TryGetValue(id, out Dictionary<uint, byte[]> cloneData))
                {
                    return cloneData;
                }
            }

            throw new SnifferException($"Clone {id} not found");
        }

        public void UpdateElementInClone(uint cloneId, uint elementId, byte[] data, ulong sourceId)
        {
            lock (OperationLock)
            {
                if (Clones.TryGetValue(cloneId, out Dictionary<uint, byte[]> cloneData))
                {
                    bool isSimilar;
                    if (cloneData.TryGetValue(elementId, out byte[] elementData))
                    {
                        isSimilar = elementData.SequenceEqual(data);
                    }
                    else
                    {
                        isSimilar = false;
                    }

                    cloneData[elementId] = data;

                    CloneChanged(this, new CloneChangedEventArgs(cloneId, elementId, data, isSimilar, sourceId));
                }
                else
                {
                    throw new SnifferException($"Clone {cloneId} not found");
                }
            }
        }

        public void UpdateCloneClock(uint clock)
        {
            ClockChanged(this, new ClockChangedEventArgs(clock));
        }

        public void Reset()
        {
            lock (OperationLock)
            {
                foreach (uint cloneId in Clones.Keys)
                {
                    Clones[cloneId] = new Dictionary<uint, byte[]>();
                }
            }
        }

    }
}
