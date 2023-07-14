using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
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
            if (client.Connected)
            {
                NetworkStream stream = client.GetStream();
                var buffer = new byte[512];
                var receiver = stream.Read(buffer);
                var decoder = Encoding.UTF8.GetString(buffer);
                var msgArray = decoder.Split("$");

                Console.WriteLine($"{DateTime.Now} {msgArray[0]} said: {msgArray[1]}, from {msgArray[2]}");
                SendMessage(decoder, client);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{DateTime.Now} ReadError({new StackFrame(0, true).GetFileLineNumber()}): {ex.Message}");
        }
    }

    /// <summary>
    /// Send message to everyone, exclude itself.
    /// </summary>
    /// <param name="message">string: message</param>
    /// <param name="theClient">TcpClient: The message sender</param>
    private static void SendMessage(string message, TcpClient theClient)
    {
        foreach (var client in _clientList)
        {
            if (client == theClient || !client.Connected) continue;

            try
            {
                NetworkStream stream = client.GetStream();
                if (stream.CanWrite)  // need to fix if client is lost
                {
                    Console.WriteLine("Server sending");
                    var msgBytes = Encoding.UTF8.GetBytes(MessageCategory.Msg + "$" + message);

                    stream.Write(msgBytes, 0, msgBytes.Length);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} SendError({new StackFrame(0, true).GetFileLineNumber()}): {ex.Message}");
                //client.GetStream().Close();
                client.Close(); // close the client, next round will not communicate with it
            }
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