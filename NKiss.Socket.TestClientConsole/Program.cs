using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Configuration;
using System.Diagnostics;
using NKiss.Common;

namespace NKiss.Socket.TestClientConsole
{
    class TestClient : AbstractClient<TestClient>
    {
        int datalength = int.Parse(ConfigurationManager.AppSettings["datalength"]);
        public void Test()
        {
            var request = new string('a', datalength);
            var rbody = SendAndReceiveData(Encoding.UTF8.GetBytes(request));
            var response = Encoding.UTF8.GetString(rbody);
            if (response != "ECHO:" + request)
                throw new Exception("数据校验失败！");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var client = TestClient.GetClient("TestCluster");
            Timer t = new Timer(obj =>
            {
                Console.Clear();
                Console.WriteLine(client.State);
            }, null, 0, 1000);

            int sleeptime = int.Parse(ConfigurationManager.AppSettings["sleeptime"]);
            int threadcount = int.Parse(ConfigurationManager.AppSettings["threadcount"]);
            int datalength = int.Parse(ConfigurationManager.AppSettings["datalength"]);
            Console.WriteLine(string.Format("任意键开始测试...集群名：{0} 数据长度：{1} 休眠时间：{2} 线程数：{3}", client.Configuration.Name, datalength, sleeptime, threadcount));
            Console.ReadLine();


            for (int i = 0; i < threadcount; i++)
            {
                new Thread(() =>
                {
                    while (true)
                    {
                        try
                        {
                            Stopwatch sw = Stopwatch.StartNew();
                            client.Test();
                            //LocalLoggingService.Debug(sw.ElapsedMilliseconds.ToString());
                        }
                        catch (Exception ex)
                        {
                            LocalLoggingService.Warning(ex.Message);
                        }
                        finally
                        {
                            Thread.Sleep(sleeptime);
                        }
                    }
                }).Start();
            }

            Console.ReadLine();
        }
    }
}
