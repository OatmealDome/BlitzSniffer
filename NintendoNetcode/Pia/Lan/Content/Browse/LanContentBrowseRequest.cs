﻿using NintendoNetcode.Pia.Lan.Content.Browse.Crypto;
using Syroot.BinaryData;

namespace NintendoNetcode.Pia.Lan.Content.Browse
{
    public class LanContentBrowseRequest : LanContent
    {
        public byte[] SessionSearchCriteria
        {
            get;
            set;
        }

        public LanCryptoContentChallenge CryptoChallenge
        {
            get;
            set;
        }

        public LanContentBrowseRequest() : base(null)
        {

        }

        public LanContentBrowseRequest(BinaryDataReader reader) : base(reader)
        {
            uint criteriaSize = reader.ReadUInt32();
            SessionSearchCriteria = reader.ReadBytes((int)criteriaSize);
            CryptoChallenge = new LanCryptoContentChallenge(reader);
        }

        public override void Serialize(BinaryDataWriter writer)
        {
            writer.Write(SessionSearchCriteria.Length);
            writer.Write(SessionSearchCriteria);
            CryptoChallenge.Serialize(writer);
        }

    }
}
