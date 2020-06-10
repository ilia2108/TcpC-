using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Linq;

namespace Server
{
    class Program
    {
        const int port = 8888;
        const string IP = "127.0.0.1";
        const int SERVER_KEY = 54621;
        const int CLIENT_KEY = 45328;



        static void SendMessage(NetworkStream stream, string message){
            var msg = Encoding.ASCII.GetBytes(message + "\\a\\b");
            stream.Write(msg, 0, msg.Length);
        }
        static bool Authenticate(NetworkStream stream, string raw)
        {
            int lettersHash = 0;
            foreach(var i in raw){
                lettersHash+=(int)i;
            }
            lettersHash = (lettersHash * 1000) % 65536;
            var serverHash = (lettersHash + SERVER_KEY)% 65536;
            var expectedClientHash = (lettersHash + CLIENT_KEY) %65536;

            SendMessage(stream, serverHash.ToString());
            Console.WriteLine($"{serverHash} is sent");

            var response = GetData(stream);
            if(Convert.ToInt32(response) == expectedClientHash)
            {
                SendMessage(stream, "200 OK");
                return true;
            }
            else
            {
                throw new LoginFailedException();
            }
        }

        static string GetData(NetworkStream stream){
            int i;
            Byte[] bytes = new Byte[256];
            String data = null;
            while((i = stream.Read(bytes, 0, bytes.Length))!=0)
            {
                // Translate data bytes to a ASCII string.
                data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);

                if(!data.EndsWith("\\a\\b"))
                {
                    Console.WriteLine("String is NOT OK");
                    throw new SyntaxErrorException();
                }
                data.Replace("\\a\\b", string.Empty);

                Console.WriteLine($"Received: {data}");
            }
            return data;
        }
        
        static void Main(string[] args)
        {
            TcpListener server = null;
            try
            {
                //server initialization in localhost
                IPAddress localAddr = IPAddress.Parse(IP);
                
                server = new TcpListener(localAddr, port);
                server.Server.SendTimeout = 1;
                server.Server.ReceiveTimeout = 1;
                server.Start();

                
               
                while(true)
                {
                    var client = server.AcceptTcpClient();
                    var stream = client.GetStream();
                    string data = GetData(stream);
                    
                    Authenticate(stream, data);
                    
                    client.Close();
                    break;

                }
            }
            catch(Exception e){
                Console.WriteLine($"Exception: {e}");
                return;
            }
            finally{
                Console.WriteLine("Something went wrong. Please try again.");
                server.Stop();
            }
        }
    }
}
