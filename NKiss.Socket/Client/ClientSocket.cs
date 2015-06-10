
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NKiss.Common;
using System.Threading;


namespace NKiss.Socket
{
    internal enum ClientSocketStatus
    {
        Idle = 1,
        Busy = 2,
        Error = 3,
        Destroy = 4,
    }

    internal class ClientSocket : IDisposable
    {
        private readonly System.Net.Sockets.Socket socket;
        private BufferedStream stream;
        private readonly Action<ClientSocket> disposeAction;
        private readonly Action<ClientSocket> lowlevelErrorAction;
        private readonly Action<ClientSocket> highLevelErrorAction;
        private readonly ClientNodeState state;

        internal IPEndPoint Endpoint { get; private set; }
        internal DateTime CreateTime { get; private set; }
        internal DateTime BusyTime { get; private set; }
        internal DateTime IdleTime { get; private set; }
        internal ClientSocketStatus Status { get; private set; }

        private string FormatData(byte[] data)
        {
            return FormatData(data, 0, data.Length);
        }

        private string FormatData(byte[] buffer, int offset, int count)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("数据大小：{0}，数据内容", count);
            for (int i = 0; i < count; i++)
            {
                sb.AppendFormat("{0:x2}", buffer[offset + i]);
                if (i % 2 == 1) sb.Append(" ");
            }
            return sb.ToString();
        }

        private void Reset()
        {
            try
            {
                if (socket == null || !socket.Connected) return;

                if (stream!= null) stream.Flush();

                int available = socket.Available;

                if (available > 0)
                {
                    byte[] data = new byte[available];
                    socket.Receive(data, 0, available, SocketFlags.None);
                    LocalLoggingService.Warning(string.Format("重置Socket {0} 时候还有未读取完的数据： {1}！", Endpoint.ToString(), FormatData(data)));
                }
            }
            catch (Exception ex)
            {
                LocalLoggingService.Debug(string.Format("从 {0} 接受数据出错，异常信息为：{1}", Endpoint.ToString(), ex.ToString()));
                lowlevelErrorAction(this);
            }
        }

        internal void Acquire()
        {
            if (Status == ClientSocketStatus.Busy) return;
            Reset();
            BusyTime = DateTime.Now;
            IdleTime = DateTime.MaxValue;
            Status = ClientSocketStatus.Busy;
        }

        internal void Release()
        {
            if (Status == ClientSocketStatus.Idle) return;
            BusyTime = DateTime.MaxValue;
            IdleTime = DateTime.Now;
            Status = ClientSocketStatus.Idle;
        }

        internal void Destroy()
        {
            if (Status == ClientSocketStatus.Destroy) return;
           
            if (socket != null)
            {
                try
                {
                    if (socket.Connected)
                        socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                    LocalLoggingService.Debug(string.Format("释放Socket {0}", Endpoint.ToString()));
                }
                catch (Exception ex)
                {
                    LocalLoggingService.Debug(string.Format("关闭Socket {0} 的时候出现异常了，异常信息为：{1}", Endpoint.ToString(), ex.ToString()));
                }
            }
            if (stream != null)
            {
                try
                {
                    stream.Close();
                }
                catch (Exception ex)
                {
                    LocalLoggingService.Debug(string.Format("关闭Socket流 {0} 的时候出现异常了，异常信息为：{1}", Endpoint.ToString(), ex.ToString()));
                }
            }

            Status = ClientSocketStatus.Destroy;
        }

        internal ClientSocket(ClientNodeState state, Action<ClientSocket> disposeAction, Action<ClientSocket> lowlevelErrorAction, Action<ClientSocket> highLevelErrorAction, int sendTimeout, int receiveTimeout, string ip, int port)
        {
            this.state = state;
            this.disposeAction = disposeAction;
            this.lowlevelErrorAction = lowlevelErrorAction;
            this.highLevelErrorAction = highLevelErrorAction;
            Endpoint = new IPEndPoint(IPAddress.Parse(ip), port);
            socket = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.NoDelay = true;
            socket.SendTimeout = sendTimeout;
            socket.ReceiveTimeout = receiveTimeout;
            CreateTime = DateTime.Now;
            IdleTime = DateTime.MaxValue;
            BusyTime = DateTime.MaxValue;
            Status = ClientSocketStatus.Idle;
        }

        internal void Connect(int timeout)
        {
            IAsyncResult result = socket.BeginConnect(Endpoint, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(timeout, true);
            if (!success)
            {
                throw new Exception(string.Format("连接到 {0} 超过设定时间 {1} 毫秒", Endpoint.ToString(), timeout));
            }
            else
            {
                socket.EndConnect(result);
                stream = new BufferedStream(new NetworkStream(socket));
            }
        }

        public byte[] Read(int length)
        {
            var buffer = new byte[length];
            var totalRead = 0;
            try
            {
                while (totalRead < length)
                {
                    var read = stream.Read(buffer, totalRead, length - totalRead);
                    if (read <= 0)
                    {
                        throw new EndOfStreamException(string.Format("不能从 {0} 流读取信息，流已经关闭！", Endpoint));
                    }

                    totalRead += read;
                }
                Interlocked.Add(ref state.totalBytesReceived, length);
                Interlocked.Increment(ref state.totalReceiveCount);
            }
            catch (Exception ex)
            {
                LocalLoggingService.Debug(string.Format("从 {0} 接受数据出错，异常信息为：{1}", Endpoint.ToString(), ex.ToString()));
                lowlevelErrorAction(this);
                Status = ClientSocketStatus.Error;
                return null;
            }
            return buffer;
        }

        public void Write(byte[] data, int offset, int length)
        {
            try
            {
                stream.Write(data, offset, length);
                Interlocked.Add(ref state.totalBytesSent, length);
                Interlocked.Increment(ref state.totalSentCount);
            }
            catch (Exception ex)
            {
                Status = ClientSocketStatus.Error;
                LocalLoggingService.Debug(string.Format("发送数据到 {0} 出错，异常信息为：{1}", Endpoint.ToString(), ex.ToString()));
                highLevelErrorAction(this);
            }
        }

        public void Write(byte[] bytes)
        {
            Write(bytes, 0, bytes.Length);
        }

        public void Dispose()
        {
            disposeAction(this);
        }
    }
}
