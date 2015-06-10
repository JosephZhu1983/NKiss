using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace NKiss.Socket
{
    public class AsyncSocketServerState : IDisposable
    {
        private Timer perSecondCounterTimer;

        internal DateTime serverStartDateTime;
        public DateTime ServerStartDateTime
        {
            get
            {
                return serverStartDateTime;
            }
        }

        internal int totalClientConnected;
        public int TotalClientConnected
        {
            get
            {
                return totalClientConnected;
            }
        }

        internal int currentClientConnected;
        public int CurrentClientConnected
        {
            get
            {
                return currentClientConnected;
            }
        }

        internal int currentConcurrentOperation;
        public int CurrentConcurrentOperation
        {
            get
            {
                return currentConcurrentOperation;
            }
        }

        private int currentIOThreadCount;
        public int CurrentIOThreadCount
        {
            get
            {
                return currentIOThreadCount;
            }
        }

        private int currentWorkThreadCount;
        public int CurrentWorkThreadCount
        {
            get
            {
                return currentWorkThreadCount;
            }
        }

        private int currentThreadCount;
        public int CurrentThreadCount
        {
            get
            {
                return currentThreadCount;
            }
        }

        internal int maxClientConnected;
        public int MaxClientConnected
        {
            get
            {
                return maxClientConnected;
            }
        }

        internal DateTime maxClientConnectedDateTime;
        public DateTime MaxClientConnectedDateTime
        {
            get
            {
                return maxClientConnectedDateTime;
            }
        }

        internal long totalBytesReceived;
        public long TotalBytesReceived
        {
            get
            {
                return totalBytesReceived;
            }
        }

        private long lastSecondBytesReceived;
        private int perSecondBytesReceived;
        public int PerSecondBytesReceived
        {
            get
            {
                return perSecondBytesReceived;
            }
        }

        internal long totalSessionCount;
        public long TotalSessionCount
        {
            get
            {
                return totalSessionCount;
            }
        }

        private long lastSecondSessionCount;
        private int perSecondSessionCount;
        public int PerSecondSessionCount
        {
            get
            {
                return perSecondSessionCount;
            }
        }

        internal long totalSentCount;
        public long TotalSentCount
        {
            get
            {
                return totalSentCount;
            }
        }

        private long lastSecondSentCount;
        private int perSecondSentConut;
        public int PerSecondSentConut
        {
            get
            {
                return perSecondSentConut;
            }
        }

        internal long totalReceiveCount;
        public long TotalReceiveCount
        {
            get
            {
                return totalReceiveCount;
            }
        }

        private long lastSecondReceiveCount;
        private int perSecondReceiveConut;
        public int PerSecondReceiveConut
        {
            get
            {
                return perSecondReceiveConut;
            }
        }

        internal long totalBytesSent;
        public long TotalBytesSent
        {
            get
            {
                return totalBytesSent;
            }
        }

        private long lastSecondBytesSent;
        private int perSecondBytesSent;
        public int PerSecondBytesSent
        {
            get
            {
                return perSecondBytesSent;
            }
        }

        public AsyncSocketServerState()
        {
            perSecondCounterTimer = new Timer(state =>
            {
                perSecondBytesReceived = (int)(totalBytesReceived - lastSecondBytesReceived);
                perSecondBytesSent = (int)(totalBytesSent - lastSecondBytesSent);
                perSecondSentConut = (int)(totalSentCount - lastSecondSentCount);
                perSecondReceiveConut = (int)(totalReceiveCount - lastSecondReceiveCount);
                perSecondSessionCount = (int)(totalSessionCount - lastSecondSessionCount);
                lastSecondBytesSent = totalBytesSent;
                lastSecondBytesReceived = totalBytesReceived;
                lastSecondReceiveCount = totalReceiveCount;
                lastSecondSentCount = totalSentCount;
                lastSecondSessionCount = totalSessionCount;
                var maxWorkThread = 0;
                var maxIoThread = 0;
                var availableWorkThread = 0;
                var availableIoThread = 0;
                ThreadPool.GetAvailableThreads(out availableWorkThread, out availableIoThread);
                ThreadPool.GetMaxThreads(out maxWorkThread, out maxIoThread);
                currentWorkThreadCount = maxWorkThread - availableWorkThread;
                currentIOThreadCount = maxIoThread - availableIoThread;
                currentThreadCount = Process.GetCurrentProcess().Threads.Count;
            }, null, 0, 1000);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("当前时间\t{0}{1}", DateTime.Now, Environment.NewLine);
            sb.AppendFormat("服务器启动时间\t{0}{1}", ServerStartDateTime, Environment.NewLine);
            sb.AppendLine();
            sb.AppendFormat("总共连接过的客户端\t{0}{1}", TotalClientConnected, Environment.NewLine);
            sb.AppendFormat("当前客户端连接\t{0}{1}", CurrentClientConnected, Environment.NewLine);
            sb.AppendFormat("最大客户端连接\t{0}{1}", MaxClientConnected, Environment.NewLine);
            sb.AppendFormat("最大客户端连接发生于\t{0}{1}", MaxClientConnectedDateTime, Environment.NewLine);
            sb.AppendFormat("每秒会话次数\t{0}{1}", PerSecondSessionCount, Environment.NewLine);
            sb.AppendFormat("总共会话次数\t{0}{1}", TotalSessionCount, Environment.NewLine);
            sb.AppendLine();
            sb.AppendFormat("当前线程\t{0}{1}", CurrentThreadCount, Environment.NewLine);
            sb.AppendFormat("当前IO线程\t{0}{1}", CurrentIOThreadCount, Environment.NewLine);
            sb.AppendFormat("当前工作线程\t{0}{1}", CurrentWorkThreadCount, Environment.NewLine);
            sb.AppendFormat("当前处理并发\t{0}{1}", CurrentConcurrentOperation, Environment.NewLine);
            sb.AppendLine();
            sb.AppendFormat("每秒接受字节\t{0}{1}", PerSecondBytesReceived, Environment.NewLine);
            sb.AppendFormat("总共接受字节\t{0}{1}", TotalBytesReceived, Environment.NewLine);
            sb.AppendFormat("每秒接受次数\t{0}{1}", PerSecondReceiveConut, Environment.NewLine);
            sb.AppendFormat("总共接受次数\t{0}{1}", TotalReceiveCount, Environment.NewLine);
            sb.AppendLine();
            sb.AppendFormat("每秒发送字节\t{0}{1}", PerSecondBytesSent, Environment.NewLine);
            sb.AppendFormat("总共发送字节\t{0}{1}", TotalBytesSent, Environment.NewLine);
            sb.AppendFormat("每秒发送次数\t{0}{1}", PerSecondSentConut, Environment.NewLine);
            sb.AppendFormat("总共发送次数\t{0}{1}", TotalSentCount, Environment.NewLine);
            return sb.ToString();
        }

        public void Dispose()
        {
            perSecondCounterTimer.Dispose();
        }
    }
}
