using System;
using Bdev.Net.Dns.Helpers;

namespace Bdev.Net.Dns
{
    /// <summary>
    /// 
    /// </summary>
    /// <see cref="https://www.ietf.org/rfc/rfc4034.txt"/>
    public class RRSIGRecord : RecordBase
    {
        public string SignersName { get; set; }
        public string Signature { get; set; }
        public short TypeCovered { get; set; }
        public byte Labels { get; set; }
        public byte Algorithm { get; set; }

        public int OriginalTtl { get; set; }
        public int SignatureExpiration { get; set; }

        public DateTime SignatureExpirationDate => Extensions.RFCBaseDate.AddSeconds(SignatureExpiration);
        public int SignatureInception { get; set; }
        public DateTime SignatureInceptionDate => Extensions.RFCBaseDate.AddSeconds(SignatureInception);

        public ushort KeyTag { get; set; }

        internal RRSIGRecord(Pointer pointer)
        {
            TypeCovered = pointer.ReadShort();
            Algorithm = pointer.ReadByte();
            Labels = pointer.ReadByte();
            OriginalTtl = pointer.ReadInt();
            SignatureExpiration = pointer.ReadInt();
            SignatureInception = pointer.ReadInt();
            KeyTag = pointer.ReadUShort();
            SignersName = pointer.ReadDomain();
            Signature = pointer.ReadStringValue();
        }
    }
}