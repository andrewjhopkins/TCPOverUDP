using TCPOverUDP;

namespace Send;

class Program
{
    static void Main(string[] args)
    {
        var tcpSocket = new TCPSocket("127.0.0.1", 12345, 54321);
        Thread.Sleep(2000);
        tcpSocket.Send("test.txt");
    }
}
