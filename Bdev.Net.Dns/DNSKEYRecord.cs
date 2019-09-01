namespace Bdev.Net.Dns
{
    public class DNSKEYRecord : RecordBase
    {
        public string Value { get; set; }
        public short Flags { get; set; }
        public byte Protocol { get; set; }
        public byte Algorithm { get; set; }

        public bool IsDnsZoneKey => (Flags & 0b01000000)==0b01000000;

        internal DNSKEYRecord(Pointer pointer)
        {
            Flags = pointer.ReadShort();
            Protocol = pointer.ReadByte();
            Algorithm = pointer.ReadByte();
            Value = pointer.ReadStringValue();
        }
    }
}