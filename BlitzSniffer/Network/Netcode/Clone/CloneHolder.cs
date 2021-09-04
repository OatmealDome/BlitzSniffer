using System.Collections.Generic;
using System.Linq;
using BlitzSniffer.Util;
using NintendoNetcode.Pia.Clone.Content;
using NintendoNetcode.Pia.Clone.Element.Data;
using NintendoNetcode.Pia.Clone.Element.Data.Event;
using NintendoNetcode.Pia.Clone.Element.Data.Reliable;
using NintendoNetcode.Pia.Clone.Element.Data.Unreliable;

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

        private Dictionary<uint, Dictionary<uint, byte[]>> Clones
        {
            get;
            set;
        }

        private Dictionary<uint, Dictionary<uint, FixedSizedQueue<ushort>>> LastEventCloneIdx;

        public delegate void CloneChangedEventHandler(object sender, CloneChangedEventArgs args);
        public event CloneChangedEventHandler CloneChanged;

        public delegate void ClockChangedEventHandler(object sender, ClockChangedEventArgs args);
        public event ClockChangedEventHandler ClockChanged;

        private object OperationLock = new object();

        private CloneHolder()
        {
            Clones = new Dictionary<uint, Dictionary<uint, byte[]>>();
            LastEventCloneIdx = new Dictionary<uint, Dictionary<uint, FixedSizedQueue<ushort>>>();
        }

        public void RegisterClone(uint id)
        {
            lock (OperationLock)
            {
                if (Clones.ContainsKey(id))
                {
                    return;
                }
                
                Clones[id] = new Dictionary<uint, byte[]>();
                LastEventCloneIdx[id] = new Dictionary<uint, FixedSizedQueue<ushort>>();
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

        public void UpdateWithContentData(CloneContentData contentData, ulong sourceId)
        {
            lock (OperationLock)
            {
                if (!Clones.ContainsKey(contentData.CloneId))
                {
                    return;
                }
                
                foreach (CloneElementData elementData in contentData.ElementData)
                {
                    switch (elementData)
                    {
                        case CloneElementDataEventData eventData:
                            UpdateElementByEventData(contentData.CloneId, sourceId, eventData);
                            break;
                        case CloneElementDataReliableData reliableData:
                            UpdateElementByReliableData(contentData.CloneId, sourceId, reliableData);
                            break;
                        case CloneElementDataUnreliable unreliableData:
                            UpdateElementByUnreliableData(contentData.CloneId, sourceId, unreliableData);
                            break;
                        default:
                            continue;
                    }
                }
            }
        }

        private void UpdateElementByEventData(uint cloneId, ulong sourceId, CloneElementDataEventData data)
        {
            if (LastEventCloneIdx[cloneId].TryGetValue(data.Id, out FixedSizedQueue<ushort> indexQueue))
            {
                bool containsIndex = indexQueue.Contains(data.Index);
                
                if (containsIndex && data.Index != data.EraseIndex)
                {
                    return;
                }

                if (!containsIndex)
                {
                    indexQueue.Enqueue(data.Index);
                }
            }
            else
            {
                FixedSizedQueue<ushort> newQueue = new FixedSizedQueue<ushort>(10);
                newQueue.Enqueue(data.Index);

                LastEventCloneIdx[cloneId][data.Id] = newQueue;
            }

            UpdateElement(cloneId, data.Id, data.Data, sourceId);
            UpdateCloneClock(data.Clock);
        }
        
        private void UpdateElementByReliableData(uint cloneId, ulong sourceId, CloneElementDataReliableData data)
        {
            UpdateElement(cloneId, data.Id, data.Data, sourceId);
            UpdateCloneClock(data.Clock);
        }
        
        private void UpdateElementByUnreliableData(uint cloneId, ulong sourceId, CloneElementDataUnreliable data)
        {
            UpdateElement(cloneId, data.Id, data.Data, sourceId);
            UpdateCloneClock(data.Clock);
        }
        
        private void UpdateElement(uint cloneId, uint elementId, byte[] data, ulong sourceId)
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

        private void UpdateCloneClock(uint clock)
        {
            ClockChanged(this, new ClockChangedEventArgs(clock));
        }

        public void Reset()
        {
            lock (OperationLock)
            {
                foreach (uint cloneId in Clones.Keys.ToList())
                {
                    Clones[cloneId] = new Dictionary<uint, byte[]>();
                    LastEventCloneIdx[cloneId] = new Dictionary<uint, FixedSizedQueue<ushort>>();
                }
            }
        }

    }
}
