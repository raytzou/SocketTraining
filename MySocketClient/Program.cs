using System.Net.Sockets;
using System.Net;
using System.Text;

class Program
{
    static async Task Main()
    {
        MySocketClient mySocketClient = new();
        await mySocketClient.CreateClient();
    }

}

class MySocketClient
{
    private readonly IPHostEntry _ipHostInfo;
    private readonly IPAddress _ipAddress;
    private readonly IPEndPoint _ipEndPoint;

    public MySocketClient()
    {
        _ipHostInfo = Dns.GetHostEntry(Dns.GetHostName()); // claim an entry for hosting a simple server. Dns.GetHostName() == localhost
        _ipAddress = _ipHostInfo.AddressList[0]; // get entry's IP, it contains ipv4, ipv6, lan ip, wan ip...
        _ipEndPoint = new(_ipAddress, 8787); // endpoint can be used for client to connect, listener for host. port number = 8787
    }

    /// <summary>
    /// Create a Socket client
    /// </summary>
    /// <returns>no return</returns>
    public async Task CreateClient()
    {
        using Socket client = new(_ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp); // claim a Socket client, use TCP protocol

        Console.WriteLine("Connect to " + _ipAddress.ToString());

        /*TcpListener listener = new(_ipEndPoint); // for testing client
        listener.Start();*/ // for testing client
        await client.ConnectAsync(_ipEndPoint); // socket client connects to listener server, should RunServer() first

        while (true)
        {
            Console.Write("\nClient Type: ");
            string msg = Console.ReadLine();

            if (string.IsNullOrEmpty(msg)) continue;

            #region Sender
            var msgBytes = Encoding.UTF8.GetBytes(msg);
            var sender = await client.SendAsync(msgBytes, SocketFlags.None); // send message, return how many bytes have been sent
            // socketflags specifies socket send and receive behaviors
            // https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.socketflags?view=net-7.0
            #endregion

            if (sender > 0) Console.WriteLine($"message has been sent. (message: {msg})");

            #region Receiver
            var buffer = new byte[32]; // cuz we all know message has been encoded in bytes, store msg from receiver
            var receiver = await client.ReceiveAsync(buffer, SocketFlags.None); // get an ecoding message from sender
            /*should not use Receive(), it's a blocking call, if there is no available data, Receive() will block until data is available*/
            var decoder = Encoding.UTF8.GetString(buffer, 0, receiver); // decode buffer, index 0, how many bytes we have to decode
            #endregion

            Console.WriteLine(decoder); // acknowledge from server

            if (msg == "end") break;
        }

        // close client
        //listener.Stop(); // for testing client
        client.Shutdown(SocketShutdown.Both);
    }
}