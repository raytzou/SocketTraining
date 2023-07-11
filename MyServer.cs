using System.Net;
using System.Net.Sockets;
using System.Text;

class MyServer
{
    static void Main()
    {
        MyTCPListener training = new();

        training.RunServer();
    }
}

class MyTCPListener
{
    private readonly IPEndPoint _endPoint;
    private static List<TcpClient> _clientList = null!;

    public MyTCPListener()
    {
        _endPoint = new(IPAddress.Any, 8787); // broadcast, any = IP: 0.0.0.0
        _clientList = new List<TcpClient>();
    }

    enum MessageCategory
    {
        Ack,Msg
    }

    public void RunServer()
    {
        TcpListener listener = new(_endPoint);

        listener.Start();

        Console.WriteLine($"Server is hosting, port: {_endPoint.Port}");
        
        Thread fork = new(ClientHandle);
        fork.Start();
        
        while (true)
        {
            var client = listener.AcceptTcpClient();

            lock (_clientList) // Runtime Exception: Collection was modified; enumeration operation may not execute.
            {
                _clientList.Add(client);
            }
            Console.WriteLine($"{DateTime.Now} A client has connected.");
        }
    }

    private void ClientHandle() // async for keeping handling client
    {
        while (true)
        {
            lock ( _clientList) // Runtime Exception: Collection was modified; enumeration operation may not execute.
            {
                foreach (var client in _clientList)
                {
                    if (client.Available > 0) // when we got some datas (we all know that is just message) from the god damn client
                    {
                        ReadMessage(client);
                        ServerAcknowledge(client);
                    }
                }
            }
        }
    }

    private static void ReadMessage(TcpClient client)
    {
        try
        {
            NetworkStream stream = client.GetStream();
            var buffer = new byte[512];
            var receiver = stream.Read(buffer);
            var decoder = Encoding.UTF8.GetString(buffer);

            Console.WriteLine($"{DateTime.Now} {decoder}");
            SendMessage(decoder, client);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{DateTime.Now} {ex.Message}");
        }
    }

    /// <summary>
    /// Send message to everyone, exclude itself.
    /// </summary>
    /// <param name="message">string: message</param>
    /// <param name="theClient">TcpClient: The message sender</param>
    private static void SendMessage(string message, TcpClient theClient)
    {
        foreach(var client in _clientList)
        {
            if (client == theClient) continue;

            NetworkStream stream = client.GetStream();
            var msgBytes = Encoding.UTF8.GetBytes(MessageCategory.Msg + "$" + message);
            
            stream.Write(msgBytes, 0, msgBytes.Length);
        }
    }

    /// <summary>
    /// Server acknowledge. For client that uses Socket.Receive() which is blocking method.
    /// </summary>
    /// <param name="client"></param>
    private static void ServerAcknowledge(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        var echo = $"{MessageCategory.Ack}${DateTime.Now}$Server acknowledge.";
        var encode = Encoding.UTF8.GetBytes(echo);
        
        stream.Write(encode, 0, encode.Length);
    }
}