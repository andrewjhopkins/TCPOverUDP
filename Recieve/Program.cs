using TCPOverUDP;

namespace Recieve;

class Program
{
    static void Main(string[] args)
    {
        var tcpSocket = new TCPSocket(12345);
        tcpSocket.Recieve();
    }
}
