using System.Net.Sockets;
using System.Net;
using System.Text;

class MyClient
{
    static async Task Main()
    {
        MySocketClient mySocketClient = new();
        await mySocketClient.CreateClient();
    }

}

class MySocketClient
{
    //private readonly IPHostEntry _ipHostInfo;
    //private readonly IPAddress _ipAddress;
    private readonly IPEndPoint _ipEndPoint;

    public MySocketClient()
    {
        //_ipHostInfo = Dns.GetHostEntry("127.0.0.1");
        //_ipAddress = _ipHostInfo.AddressList[0];
        _ipEndPoint = new(IPAddress.Parse("127.0.0.1"), 8787);
    }

    /// <summary>
    /// Create a Socket client
    /// </summary>
    /// <returns>no return</returns>
    public async Task CreateClient()
    {
        using Socket client = new(_ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp); // claim a Socket client, use TCP protocol

        Console.WriteLine("Connect to " + _ipEndPoint.Address);
        Console.WriteLine("Please Enter your name: ");

        var name = Console.ReadLine();
        if (string.IsNullOrEmpty(name)) name = "Unknown";
        /*TcpListener listener = new(_ipEndPoint); // for testing client
        listener.Start();*/ // for testing client
        await client.ConnectAsync(_ipEndPoint); // socket client connects to listener server, should RunServer() first

        while (true)
        {
            Console.Write("\nSay: ");
            var msg = Console.ReadLine();

            if (string.IsNullOrEmpty(msg)) continue;

            #region Sender
            var msgBytes = Encoding.UTF8.GetBytes(name + "$" + msg + "$" + _ipEndPoint.Address);
            var sender = await client.SendAsync(msgBytes, SocketFlags.None); // send message, return how many bytes have been sent
            // socketflags specifies socket send and receive behaviors
            // https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.socketflags?view=net-7.0
            #endregion

            if (sender > 0) Console.WriteLine($"message has been sent. (message: {msg})");

            #region Receiver
            try
            {
                var buffer = new byte[128]; // cuz we all know message has been encoded in bytes, store msg from receiver
                var receiver = await client.ReceiveAsync(buffer, SocketFlags.None); // get an ecoding message from sender
                /*should not use Receive(), it's a blocking call, if there is no available data, Receive() will block until data is available*/
                var decoder = Encoding.UTF8.GetString(buffer, 0, receiver); // decode buffer, index 0, how many bytes we have to decode
                var msgArray = decoder.Split("$");
                
                if(msgArray.Length >= 3)
                {
                    if (msgArray[0] == "Ack")
                        Console.WriteLine(msgArray[2]); // acknowledge from server
                    else if (msgArray[0] == "Msg")
                        Console.WriteLine(msgArray[1] + " said: " + msgArray[2]);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Client receive error: {ex.Message}");
                break;
            }
            #endregion

            if (msg == "end") break;
        }

        // close client
        //listener.Stop(); // for testing client
        client.Shutdown(SocketShutdown.Both);
    }
}