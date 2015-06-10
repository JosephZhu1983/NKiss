using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NKiss.Rpc.TestContract;
using System.Diagnostics;
using System.Threading.Tasks;
using NKiss.Common;
using System.IO;

namespace NKiss.Rpc.TestClientConsole
{
    class Program
    {
        private readonly static string FILE_PATH = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "files");

        static Program()
        {
            if (!Directory.Exists(FILE_PATH))
                Directory.CreateDirectory(FILE_PATH);
        }

        static void Main(string[] args)
        {
            Console.ReadLine();
            int count = 20000;
            string small = new string('a', 1024 * 10);
            string large = new string('a', 1024 * 100);

            var sw = Stopwatch.StartNew();

            var mathService = ServiceClient.CreateClient<IMathService>();
            Console.WriteLine(string.Format("开始串行计算 {0} 次...", count));
            sw.Restart();
            try
            {
                for (int i = 0; i < count; i++)
                {
                    var result = mathService.Add(1, 2);
                    if (result != 3)
                        throw new Exception("错误");
                }
                Console.WriteLine("耗时：" + sw.ElapsedMilliseconds + "毫秒，每秒操作数：" + count * 1000 / sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                LocalLoggingService.Warning(ex.ToString());
            }

            Console.WriteLine(string.Format("开始并行计算 {0} 次...", count));
            sw.Restart();
            try
            {
                Parallel.For(0, count, i =>
                {
                    var result = mathService.Devide(10, 2);
                    if (result != 5)
                        throw new Exception("错误");

                });
                Console.WriteLine("耗时：" + sw.ElapsedMilliseconds + "毫秒，每秒操作数：" + count * 1000 / sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                LocalLoggingService.Warning(ex.ToString());
            }

            //try
            //{
            //    mathService.Devide(1, 0).ToString();
            //}
            //catch (Exception ex)
            //{
            //    LocalLoggingService.Warning(ex.ToString());
            //}

            //var fileService = ServiceClient.CreateClient<IFileService>();

            //Console.WriteLine(string.Format("开始测试小文件"));
            //sw.Restart();

            //for (int i = 0; i < count; i++)
            //{
            //    try
            //    {
            //        var name = DateTime.Now.ToString("yyyyMMddHHmmss") + ".pdf";
            //        fileService.UploadFile(name, File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NoSQL-Whitepaper.pdf")));
            //        File.WriteAllBytes(Path.Combine(FILE_PATH, name), fileService.DownloadFile(name));
            //        Console.WriteLine("耗时：" + sw.ElapsedMilliseconds + "毫秒");
            //    }
            //    catch (Exception ex)
            //    {
            //        LocalLoggingService.Warning(ex.ToString());
            //    }
            //}


            //Console.WriteLine(string.Format("开始测试大文件"));
            //sw.Restart();

            //try
            //{
            //    var name = DateTime.Now.ToString("yyyyMMddHHmmss") + ".rar";
            //    fileService.UploadFile(name, File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mindjet_MindManager_v8.0.217.rar")));
            //    Console.ReadLine();
            //    File.WriteAllBytes(Path.Combine(FILE_PATH, name), fileService.DownloadFile(name));
            //    Console.WriteLine("耗时：" + sw.ElapsedMilliseconds + "毫秒");
            //}
            //catch (Exception ex)
            //{
            //    LocalLoggingService.Warning(ex.ToString());
            //}

            //var scService = ServiceClient.CreateClient<IShoppingCartService>();

            //var name = "朱晔";
            //scService.AddItem(name, new ShoppingCartItem
            //{
            //    Name = "Item1",
            //    Price = 100,
            //});
            //scService.AddItem(name, new ShoppingCartItem
            //{
            //    Name = "Item2",
            //    Price = 200,
            //});

            //var items = scService.GetItems(name);
            //items.ForEach(Console.WriteLine);
            //scService.ClearItems(name);
            //Console.WriteLine(scService.GetItems(name).Count);

            Console.ReadLine();
        }
    }
}
