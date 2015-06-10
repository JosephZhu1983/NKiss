using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NKiss.Rpc.TestContract;
using System.Threading;

namespace NKiss.Rpc.TestServerConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var host = new ServiceHost())
            {
                host.AddService<IMathService, MathService>(ServiceInstanceMode.SingleCall);
                host.AddService<IShoppingCartService, ShoppingCartService>(ServiceInstanceMode.Singleton);
                host.AddService<IFileService, FileService>(ServiceInstanceMode.Singleton);
                host.Start();

                Timer t = new Timer(obj =>
                {
                    Console.Clear();
                    Console.WriteLine(host.SocketConfiguration);
                    Console.WriteLine(host.SocketState);
                }, null, 0, 1000);

                Console.ReadLine();
            }
        }
    }
}
