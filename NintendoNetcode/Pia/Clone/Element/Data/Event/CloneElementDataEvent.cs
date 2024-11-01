﻿using Syroot.BinaryData;

namespace NintendoNetcode.Pia.Clone.Element.Data.Event
{
    public abstract class CloneElementDataEvent : CloneElementData
    {
        private byte Type
        {
            get;
            set;
        }

        public CloneElementDataEvent(BinaryDataReader reader) : base(reader)
        {
            Type = reader.ReadByte();
        }

    }
}
