using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace Server
{
    class Program
    {
        const int port = 8888;
        const string IP = "127.0.0.1";
        const int SERVER_KEY = 54621;
        const int CLIENT_KEY = 45328;



        static void SendMessage(NetworkStream stream, string message)
        {
            Console.WriteLine($"{message + "\a\b"} is sent");
            var msg = Encoding.ASCII.GetBytes(message + "\a\b");
            stream.Write(msg, 0, msg.Length);
        }
        static bool Authenticate(NetworkStream stream, string raw)
        {
            Console.WriteLine("try to login...");

            int lettersHash = 0;
            foreach(var i in raw){
                lettersHash+=(int)i;
            }
            lettersHash = (lettersHash * 1000) % 65536;
            var serverHash = (lettersHash + SERVER_KEY)% 65536;
            var expectedClientHash = (lettersHash + CLIENT_KEY) %65536;

            SendMessage(stream, serverHash.ToString());
           
            Console.WriteLine("trying to get some data...");
            //ToDo: fix
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
        
        static (int, int) getCoordsFromString(string str)
        {
            var list = str.Split(' ');

            if(list[0] != "OK")
                throw new SyntaxErrorException();
            
            try
            {
                  return (
                    Convert.ToInt32(list[1]),
                    Convert.ToInt32(list[2])
                    );
            }
            catch
            {
                throw new SyntaxErrorException();
            }

        }
        static ((int, int), string) FindCoordsAndDirection(NetworkStream stream)
        {
            string raw_pos1, raw_pos2;
            do
            {
                SendMessage(stream, "102 MOVE");
                raw_pos1 = GetData(stream);

                SendMessage(stream, "102 MOVE");
                raw_pos2 = GetData(stream);
            }
            while(raw_pos1 == raw_pos2);

            var pos1 = getCoordsFromString(raw_pos1);
            var pos2 = getCoordsFromString(raw_pos2);
            string direction = string.Empty;
            if(pos1.Item1 < pos2.Item1)
               direction = "Down";
            else if(pos1.Item1 > pos2.Item1)
                direction = "Up";
            if(pos1.Item2 < pos2.Item2)
                direction = "Right";
            else if(pos1.Item2 > pos2.Item2)
                direction = "Left";

            return (pos2, direction);
        }
        
        static string GetMessage(NetworkStream stream, string direction){
            string msg = string.Empty;
            Rotate(stream, direction, "Up");
            for(int i = 0; i< 4; ++i){
                for(int j = 0; j< 4; ++j){
                    SendMessage(stream, "103 TURN LEFT");
                    msg = GetData(stream);
                    for(int k = 0; k< 4-i; ++k){
                        while(msg.StartsWith("OK")){
                            SendMessage(stream, "105 GET MESSAGE");
                            msg = GetData(stream);
                        }
                        if(msg != "")
                            return msg;
                        SendMessage(stream, "102 MOVE");
                        getCoordsFromString(GetData(stream));
                    }
                }
            }
            return msg;
        }
        static string Rotate(NetworkStream stream, string From, string To)
        {
            var directions = new Dictionary<string, int>();
            directions.Add("Left", 0);
            directions.Add("Up", 1);
            directions.Add("Right", 2);
            directions.Add("Down", 3);

            var current = directions[From];
            var dest = directions[To];

            if(current == dest)
                return To;
            var diff = dest - current;

            for(int i = 0; i< Math.Abs(diff); ++i){
                if(current < dest)
                    SendMessage(stream, "104 TURN RIGHT");
                else
                    SendMessage(stream, "103 TURN LEFT");
            }
            return To;

        }
        static (int, int) UpdatePosition(NetworkStream stream, (int, int) position, bool isX, int MaxCoord)
        {
           bool border = isX? (position.Item1 != MaxCoord): (position.Item2 != MaxCoord);
           while(border){
               SendMessage(stream, "102 MOVE");
               position = getCoordsFromString(GetData(stream));
           }
           return position;
        }
        static void Move(NetworkStream stream)
        {
            var res = FindCoordsAndDirection(stream);
            var coords = res.Item1;
            var direction = res.Item2;

            if(coords.Item1 > -2){
                direction = Rotate(stream, direction, "Up");
                coords = UpdatePosition(stream, coords, false, -2);
            }
            if(coords.Item1 < -2){
                direction = Rotate(stream, direction, "Down");
                coords = UpdatePosition(stream, coords, false, -2);
            }
            if(coords.Item2 > 2){
                 direction = Rotate(stream, direction, "Left");
                coords = UpdatePosition(stream, coords, true, 2);
            }
            if(coords.Item2 < 2){
                 direction = Rotate(stream, direction, "Right");
                coords = UpdatePosition(stream, coords, true, 2);
            }
            string message = GetMessage(stream, direction);
            SendMessage(stream, "106 LOGOUT");
        }
        static string GetData(NetworkStream stream){
            int i;
            Byte[] bytes = new Byte[256];
            String data = null;
            while((i = stream.Read(bytes, 0, bytes.Length))!=0)
            {
                // Translate data bytes to a ASCII string.
                data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);

                if(!data.EndsWith("\a\b"))
                {
                    Console.WriteLine("String is NOT OK");
                    throw new SyntaxErrorException();
                }
                data.Replace("\a\b", string.Empty);

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
                server.Server.SendTimeout = 10;
                server.Server.ReceiveTimeout = 10;
                server.Start();
               
                while(true)
                {
                    var client = server.AcceptTcpClient();
                    var stream = client.GetStream();
                    string data = GetData(stream);
                
                    Authenticate(stream, data);
                    Move(stream);


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
