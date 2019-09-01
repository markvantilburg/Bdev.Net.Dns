using System.Text;

namespace Bdev.Net.Dns
{
    public class TXTRecord : RecordBase
    {
        public string Value { get; set; }

        internal TXTRecord(Pointer pointer)
        {
            Value = pointer.ReadStringValue();
        }

        public override string ToString()
        {
            return $"{Value}";
        }
    }
}