using System.Net;
using System.Net.Sockets;
using System.Text;

/*
 * https://learn.microsoft.com/zh-tw/dotnet/fundamentals/networking/sockets/socket-services
 * **/

class Program
{
    static async Task Main()
    {
        MySocketServer training = new();

        await training.RunServer();
    }
}

class MySocketServer
{
    private readonly IPHostEntry _ipHostInfo; 
    private readonly IPAddress _ipAddress;
    private readonly IPEndPoint _ipEndPoint;

    public MySocketServer()
    {
        _ipHostInfo = Dns.GetHostEntry(Dns.GetHostName()); // claim an entry for hosting a simple server. Dns.GetHostName() == localhost
        _ipAddress = _ipHostInfo.AddressList[0]; // get entry's IP, it contains ipv4, ipv6, lan ip, wan ip...
        _ipEndPoint = new(_ipAddress, 8787); // endpoint can be used for client to connect, listener for host. port number = 8787
    }

    /// <summary>
    /// Run a Socket server
    /// </summary>
    /// <returns>no return</returns>
    public async Task RunServer()
    {
        using Socket listener = new(_ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        listener.Bind(_ipEndPoint); // to associate the socket with the network address
        listener.Listen(100); // 100 => The maximum length of the pending connections queue

        Console.WriteLine($"Server is hosting, port: {_ipEndPoint.Port}"); // update: print message first
        var handler = await listener.AcceptAsync();// blocking call, program will handle until receiving data

        while (true)
        {
            try
            {
                var buffer = new byte[32]; // buffer for storing message
                var receiver = await handler.ReceiveAsync(buffer, SocketFlags.None); // receive message to buffer, and get bytes size
                var decoder = Encoding.UTF8.GetString(buffer, 0, receiver);

                Console.WriteLine($"Server side received message: {decoder}");

                if (decoder == "end")
                {
                    Console.WriteLine("Shutting the server down...");
                    break;
                }

                var echo = $"Server got message, acknowledgment.";
                var echoByte = Encoding.UTF8.GetBytes(echo);
                await handler.SendAsync(echoByte, 0); // sender, server ack
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                // break; // don't break here, but using Exception Message to notify that client has exited
            }
        }

    }
}