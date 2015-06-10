using NKiss.Common;
using System.Text;
using System;

namespace NKiss.Socket
{
    public class AsyncSocketServerConfiguration
    {
        public bool EnablePerformanceCounter { get; set; }

        public int MaxConnections { get; set; }

        public int MaxQueuedConnections { get; set; }

        public int ListeningPort { get; set; }

        public int BufferSize { get; set; }

        public AsyncSocketServerConfiguration()
        {
            EnablePerformanceCounter = true;
            MaxConnections = 5000;
            MaxQueuedConnections = 100;
            ListeningPort = 1983;
            BufferSize = 20480;
        }

        public static AsyncSocketServerConfiguration GetConfig()
        {
            return LocalConfigService.GetConfig(new AsyncSocketServerConfiguration());
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("是否允许性能计数器\t{0}{1}", EnablePerformanceCounter, Environment.NewLine);
            sb.AppendFormat("最大连接\t{0}{1}", MaxConnections, Environment.NewLine);
            sb.AppendFormat("最大排队连接\t{0}{1}", MaxQueuedConnections, Environment.NewLine);
            sb.AppendFormat("监听端口\t{0}{1}", ListeningPort, Environment.NewLine);
            sb.AppendFormat("缓冲大小\t{0}{1}", BufferSize, Environment.NewLine);
            return sb.ToString();
        }
    }
}
