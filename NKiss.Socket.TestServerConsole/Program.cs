using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;

namespace NKiss.Socket.TestServerConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                using (AsyncSocketServer server = new AsyncSocketServer())
                {
                    server.Start(receive =>
                    {
                        string echo = "ECHO:";
                        var prefix = Encoding.UTF8.GetBytes(echo);
                        var send = new byte[prefix.Length + receive.Length];
                        Buffer.BlockCopy(prefix, 0, send, 0, prefix.Length);
                        Buffer.BlockCopy(receive, 0, send, prefix.Length, receive.Length);
                        return send;
                    });

                    Timer t = new Timer(obj =>
                    {
                        Console.Clear();
                        Console.WriteLine(server.Configuration);
                        Console.WriteLine(server.State);
                    }, null, 0, 1000);

                    Console.ReadLine();
                }                
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
