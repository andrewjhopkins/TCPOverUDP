namespace TCPOverUDP
{
    public class TCPPacket
    {
        public ushort SourcePort { get; set; } 
        public ushort DestinationPort { get; set; } 
        public uint SequenceNumber { get; set; }
        public uint AcknowledgementNumber { get; set; }
        public bool SYN { get; set; }
        public bool ACK { get; set; }
        public ushort WindowSize { get; set; }
        public uint DataLength { get; set; }
        public byte[] Data { get; set; }

        public static int DataOffset =
            sizeof(ushort) +
            sizeof(ushort) +
            sizeof(uint) +
            sizeof(uint) +
            sizeof(bool) +
            sizeof(bool) +
            sizeof(ushort) +
            sizeof(int);

        public TCPPacket(ushort sourcePort, ushort destinationPort, uint sequenceNumber, uint acknowledgementNumber, ushort windowSize, bool syn, bool ack, byte[] data)
        {
            SourcePort = sourcePort;
            DestinationPort = destinationPort;
            SequenceNumber = sequenceNumber;
            AcknowledgementNumber = acknowledgementNumber;
            WindowSize = windowSize;
            SYN = syn;
            ACK = ack;
            Data = data ?? new byte[0];
            DataLength = (uint)Data.Length;
        }

        public TCPPacket(byte[] data) 
        { 
            var position = 0;
            SourcePort = BitConverter.ToUInt16(data.Take(sizeof(ushort)).ToArray());
            position += sizeof(ushort);

            DestinationPort = BitConverter.ToUInt16(data.Skip(position).Take(sizeof(ushort)).ToArray());
            position += sizeof(ushort);

            SequenceNumber = BitConverter.ToUInt32(data.Skip(position).Take(sizeof(uint)).ToArray());
            position += sizeof(uint);

            AcknowledgementNumber = BitConverter.ToUInt32(data.Skip(position).Take(sizeof(uint)).ToArray());
            position += sizeof(uint);

            ACK = BitConverter.ToBoolean(data.Skip(position).Take(sizeof(bool)).ToArray());
            position += sizeof(bool);

            SYN = BitConverter.ToBoolean(data.Skip(position).Take(sizeof(bool)).ToArray());
            position += sizeof(bool);

            WindowSize = BitConverter.ToUInt16(data.Skip(position).Take(sizeof(ushort)).ToArray());
            position += sizeof(ushort);

            DataLength = BitConverter.ToUInt32(data.Skip(position).Take(sizeof(uint)).ToArray());
            position += sizeof(uint);

            Data = data.Skip(position).Take((int)DataLength).ToArray();

            return;
        }

        public byte[] ToBytes()
        {
            var position = 0;

            var bytes = new byte[DataOffset + DataLength];

            BitConverter.GetBytes(SourcePort).ToArray().CopyTo(bytes, 0);
            position += sizeof(ushort);

            BitConverter.GetBytes(DestinationPort).ToArray().CopyTo(bytes, position);
            position += sizeof(ushort);

            BitConverter.GetBytes(SequenceNumber).ToArray().CopyTo(bytes, position);
            position += sizeof(uint);

            BitConverter.GetBytes(AcknowledgementNumber).ToArray().CopyTo(bytes, position);
            position += sizeof(uint);

            BitConverter.GetBytes(ACK).ToArray().CopyTo(bytes, position);
            position += sizeof(bool);

            BitConverter.GetBytes(SYN).ToArray().CopyTo(bytes, position);
            position += sizeof(bool);

            BitConverter.GetBytes(WindowSize).ToArray().CopyTo(bytes, position);
            position += sizeof(ushort);

            BitConverter.GetBytes(DataLength).ToArray().CopyTo(bytes, position);
            position += sizeof(int);

            Data.CopyTo(bytes, position);

            return bytes;
        }
    }
}
