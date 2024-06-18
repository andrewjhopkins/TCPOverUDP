using System.Net;
using System.Net.Sockets;

namespace TCPOverUDP
{
    public class TCPSocket
    {
        public string DestinationIpAddress { get; set; }
        public ushort DestinationPort { get; set; }
        public ushort SourcePort { get; set; }
        public ushort WindowSize { get; set; } = 1;

        private uint Seq { get; set; }
        private int LastSentIndex { get; set; } = -1;

        private List<TCPPacket> Window { get; set; } = new List<TCPPacket>();

        // Client Socket
        public TCPSocket(string destinationIpAddress, ushort destinationPort, ushort sourcePort) 
        {
            DestinationIpAddress = destinationIpAddress;
            DestinationPort = destinationPort;
            SourcePort = sourcePort;

            Seq = (uint)new Random().Next(-int.MaxValue, int.MaxValue);

            var synPacket = new TCPPacket(sourcePort, destinationPort, Seq, 0, WindowSize, true, false, new byte[0]);
            var synPacketBytes = synPacket.ToBytes();

            SendViaUDP(synPacketBytes);
            Console.WriteLine($"Sent SYN packet... to port: {DestinationPort}. handshake 1/3");

            TCPPacket synAckPacket = null;
            while (synAckPacket == null || !(synAckPacket.ACK && synAckPacket.SYN && synAckPacket.AcknowledgementNumber == Seq + 1)) 
            {
                if (synAckPacket != null)
                {
                    Console.WriteLine("Invalid SYN ACK packet recieved");
                }

                Console.WriteLine($"Waiting for SYN ACK... on port: {SourcePort}");
                (synAckPacket, var receiveIpAddress, var recievePort) = RecieveViaUDP(DestinationPort);
                Console.WriteLine($"Recieved SYN ACK packet... from port: {recievePort} handshake 2/3");
            }


            var ackPacket = new TCPPacket(SourcePort, DestinationPort, Seq, synAckPacket.SequenceNumber + 1, WindowSize, false, true, new byte[0]);
            SendViaUDP(ackPacket.ToBytes());
            Console.WriteLine("Sending ACK packet... handshake 3/3");
        }

        // Server Socket
        public TCPSocket(ushort sourcePort)
        {
            SourcePort = sourcePort;
            Seq = (uint)new Random().Next(-int.MaxValue, int.MaxValue);

            Console.WriteLine($"Listening... on port: {SourcePort}");
            var (synPacket, destinationIpAddress, destinationPort) = RecieveViaUDP();
            Console.WriteLine($"Recieved SYN packet... from port: {destinationPort} handshake 1/3");

            DestinationIpAddress = destinationIpAddress;
            DestinationPort = destinationPort;

            var synAckPacket = new TCPPacket(sourcePort, destinationPort, Seq, synPacket.SequenceNumber + 1, WindowSize, true, true, new byte[0]);
            SendViaUDP(synAckPacket.ToBytes());
            Console.WriteLine($"Sending SYN ACK packet... to port: {destinationPort} handshake 2/3");

            TCPPacket ackPacket = null;
            while (ackPacket == null || !(ackPacket.ACK && !ackPacket.SYN && ackPacket.AcknowledgementNumber == Seq + 1))
            {
                if (ackPacket != null)
                { 
                    Console.WriteLine("Invalid ACK packet recieved");
                }

                (ackPacket, var recieveIpAddress, var recievePort) = RecieveViaUDP(DestinationPort);
                Console.WriteLine($"Ack packet recieved... from port: {recievePort} handshake 3/3");
            }

            Console.WriteLine("Connected");
            return;
        }

        public void Send(string filePath)
        {
            var path = Path.GetFullPath(filePath);
            var fileInfo = new FileInfo(path);

            if (!fileInfo.Exists)
            {
                Console.WriteLine($"File: {filePath} does not exist");
                return;
            }

            var fileName = fileInfo.Name;
            var fileSize = fileInfo.Length;

            using (var fileStream = File.OpenRead(filePath))
            { 
                var packets = new List<TCPPacket>();
                Console.WriteLine("Start sending...");

                var readBytes = 0;

                while (readBytes < fileSize)
                {
                    packets.Clear();
                    for (var i = 0; readBytes < fileSize; i++)
                    {
                        var data = new byte[1408 - TCPPacket.DataOffset];
                        var lengthOfBytesRead = fileStream.Read(data, 0, data.Length);

                        var fileData = new byte[1408 - TCPPacket.DataOffset];
                        Array.Copy(data, fileData, lengthOfBytesRead);

                        if (lengthOfBytesRead == 0)
                        {
                            break;
                        }

                        readBytes += lengthOfBytesRead;
                        var packet = new TCPPacket(SourcePort, DestinationPort, Seq, 0, WindowSize, false, false, fileData);
                        packets.Add(packet);
                        Seq += packet.DataLength + 1;
                    }

                    Window.AddRange(packets);

                    for (var i = LastSentIndex + 1; i < WindowSize && i < Window.Count(); i++)
                    { 
                        var packet = Window[i];
                        SendViaUDP(packet.ToBytes());
                        Console.WriteLine($"Packet sent.. {packet.SequenceNumber}");
                        LastSentIndex += 1;
                    }

                    Console.WriteLine($"WindowSize : {Window.Count()}, LastSentIndex: {LastSentIndex}, Progress: %{readBytes / fileSize * 100}");
                    // TODO: Acknowledge Recieve()
                }
            }
        }

        private void SendViaUDP(byte[] data)
        {
            using (var udpClient = new UdpClient(SourcePort))
            {
                udpClient.Connect(DestinationIpAddress, DestinationPort);
                udpClient.Send(data, data.Length);
                udpClient.Close();
            }

            return;
        }

        private (TCPPacket, string, ushort) RecieveViaUDP(ushort destinationPort = 0)
        {
            using (var udpClient = new UdpClient(SourcePort))
            {
                IPEndPoint RemoteIPEndPoint = new IPEndPoint(IPAddress.Any, destinationPort);
                var recieveBytes = udpClient.Receive(ref RemoteIPEndPoint);
                udpClient.Close();
                var tcpPacket = new TCPPacket(recieveBytes);
                return (tcpPacket, RemoteIPEndPoint.Address.ToString(), (ushort) RemoteIPEndPoint.Port);
            }
        }
    }
}
