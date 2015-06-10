using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NKiss.Common;

namespace NKiss.Socket
{
    public class AsyncSocketServer : IDisposable
    {
        private Func<byte[], byte[]> handler;
        private AsyncSocketServerConfiguration config;
        private AsyncSocketServerState state;
        private BufferManager bufferManager;
        private ConcurrentStack<SocketAsyncEventArgs> acceptEventArgsPool;
        private ConcurrentStack<SocketAsyncEventArgs> operationEventArgsPool;
        private Semaphore maxConnectionsEnforcer;
        private System.Net.Sockets.Socket listenSocket;
        private ServerDataHandler dataHandler = new ServerDataHandler();

        public AsyncSocketServerState State
        {
            get
            {
                return state;
            }
        }

        public AsyncSocketServerConfiguration Configuration
        {
            get
            {
                return config;
            }
        }

        public AsyncSocketServer(AsyncSocketServerConfiguration config)
        {
            try
            {
                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
                this.config = config;
                if (config.EnablePerformanceCounter)
                    state = new AsyncSocketServerState();

                //总共需要的缓冲为最大连接*2（收发）*每一个缓冲大小
                bufferManager = new BufferManager(config.BufferSize * config.MaxConnections * 2, config.BufferSize * 2);
                //独立的接受参数池和操作（收发）参数池
                acceptEventArgsPool = new ConcurrentStack<SocketAsyncEventArgs>();
                operationEventArgsPool = new ConcurrentStack<SocketAsyncEventArgs>();
                //限制客户端连接并发
                maxConnectionsEnforcer = new Semaphore(config.MaxConnections, config.MaxConnections);
            }
            catch (Exception ex)
            {
                LocalLoggingService.Error("初始化服务器出错，异常信息为：" + ex.ToString());
            }
        }

        public AsyncSocketServer()
            : this(AsyncSocketServerConfiguration.GetConfig())
        {

        }

        public void Start(Func<byte[], byte[]> handler)
        {
            //从外部传入服务端的处理逻辑
            this.handler = handler;
            try
            {
                //初始化缓冲
                bufferManager.Init();
            }
            catch (Exception ex)
            {
                LocalLoggingService.Error("初始化缓冲出错，异常信息为：" + ex.ToString());
                return;
            }

            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, config.ListeningPort);
            listenSocket = new System.Net.Sockets.Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                listenSocket.Bind(localEndPoint);
                listenSocket.Listen(config.MaxQueuedConnections);
            }
            catch (Exception ex)
            {
                LocalLoggingService.Error("监听端口出错，异常信息为：" + ex.ToString());
                return;
            }
            //设置服务端启动时间
            if (config.EnablePerformanceCounter)
                state.serverStartDateTime = DateTime.Now;

            //开始接受客户端连接
            StartAccept();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LocalLoggingService.Error("捕获到未处理异常，异常信息为：" + (e.ExceptionObject as Exception).ToString());
        }

        private void StartAccept()
        {
            maxConnectionsEnforcer.WaitOne();

            SocketAsyncEventArgs acceptEventArgs;
            if (!acceptEventArgsPool.TryPop(out acceptEventArgs))
            {
                acceptEventArgs = CreateAcceptEventArgs();
                LocalLoggingService.Debug("新建一个acceptEventArgs");
            }

            try
            {
                if (!listenSocket.AcceptAsync(acceptEventArgs))
                {
                    LocalLoggingService.Warning("同步方式建立连接");
                    ProcessAccept(acceptEventArgs);
                }
            }
            catch (Exception ex)
            {
                LocalLoggingService.Warning("异步建立连接出错，异常信息为：" + ex.ToString());
            }
        }

        private SocketAsyncEventArgs CreateAcceptEventArgs()
        {
            SocketAsyncEventArgs acceptEventArgs = new SocketAsyncEventArgs();
            //接受的参数不需要缓冲区以及Token
            acceptEventArgs.Completed += (sender, e) => ProcessAccept(e);
            return acceptEventArgs;
        }

        private void ProcessAccept(SocketAsyncEventArgs acceptEventArgs)
        {
            LocalLoggingService.Info("成功建立一个连接");

            //立即可以再建立连接
            StartAccept();

            //失败
            if (acceptEventArgs.SocketError != SocketError.Success)
            {
                LocalLoggingService.Warning("建立连接出错，异常状态为：" + acceptEventArgs.SocketError);
                //关闭失败的Socket
                acceptEventArgs.AcceptSocket.Close();
                //接受参数可以重用
                acceptEventArgsPool.Push(acceptEventArgs);
                return;
            }

            //处理性能计数器
            if (config.EnablePerformanceCounter)
            {
                Interlocked.Increment(ref state.totalClientConnected);
                Interlocked.Increment(ref state.currentClientConnected);

                if (state.maxClientConnected < state.currentClientConnected)
                {
                    state.maxClientConnected = state.totalClientConnected;
                    state.maxClientConnectedDateTime = DateTime.Now;
                }
            }

            //提领一个SocketAsyncEventArgs用于收发消息
            SocketAsyncEventArgs operationEventArgs;
            if (!operationEventArgsPool.TryPop(out operationEventArgs))
            {
                LocalLoggingService.Debug("新建一个operationEventArgs");
                operationEventArgs = CreateOperationEventArgs();
                if (operationEventArgs == null)
                {
                    LocalLoggingService.Warning("CreateOperationEventArgs失败");
                    return;
                }
            }

            //Socket转接
            operationEventArgs.AcceptSocket = acceptEventArgs.AcceptSocket;
            //accept的Socket释放
            acceptEventArgs.AcceptSocket = null;
            //accept的SocketAsyncEventArgs还进去
            acceptEventArgsPool.Push(acceptEventArgs);
            //开始接受消息
            StartReceive(operationEventArgs);
        }


        private SocketAsyncEventArgs CreateOperationEventArgs()
        {
            SocketAsyncEventArgs operationEventArgs = new SocketAsyncEventArgs();
            //缓冲
            if (!bufferManager.SetBuffer(operationEventArgs))
            {
                LocalLoggingService.Warning("没有足够的缓冲内存");
                return null;
            }
            //异步事件
            operationEventArgs.Completed += (sender, e) => ProcessOperation(e);
            //Token
            OperationUserToken token = new OperationUserToken(operationEventArgs.Offset, operationEventArgs.Offset + config.BufferSize);
            operationEventArgs.UserToken = token;
            return operationEventArgs;
        }

        private void StartReceive(SocketAsyncEventArgs operationEventArgs)
        {
            LocalLoggingService.Debug("等待接受数据...");

            //operationEventArgs是有Token的
            OperationUserToken token = operationEventArgs.UserToken as OperationUserToken;
            //设置缓冲
            operationEventArgs.SetBuffer(token.ReceiveOffsetOnBuffer, config.BufferSize);
            try
            {
                if (!operationEventArgs.AcceptSocket.ReceiveAsync(operationEventArgs))
                {
                    LocalLoggingService.Warning("同步方式接受数据");
                    ProcessReceive(operationEventArgs);
                }
            }
            catch (Exception ex)
            {
                LocalLoggingService.Warning("异步接受数据出错，异常信息为：" + ex.ToString());
            }
        }

        private void ProcessReceive(SocketAsyncEventArgs operationEventArgs)
        {
            if (!operationEventArgs.AcceptSocket.Connected)
            {
                LocalLoggingService.Warning("接受数据的时候发现端口已经断开");
                CloseClientSocket(operationEventArgs);
                return;
            }

            OperationUserToken token = operationEventArgs.UserToken as OperationUserToken;
            if (operationEventArgs.SocketError != SocketError.Success)
            {
                LocalLoggingService.Warning("接受数据出错，状态为：" + operationEventArgs.SocketError);
                //为下一次接受做准备
                token.ResetForReceive();
                //关闭连接
                CloseClientSocket(operationEventArgs);
                return;
            }

            if (operationEventArgs.BytesTransferred == 0)
            {
                LocalLoggingService.Debug("没有需要接受的数据了");
                token.ResetForReceive();
                CloseClientSocket(operationEventArgs);
                return;
            }

            if (config.EnablePerformanceCounter)
            {
                Interlocked.Add(ref state.totalBytesReceived, operationEventArgs.BytesTransferred);
                Interlocked.Increment(ref state.totalReceiveCount);
            }

            try
            {
                if (!dataHandler.ReceiveData(operationEventArgs, operationEventArgs.BytesTransferred, 0))
                {
                    StartReceive(operationEventArgs);
                    return;
                }
            }
            catch (Exception ex)
            {
                LocalLoggingService.Warning("在处理接收到的数据时发生错误，异常信息为：" + ex.ToString());
                CloseClientSocket(operationEventArgs);
                return;
            }

            if (config.EnablePerformanceCounter)
                Interlocked.Increment(ref state.currentConcurrentOperation);
            try
            {
                //生成需要发送的数据
                token.SendBodyData = handler(token.ReceiveBodyData);
            }
            catch (Exception ex)
            {
                LocalLoggingService.Warning("处理数据的时候出错，异常信息：" + ex.ToString());
                CloseClientSocket(operationEventArgs);
                return;
            }
            if (config.EnablePerformanceCounter)
                Interlocked.Decrement(ref state.currentConcurrentOperation);

            if (token.SendBodyData == null)
            {
                LocalLoggingService.Warning("处理数据后并没有得到有效的数据，请求被忽略");
                CloseClientSocket(operationEventArgs);
                return;
            }

            token.ResetForSend();
            StartSend(operationEventArgs);
        }

        private void StartSend(SocketAsyncEventArgs operationEventArgs)
        {
            LocalLoggingService.Debug("开始发送数据...");

            dataHandler.PrepareSendData(operationEventArgs, config.BufferSize, 0);

            try
            {
                if (!operationEventArgs.AcceptSocket.SendAsync(operationEventArgs))
                {
                    LocalLoggingService.Warning("同步发送消息");
                    ProcessSend(operationEventArgs);
                }
            }
            catch (Exception ex)
            {
                LocalLoggingService.Warning("异步发送数据出错：" + ex.ToString());
            }
        }

        private void ProcessSend(SocketAsyncEventArgs operationEventArgs)
        {
            if (!operationEventArgs.AcceptSocket.Connected)
            {
                LocalLoggingService.Warning("发送数据的时候发现端口已经断开");
                CloseClientSocket(operationEventArgs);
                return;
            }

            OperationUserToken token = operationEventArgs.UserToken as OperationUserToken;

            if (operationEventArgs.SocketError != SocketError.Success)
            {
                LocalLoggingService.Warning("发送数据出错：" + operationEventArgs.SocketError);
                CloseClientSocket(operationEventArgs);
            }

            if (config.EnablePerformanceCounter)
            {
                Interlocked.Add(ref state.totalBytesSent, operationEventArgs.BytesTransferred);
                Interlocked.Increment(ref state.totalSentCount);
            }

            if (!token.SendHeaderCompleted || !token.SendBodyCompleted)
            {
                StartSend(operationEventArgs);
                return;
            }

            LocalLoggingService.Debug("所有数据发送完毕，等待下一次请求");
            if (config.EnablePerformanceCounter)
                Interlocked.Increment(ref state.totalSessionCount);
            token.ResetForReceive();
            StartReceive(operationEventArgs);
        }

        private void CloseClientSocket(SocketAsyncEventArgs operationEventArgs)
        {
            LocalLoggingService.Info("关闭连接");
            try
            {
                operationEventArgs.AcceptSocket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception ex)
            {
                LocalLoggingService.Warning("Shutdown的时候出现异常：" + ex.Message);
            }
            try
            {
                var token = operationEventArgs.UserToken as OperationUserToken;
                token.ResetForReceive();
                token.ResetForSend();
                //关闭Socket
                operationEventArgs.AcceptSocket.Close();
                //回收operationEventArgs
                operationEventArgsPool.Push(operationEventArgs);
                //少一个客户端连接
                if (config.EnablePerformanceCounter)
                    Interlocked.Decrement(ref state.currentClientConnected);
                //可以多放一个连接进来
                maxConnectionsEnforcer.Release();
            }
            catch (Exception ex)
            {
                LocalLoggingService.Error("关闭连接的时候出现错误，异常信息为：" + ex.ToString());
            }
        }

        private void ProcessOperation(SocketAsyncEventArgs operationEventArgs)
        {
            switch (operationEventArgs.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(operationEventArgs);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(operationEventArgs);
                    break;
                default:
                    LocalLoggingService.Warning("上一个操作不是接受也不是发送");
                    break;
            }
        }

        public void Dispose()
        {
            //性能计数器
            if (config.EnablePerformanceCounter)
                state.Dispose();

            //两个池
            SocketAsyncEventArgs eventArgs;
            while (acceptEventArgsPool.TryPop(out eventArgs))
            {
                eventArgs.Dispose();
            }
            while (operationEventArgsPool.TryPop(out eventArgs))
            {
                eventArgs.Dispose();
            }
        }
    }
}
